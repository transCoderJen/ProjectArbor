using System.Collections.Generic;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Effects;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    public enum ToolType
    {
        Plough,
        Blood,
        Seeds,
        Basket
    }

    public class Player : Entity, IHealable, ISaveManager
    {
        public static Player Instance { get; private set; }

        #region Combat / Progression

        [Header("Attack Details")]
        public Vector2[] AttackMovement;
        public float CounterAttackDuration;

        [Header("Progression")]
        [SerializeField] private PlayerProgression progression;

        public int AttackDamage =>
            progression != null ? progression.CurrentWeaponDamage : 1;

        [HideInInspector] public bool AttackBuffered = false;

        #endregion

        #region Input

        [Header("Input")]
        [SerializeField] private PlayerInputReader inputReader;

        public PlayerInputReader InputReader => inputReader;
        public PlayerInput PlayerInput => inputReader != null ? inputReader.PlayerInput : null;

        public Vector2 CachedMoveInput { get; private set; }
        public bool UsingController { get; private set; }

        private bool controlsEnabled = true;
        public bool ControlsEnabled => controlsEnabled;

        #endregion

        #region Components / Transforms

        [Header("Components")]
        public TerrainGrassCutter GrassCutter;
        [SerializeField] private LayerBasedParticleSpawner particleSpawner;

        [Header("Transforms")]
        public Transform ToolIndicator;
        public Transform GrowBlockCheck;

        [Header("Settings")]
        public float GrowBlockCheckDistance;

        #endregion

        #region Farming / Tools

        [Header("Seed")]
        public ItemData_Seed EquippedSeed;

        public ToolType CurrentTool;

        [Header("Farming")]
        [SerializeField] private float blockInteractRadius = 3f;
        public bool InManagementState = false;
        public bool InCommanderMode = false;
        
        #endregion

        #region Building

        [Header("Building")]
        [SerializeField] private BuildingSO EquippedBuildable;

        [Header("Building Placement Colors")]
        [SerializeField, ColorUsage(showAlpha: true, hdr: true)]
        private Color errorTintColor = new(0.35f, 0.12f, 0.14f, 1f);

        [SerializeField, ColorUsage(showAlpha: true, hdr: true)]
        private Color availableToPlaceTintColor = new(51f / 255f, 166f / 255f, 255f / 255f, 1f);

        [Header("2D Building Placement Glow")]
        [SerializeField] private float errorSineGlowMin = 0.35f;

        [SerializeField, ColorUsage(showAlpha: true, hdr: true)]
        private Color errorSineGlowColor = new(0.12f, 0.09f, 0.10f, 1f);

        [SerializeField] private float availableSineGlowMin = 0.75f;

        [SerializeField, ColorUsage(showAlpha: true, hdr: true)]
        private Color availableSineGlowColor = new(22f / 255f, 25f / 255f, 26f / 255f, 1f);

        [Header("2D Building Placement Hologram")]
        [SerializeField, ColorUsage(showAlpha: true, hdr: true)]
        private Color errorHologramTintColor = new(0.18f, 0.10f, 0.13f, 1f);

        [SerializeField, ColorUsage(showAlpha: true, hdr: true)]
        private Color availableHologramTintColor = new(29f / 255f, 25f / 255f, 36f / 255f, 1f);
        

        [Header("Build Drag")]
        [SerializeField] private float dragBuildCooldown = 0.05f;

        private float nextDragBuildTime;
        private float currentBuildYRotation;

        private bool buildingPlacementActive;
        public bool IsPlacingBuilding => buildingPlacementActive;
        private bool commandDragPlacementEnabled;
        private GameObject ghostInstance;
        private MeshRenderer[] ghostRenderers;
        private SpriteRenderer[] ghostSpriteRenderers;

        private readonly HashSet<GrowBlock> dragVisitedBlocks = new();

        private static readonly int TINT = Shader.PropertyToID("_Tint");
        private static readonly int FRESNEL = Shader.PropertyToID("_FresnelColor");
        private static readonly int HOLOGRAM_TINT = Shader.PropertyToID("_HologramTint");
        private static readonly int SINE_GLOW_MIN = Shader.PropertyToID("_SineGlowMin");
        private static readonly int SINE_GLOW_COLOR = Shader.PropertyToID("_SineGlowColor");

        private MaterialPropertyBlock ghostPropertyBlock;

        #endregion

        #region Water Blocking

        [Header("Water Blocking")]
        [SerializeField] private Terrain terrain;
        [SerializeField] private float waterLevelY = 0f;
        [SerializeField] private float waterBorderPadding = 0.25f;

        #endregion

        #region Interact

        [Header("Interact")]
        [SerializeField] private float interactRadius = 2f;
        [SerializeField] private LayerMask interactLayer;
        [SerializeField] private LayerMask floorLayer;

        private IInteractable currentHighlightedInteractable;

        #endregion

        #region State Machine

        [Header("State Machine")]
        public PlayerStateMachine StateMachine { get; private set; }
        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerManagementState ManagementState { get; private set; }
        public PlayerCommanderState CommanderState { get; private set; }
        public PlayerAttackState AttackState { get; private set; }

        #endregion

        public bool HasNearbyInteractable =>
            GetClosestInteractable() != null;
        
        private bool controllerToolTargeting;

        public bool IsControllerToolTargeting =>
            UsingController && controllerToolTargeting;

        #region Unity Lifecycle

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            base.Awake();

            if (inputReader == null)
                inputReader = GetComponent<PlayerInputReader>();

            if (progression == null)
                progression = GetComponent<PlayerProgression>();

            StateMachine = new PlayerStateMachine();

            IdleState = new PlayerIdleState(this, StateMachine, "Idle");
            MoveState = new PlayerMoveState(this, StateMachine, "Move");
            ManagementState = new PlayerManagementState(this, StateMachine, "Idle");
            AttackState = new PlayerAttackState(this, StateMachine, "Attack");
            CommanderState = new PlayerCommanderState(this, StateMachine, "Idle");

            ghostPropertyBlock = new MaterialPropertyBlock();
        }

        protected override void Start()
        {
            base.Start();

            if (PlayerInput != null)
                UsingController = PlayerInput.currentControlScheme == "Gamepad";

            StateMachine.Initialize(IdleState);

            foreach (PlayerInput playerInput in PlayerInput.all)
            {
                Debug.Log(
                    $"[PlayerInput] " +
                    $"Object={playerInput.gameObject.name}, " +
                    $"Scene={playerInput.gameObject.scene.name}, " +
                    $"Index={playerInput.playerIndex}, " +
                    $"Scheme={playerInput.currentControlScheme}, " +
                    $"Enabled={playerInput.enabled}",
                    playerInput.gameObject);
            }

            
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (inputReader == null)
                inputReader = GetComponent<PlayerInputReader>();

            if (inputReader != null)
            {
                inputReader.MoveChanged += HandleMoveChanged;
                inputReader.InteractPressed += HandleInteractPressed;
                inputReader.InteractReleased += HandleInteractReleased;
                inputReader.AttackPressed += HandleAttackInput;
                inputReader.CancelPressed += HandleCancelInput;

                if (inputReader.PlayerInput != null)
                    inputReader.PlayerInput.onControlsChanged += OnControlsChanged;
            }

            Bus<ToolEquipEvent>.OnEvent += HandleToolEquipped;
            Bus<SeedEquipEvent>.OnEvent += HandleSeedEquipped;
            Bus<EnablePlayerMovementEvent>.OnEvent += HandleEnablePlayerMovement;
        }

        private void HandleInteractPressed()
        {
            if (!controlsEnabled)
                return;

            IInteractable closestInteractable = GetClosestInteractable();

            if (closestInteractable != null)
            {
                closestInteractable.Interact(this);
                return;
            }

            if (UsingController && GetBlock() != null)
            {
                controllerToolTargeting = true;
                return;
            }

            UseTool();
        }

        private void HandleInteractReleased()
        {
            if (!controllerToolTargeting)
                return;

            controllerToolTargeting = false;

            if (!controlsEnabled)
                return;

            UseTool();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (inputReader != null)
            {
                inputReader.MoveChanged -= HandleMoveChanged;
                inputReader.InteractPressed -= HandleInteractPressed;
                inputReader.InteractReleased -= HandleInteractReleased;
                inputReader.AttackPressed -= HandleAttackInput;
                inputReader.CancelPressed -= HandleCancelInput;

                if (inputReader.PlayerInput != null)
                    inputReader.PlayerInput.onControlsChanged -= OnControlsChanged;
            }

            Bus<ToolEquipEvent>.OnEvent -= HandleToolEquipped;
            Bus<SeedEquipEvent>.OnEvent -= HandleSeedEquipped;
            Bus<EnablePlayerMovementEvent>.OnEvent -= HandleEnablePlayerMovement;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        protected override void Update()
        {
            base.Update();

            if (!controlsEnabled)
                return;

            StateMachine.CurrentState?.Update();

            if (inputReader.InteractHeld)
            {
                TryContinuousInteract();
            }

            UpdateInteractableHighlight();
            HandleDebugInputs();

            if (buildingPlacementActive &&
                !InManagementState &&
                !InCommanderMode)
            {
                CancelBuildingPlacement();
            }

            CreateGhost();
            HandleGhost();
            HandleBuildDrag();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!controlsEnabled)
            {
                StateMachine.ChangeState(IdleState);
                StopMovement();
                return;
            }

            StateMachine.CurrentState?.FixedUpdate();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateGrowBlockCheckPosition();
        }

        #endregion

        #region Input Handlers

        private void HandleMoveChanged(Vector2 moveInput)
        {
            if (!controlsEnabled)
            {
                CachedMoveInput = Vector2.zero;
                return;
            }

            CachedMoveInput = moveInput;
        }

        private void HandleAttackInput()
        {
            if (Helpers.DebounceClicks())
                return;
            
            if (!controlsEnabled)
                return;

            if (InManagementState)
                return;

            AttackBuffered = true;
        }

        private void HandleCancelInput()
        {
            if (!controlsEnabled)
                return;

            DestroyGhost();
            // ResetBuildDrag();
        }

        private void OnControlsChanged(PlayerInput input)
        {
            Debug.Log(
                $"Controls changed. Scheme='{input.currentControlScheme}', " +
                $"Devices={string.Join(", ", input.devices)}");

            UsingController = input.currentControlScheme == "Gamepad";
        }

        private void HandleEnablePlayerMovement(EnablePlayerMovementEvent evt)
        {
            controlsEnabled = evt.EnableMovement;

            if (!controlsEnabled)
            {
                StopMovement();
                CachedMoveInput = Vector2.zero;
                inputReader?.ClearMoveInput();
                inputReader?.ClearHeldInputs();
                // ResetBuildDrag();
            }
        }

        #endregion

        #region Debug Inputs

        private void HandleDebugInputs()
        {
#if UNITY_EDITOR
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.numpad1Key.wasPressedThisFrame)
                CurrentTool = ToolType.Plough;

            if (Keyboard.current.numpad2Key.wasPressedThisFrame)
                CurrentTool = ToolType.Blood;

            if (Keyboard.current.numpad3Key.wasPressedThisFrame)
                CurrentTool = ToolType.Seeds;

            if (Keyboard.current.oem4Key.wasPressedThisFrame)
                CurrentTool = ToolType.Basket;

            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                if (CameraManager.Instance.IsTransitioning)
                    return;

                if (StateMachine.CurrentState != ManagementState &&
                    StateMachine.CurrentState != CommanderState)
                {
                    StateMachine.ChangeState(ManagementState);
                }
            }

            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                if (CameraManager.Instance.IsTransitioning)
                    return;

                if (StateMachine.CurrentState != CommanderState &&
                    StateMachine.CurrentState != ManagementState)
                {
                    StateMachine.ChangeState(CommanderState);
                }
            }
            
            if (Keyboard.current.kKey.isPressed)
                GridInfo.Instance.GrowCrop();
#endif
        }

        #endregion

        #region Events

        private void HandleSeedEquipped(SeedEquipEvent evt)
        {
            EquippedSeed = evt.Seed;
        }

        private void HandleToolEquipped(ToolEquipEvent evt)
        {
            CurrentTool = evt.Tool;
        }

        #endregion

        #region Movement

        public override void ApplyMovement(Vector2 input, bool normalized = true)
        {
            if (WouldMoveIntoWater(input, normalized))
            {
                StopMovement();
                return;
            }

            base.ApplyMovement(input, normalized);
            UpdateGrowBlockCheckPosition();
        }

        public void SetFacingDirection(Vector3 direction)
        {
            if (direction == Vector3.zero)
                return;

            LastFacingDir = direction.normalized;
            FacingDir = direction.normalized;
        }

        private bool WouldMoveIntoWater(Vector2 input, bool normalized)
        {
            if (terrain == null || input == Vector2.zero)
                return false;

            Vector2 moveInput = normalized ? input.normalized : input;
            Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

            Vector3 checkPosition =
                transform.position + moveDirection * waterBorderPadding;

            float terrainWorldHeight =
                terrain.SampleHeight(checkPosition) + terrain.transform.position.y;

            return terrainWorldHeight <= waterLevelY;
        }

        private void UpdateGrowBlockCheckPosition()
        {
            if (GrowBlockCheck == null)
                return;

            GrowBlockCheck.position =
                transform.position +
                FacingDir * GrowBlockCheckDistance +
                Vector3.up * CheckHeight;
        }

        #endregion

        #region Tools

        private void UseTool()
        {
            if (!controlsEnabled)
                return;

            if (InManagementState)
                return;

            GrowBlock block = GetBlock();

            if (block == null)
                return;

            block.UseContextAction(EquippedSeed);
        }

        #endregion

        #region Build Drag

        private void HandleBuildDrag()
        {
            bool managementDragActive = InManagementState;

            bool commandPlacementActive =
                InCommanderMode &&
                buildingPlacementActive;

            if (!managementDragActive && !commandPlacementActive)
            {
                ResetBuildDrag();
                return;
            }

            if (inputReader == null)
                return;

            if (commandPlacementActive && !commandDragPlacementEnabled)
            {
                HandleSingleCommandPlacement();
                return;
            }

            if (!inputReader.AttackHeld)
            {
                ResetBuildDrag();
                return;
            }

            GrowBlock block = GetBlock();

            if (block == null)
                return;

            if (dragVisitedBlocks.Contains(block))
                return;

            if (commandPlacementActive)
            {
                TryPlaceSelectedBuilding(block);
                return;
            }

            // Old dedicated management building mode.
            if (TryBuildOnBlock(block))
                dragVisitedBlocks.Add(block);
        }

        private void HandleSingleCommandPlacement()
        {
            if (Mouse.current == null)
                return;

            if (!Mouse.current.leftButton.wasReleasedThisFrame)
                return;

            GrowBlock block = GetBlock();

            if (block == null)
                return;

            TryPlaceSelectedBuilding(block);
        }

        private void ResetBuildDrag()
        {
            dragVisitedBlocks.Clear();
        }

        public bool TryPlaceSelectedBuilding(GrowBlock targetBlock)
        {
            if (!buildingPlacementActive)
                return false;

            if (targetBlock == null)
                return false;

            if (dragVisitedBlocks.Contains(targetBlock))
                return false;

            if (!TryBuildOnBlock(targetBlock))
                return false;

            dragVisitedBlocks.Add(targetBlock);

            if (!commandDragPlacementEnabled)
                CancelBuildingPlacement();

            return true;
        }

        private bool TryBuildOnBlock(GrowBlock block)
        {
            Debug.Log(
                $"[Try Build] Attempt started on block: " +
                $"{(block != null ? block.name : "null")}");

            if (!CanBuildOnBlock(block))
            {
                Debug.LogWarning("[Try Build] Aborted: CanBuildOnBlock returned false.");
                return false;
            }

            bool replacingFenceWithGate =
                IsGateReplacingFence(
                    block,
                    out FencePost2D fenceToReplace);

            Debug.Log(
                $"[Try Build] Replacement result: {replacingFenceWithGate}, " +
                $"Fence: {(fenceToReplace != null ? fenceToReplace.name : "null")}");

            GameObject builtObject = Instantiate(
                EquippedBuildable.Prefab,
                block.transform.position,
                Quaternion.Euler(
                    EquippedBuildable.XRotation,
                    currentBuildYRotation,
                    0f));

            Debug.Log(
                $"[Try Build] Instantiated: " +
                $"{(builtObject != null ? builtObject.name : "null")}");

            if (!builtObject.TryGetComponent(out BaseBuilding buildable))
            {
                Debug.LogError(
                    $"[Try Build] Failed: instantiated prefab " +
                    $"{builtObject.name} has no BaseBuilding on the root.");

                Destroy(builtObject);
                return false;
            }

            buildable.PlaceAsConstructionSite();

            Debug.Log(
                $"[Try Build] Construction site created for {buildable.name}.");

            block.ResetCrop();

            if (replacingFenceWithGate && fenceToReplace != null)
            {
                Debug.Log(
                    $"[Try Build] Replacing {fenceToReplace.name} " +
                    $"with {buildable.name}.");

                fenceToReplace.ReplaceWith(buildable);

                Debug.Log(
                    $"[Try Build] Replacement call complete. " +
                    $"Block now contains: " +
                    $"{(block.CurrentBuildable != null ? block.CurrentBuildable.name : "null")}");
            }
            else
            {
                Debug.Log("[Try Build] Performing normal placement.");

                buildable.SetOccupiedBlock(block);
                block.SetBuildable(buildable);

                if (buildable is FencePost2D fence)
                {
                    Debug.Log(
                        $"[Try Build] Refreshing fence connections for {fence.name}.");

                    fence.RefreshConnections(block);
                    FencePost2D.RefreshNeighbors(block);
                }
            }

            EquippedBuildable.SpendCost();

            Debug.Log("[Try Build] Build attempt completed successfully.");
            return true;
        }

        private bool CanBuildOnBlock(GrowBlock block)
        {
            Debug.Log(
                $"[Build Check] Checking block: {(block != null ? block.name : "null")}");

            if (block == null)
            {
                Debug.LogWarning("[Build Check] Failed: block is null.");
                return false;
            }

            Debug.Log(
                $"[Build Check] IsActive: {block.IsActive}, " +
                $"HasBuildable: {block.HasBuildable}, " +
                $"CurrentBuildable: {(block.CurrentBuildable != null ? block.CurrentBuildable.name : "null")}");

            /*
            * Normally occupied blocks are inactive because SetBuildable()
            * sets IsActive to false. Therefore, replacement must be checked
            * before rejecting the block for being inactive.
            */
            bool replacingFenceWithGate =
                IsGateReplacingFence(block, out FencePost2D fenceToReplace);

            Debug.Log(
                $"[Build Check] Replacing fence with gate: {replacingFenceWithGate}, " +
                $"Fence to replace: {(fenceToReplace != null ? fenceToReplace.name : "null")}");

            if (!block.IsActive && !replacingFenceWithGate)
            {
                Debug.LogWarning(
                    "[Build Check] Failed: block is inactive and this is not a valid replacement.");

                return false;
            }

            if (EquippedBuildable == null)
            {
                Debug.LogWarning("[Build Check] Failed: EquippedBuildable is null.");
                return false;
            }

            if (EquippedBuildable.Prefab == null)
            {
                Debug.LogWarning("[Build Check] Failed: equipped prefab is null.");
                return false;
            }

            if (block.HasBuildable && !replacingFenceWithGate)
            {
                Debug.LogWarning(
                    "[Build Check] Failed: block is occupied and replacement is not valid.");

                return false;
            }

            if (!EquippedBuildable.CanAfford())
            {
                Debug.LogWarning("[Build Check] Failed: cannot afford buildable.");
                return false;
            }

            if (EquippedBuildable.Prefab.TryGetComponent(
                    out FencePost2D fence))
            {
                bool canPlaceFence = fence.CanPlaceFence(block);

                Debug.Log(
                    $"[Build Check] Fence placement result: {canPlaceFence}, " +
                    $"Prefab component: {fence.GetType().Name}, " +
                    $"IsGate: {fence.IsGate}");

                if (!canPlaceFence)
                {
                    Debug.LogWarning(
                        "[Build Check] Failed: FencePost2D.CanPlaceFence returned false.");

                    return false;
                }
            }

            Debug.Log("[Build Check] Success: block can be built on.");
            return true;
        }

        private bool IsGateReplacingFence(GrowBlock block, out FencePost2D fenceToReplace)
        {
            fenceToReplace = null;

            Debug.Log(
                $"[Gate Replace] Checking replacement. " +
                $"Block: {(block != null ? block.name : "null")}, " +
                $"Equipped: {(EquippedBuildable != null ? EquippedBuildable.name : "null")}, " +
                $"Prefab: {(EquippedBuildable != null && EquippedBuildable.Prefab != null ? EquippedBuildable.Prefab.name : "null")}");

            if (block == null)
            {
                Debug.LogWarning("[Gate Replace] Failed: block is null.");
                return false;
            }

            if (EquippedBuildable == null)
            {
                Debug.LogWarning("[Gate Replace] Failed: EquippedBuildable is null.");
                return false;
            }

            if (EquippedBuildable.Prefab == null)
            {
                Debug.LogWarning("[Gate Replace] Failed: equipped prefab is null.");
                return false;
            }

            Gate gateComponent =
                EquippedBuildable.Prefab.GetComponent<Gate>();

            Debug.Log(
                $"[Gate Replace] Gate component on prefab root: " +
                $"{(gateComponent != null ? gateComponent.GetType().Name : "none")}");

            if (gateComponent == null)
            {
                Debug.LogWarning(
                    $"[Gate Replace] Failed: prefab {EquippedBuildable.Prefab.name} " +
                    $"does not have Gate on the root object.");

                return false;
            }

            Debug.Log(
                $"[Gate Replace] Block HasBuildable: {block.HasBuildable}, " +
                $"CurrentBuildable: {(block.CurrentBuildable != null ? block.CurrentBuildable.name : "null")}, " +
                $"Current type: {(block.CurrentBuildable != null ? block.CurrentBuildable.GetType().Name : "null")}");

            if (block.CurrentBuildable is not FencePost2D existingFence)
            {
                Debug.LogWarning(
                    "[Gate Replace] Failed: CurrentBuildable is not FencePost2D.");

                return false;
            }

            Debug.Log(
                $"[Gate Replace] Existing fence found: {existingFence.name}, " +
                $"Type: {existingFence.GetType().Name}, " +
                $"IsGate: {existingFence.IsGate}");

            if (existingFence.IsGate)
            {
                Debug.LogWarning(
                    "[Gate Replace] Failed: existing object is already a gate.");

                return false;
            }

            fenceToReplace = existingFence;

            Debug.Log(
                $"[Gate Replace] Success: {existingFence.name} can be replaced " +
                $"with {EquippedBuildable.Prefab.name}.");

            return true;
        }

        private void RefreshNeighborFencePosts(GrowBlock block)
        {
            TryRefreshFencePost(block, Vector2Int.up);
            TryRefreshFencePost(block, Vector2Int.down);
            TryRefreshFencePost(block, Vector2Int.left);
            TryRefreshFencePost(block, Vector2Int.right);
        }

        private void TryRefreshFencePost(GrowBlock block, Vector2Int direction)
        {
            GrowBlock neighbor = GridManager.Instance.GetNeighbor(block, direction);

            if (neighbor == null)
                return;

            if (neighbor.CurrentBuildable is FencePost2D fencePost)
                fencePost.RefreshConnections(neighbor);
        }

        #endregion

        #region Interact

        private void TryInteract()
        {
            if (!controlsEnabled)
                return;
            
            Debug.Log("Trying to interact");

            IInteractable closestInteractable = GetClosestInteractable();

            if (closestInteractable == null)
                return;

            closestInteractable.Interact(this);
        }

        private void TryContinuousInteract()
        {
            if (!controlsEnabled)
                return;

            IInteractable closestInteractable = GetClosestInteractable();

            if (closestInteractable == null)
                return;

            controllerToolTargeting = false;

            if (closestInteractable is not IContinuousInteractable continuousInteractable)
                return;

            continuousInteractable.ContinuousInteract(this);
        }
        private IInteractable GetClosestInteractable()
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                interactRadius,
                interactLayer);

            IInteractable closestInteractable = null;
            float closestDistance = float.MaxValue;

            foreach (Collider hit in hits)
            {
                if (!hit.TryGetComponent(out IInteractable interactable))
                    continue;

                float distance =
                    (hit.transform.position - transform.position).sqrMagnitude;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }

            return closestInteractable;
        }

        private void UpdateInteractableHighlight()
        {
            IInteractable closestInteractable = GetClosestInteractable();

            if (closestInteractable == currentHighlightedInteractable)
                return;

            currentHighlightedInteractable?.Highlight(false);

            currentHighlightedInteractable = closestInteractable;

            currentHighlightedInteractable?.Highlight(true);
        }

        #endregion

        #region Grass

        public void TryCutGrass(Vector3 hitPoint)
        {
            if (GrassCutter == null)
                return;

            GrassCutter.CutGrass(LastFacingDir);
        }

        #endregion

        #region Grid Lookup

        public GrowBlock GetBlock()
        {
            bool usingController =
                PlayerInput != null &&
                PlayerInput.currentControlScheme == "Gamepad";

            return usingController
                ? GridManager.Instance.GetBlockController()
                : GridManager.Instance.GetBlock();
        }

        #endregion

        #region Ghost

        public void ShowBuildingGhost(BuildingSO building, bool allowDragPlacement)
        {
            if (building == null || building.Prefab == null)
                return;

            DestroyGhost();
            ResetBuildDrag();

            EquippedBuildable = building;
            commandDragPlacementEnabled = allowDragPlacement;
            buildingPlacementActive = true;
        }
        
        private void CreateGhost()
        {
            if (!buildingPlacementActive)
                return;

            if (!InManagementState && !InCommanderMode)
                return;

            if (ghostInstance != null)
                return;

            if (EquippedBuildable == null || EquippedBuildable.Prefab == null)
                return;

            ghostInstance = Instantiate(EquippedBuildable.Prefab);

            ghostRenderers =
                ghostInstance.GetComponentsInChildren<MeshRenderer>(true);

            ghostSpriteRenderers =
                ghostInstance.GetComponentsInChildren<SpriteRenderer>(true);

            ApplyGhostMaterial();
        }

        public void CancelBuildingPlacement()
        {
            buildingPlacementActive = false;
            commandDragPlacementEnabled = false;
            EquippedBuildable = null;

            ResetBuildDrag();
            DestroyGhost();
        }

        private void ApplyGhostMaterial()
        {
            if (EquippedBuildable == null || EquippedBuildable.GhostMaterial == null)
                return;

            if (ghostRenderers != null)
            {
                foreach (MeshRenderer renderer in ghostRenderers)
                {
                    if (renderer == null)
                        continue;

                    renderer.sharedMaterial = EquippedBuildable.GhostMaterial;
                }
            }

            if (ghostSpriteRenderers != null)
            {
                foreach (SpriteRenderer spriteRenderer in ghostSpriteRenderers)
                {
                    if (spriteRenderer == null)
                        continue;

                    spriteRenderer.sharedMaterial = EquippedBuildable.GhostMaterial;
                }
            }
        }

        private void HandleGhost()
        {
            if (ghostInstance == null || inputReader == null)
                return;

            Ray cameraRay =
                Helpers.Camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, floorLayer))
                return;

            GrowBlock growBlock = GridManager.Instance.GetBlock();

            if (growBlock != null)
                ghostInstance.transform.position = growBlock.transform.position;
            else
                ghostInstance.transform.position = hit.point;

            bool isFenceGhost = ghostInstance.TryGetComponent(out FencePost2D fencePostGhost);

            if (isFenceGhost && growBlock != null)
            {
                fencePostGhost.RefreshGhostConnections(growBlock);
            }
            else if (isFenceGhost)
            {
                fencePostGhost.ShowDefaultVisual();
            }

            // HandleBuildRotationInput();

            if (isFenceGhost)
            {
                ghostInstance.transform.rotation = Quaternion.identity;
            }
            else
            {
                ghostInstance.transform.rotation =
                    Quaternion.Euler(EquippedBuildable.XRotation, currentBuildYRotation, 0f);
            }

            UpdateGhostColor();
        }

        // private void HandleBuildRotationInput()
        // {
        //     if (!InManagementState)
        //         return;

        //     if (Keyboard.current == null)
        //         return;

        //     if (Keyboard.current.qKey.wasPressedThisFrame)
        //         currentBuildYRotation -= buildRotationStep;

        //     if (Keyboard.current.eKey.wasPressedThisFrame)
        //         currentBuildYRotation += buildRotationStep;

        //     currentBuildYRotation = Mathf.Repeat(currentBuildYRotation, 360f);
        // }

        private void UpdateGhostColor()
        {
            if (EquippedBuildable == null)
                return;

            BaseBuilding buildable =
                EquippedBuildable.Prefab.GetComponent<BaseBuilding>();

            if (buildable == null)
                return;

            GrowBlock hoveredBlock = GetBlock();

            bool blockedBySameBuildable =
                hoveredBlock != null &&
                hoveredBlock.CurrentBuildable != null &&
                hoveredBlock.CurrentBuildable.UnitSO != null &&
                hoveredBlock.CurrentBuildable.UnitSO.ItemID == EquippedBuildable.ItemID;

            bool canBuildOnBlock =
                hoveredBlock != null &&
                CanBuildOnBlock(hoveredBlock);

            bool allRestrictionsPass =
                buildable.AllRestrictionsPass() &&
                hoveredBlock != null &&
                CanBuildOnBlock(hoveredBlock);

            // Preserve your previous same-buildable behavior if it is intentional.
            if (blockedBySameBuildable)
                allRestrictionsPass = true;

            Color tintColor =
                allRestrictionsPass
                    ? availableToPlaceTintColor
                    : errorTintColor;

            float sineGlowMin =
                allRestrictionsPass
                    ? availableSineGlowMin
                    : errorSineGlowMin;

            Color sineGlowColor =
                allRestrictionsPass
                    ? availableSineGlowColor
                    : errorSineGlowColor;

            Color hologramTintColor =
                allRestrictionsPass
                    ? availableHologramTintColor
                    : errorHologramTintColor;

            if (ghostRenderers != null)
            {
                foreach (MeshRenderer renderer in ghostRenderers)
                {
                    if (renderer == null)
                        continue;

                    ApplyBuildGhostPropertyBlock(
                        renderer,
                        tintColor,
                        sineGlowMin,
                        sineGlowColor,
                        hologramTintColor);
                }
            }

            if (ghostSpriteRenderers == null)
                return;

            foreach (SpriteRenderer spriteRenderer in ghostSpriteRenderers)
            {
                if (spriteRenderer == null)
                    continue;

                ApplyBuildGhostPropertyBlock(
                    spriteRenderer,
                    tintColor,
                    sineGlowMin,
                    sineGlowColor,
                    hologramTintColor);
            }
        }

        private void ApplyBuildGhostPropertyBlock(
            Renderer renderer,
            Color tintColor,
            // Color fresnelColor,
            float sineGlowMin,
            Color sineGlowColor,
            Color hologramTintColor)
        {
            if (renderer == null)
                return;

            renderer.GetPropertyBlock(ghostPropertyBlock);

            ghostPropertyBlock.SetColor(TINT, tintColor);
            // ghostPropertyBlock.SetColor(FRESNEL, fresnelColor);
            ghostPropertyBlock.SetFloat(SINE_GLOW_MIN, sineGlowMin);
            ghostPropertyBlock.SetColor(SINE_GLOW_COLOR, sineGlowColor);
            ghostPropertyBlock.SetColor(HOLOGRAM_TINT, hologramTintColor);

            renderer.SetPropertyBlock(ghostPropertyBlock);
        }

        public void DestroyGhost()
        {
            if (ghostInstance == null)
                return;

            Destroy(ghostInstance);

            ghostInstance = null;
            ghostRenderers = null;
            ghostSpriteRenderers = null;
        }

        #endregion

        #region Damage / Animation

        public override void DamageEffect(bool knockback, Transform attacker = null)
        {
            base.DamageEffect(knockback, attacker);
        }

        public void AnimationTrigger()
        {
            StateMachine.CurrentState.AnimationFinishedTrigger();
        }

        public void Heal(int healAmount)
        {
            if (Health == null)
                return;

            Health.Heal(healAmount);

            if (Fx != null)
                Fx.CreatePopUpText(healAmount.ToString(), Color.green);
        }

        #endregion

        #region Save / Load

        public void LoadData(GameData data)
        {
            if (data.playerPosition != Vector3.zero)
                transform.position = data.playerPosition;
        }

        public void SaveData(ref GameData data)
        {
            data.playerPosition = transform.position;
        }

        #endregion
    }
}