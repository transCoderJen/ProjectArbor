using ShiftedSignal.Garden.Effects;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.SaveAndLoad;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Misc;
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

        [Header("Attack Details")]
        public Vector2[] AttackMovement;
        public float CounterAttackDuration;

        [Header("Progression")]
        [SerializeField] private PlayerProgression progression;

        public int AttackDamage =>
            progression != null ? progression.CurrentWeaponDamage : 1;

        [Header("Input")]
        [SerializeField] private PlayerInputReader inputReader;
        public PlayerInputReader InputReader => inputReader;
        public PlayerInput PlayerInput => inputReader != null ? inputReader.PlayerInput : null;

        [Header("Components")]
        public TerrainGrassCutter GrassCutter;
        [SerializeField] private LayerBasedParticleSpawner particleSpawner;

        [Header("Transforms")]
        public Transform ToolIndicator;
        public Transform GrowBlockCheck;

        [Header("Settings")]
        public float GrowBlockCheckDistance;

        [Header("Seed")]
        public ItemData_Seed EquippedSeed;

        [Header("Water Blocking")]
        [SerializeField] private Terrain terrain;
        [SerializeField] private float waterLevelY = 0f;
        [SerializeField] private float waterBorderPadding = 0.25f;

        [Header("Interact")]
        [SerializeField] private float interactRadius = 2f;
        [SerializeField] private LayerMask interactLayer;
        [SerializeField] private LayerMask floorLayer;

        [HideInInspector] public bool AttackBuffered = false;

        public ToolType CurrentTool;

        [SerializeField] private BuildableData EquippedBuildable;

        [Header("Farming")]
        [SerializeField] private float blockInteractRadius = 3f;
        public bool InManagementState = false;

        [Header("State Machine")]
        public PlayerStateMachine StateMachine { get; private set; }
        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerManagementState ManagementState { get; private set; }
        public PlayerAttackState AttackState { get; private set; }

        public Vector2 CachedMoveInput { get; private set; }
        public bool UsingController { get; private set; }

        [Header("Building Placement Colors")]
        [SerializeField] [ColorUsage(showAlpha: true, hdr: true)]
        private Color errorTintColor = Color.red;

        [SerializeField] [ColorUsage(showAlpha: true, hdr: true)]
        private Color errorFresnelColor = new(4, 1.7f, 0, 2);

        [SerializeField] [ColorUsage(showAlpha: true, hdr: true)]
        private Color availableToPlaceTintColor = new(0.2f, 0.65f, 1, 2);

        [SerializeField] [ColorUsage(showAlpha: true, hdr: true)]
        private Color availableToPlaceFresnelColor = new(.02f, 0.65f, 1, 2);

        [SerializeField] private float buildRotationStep = 90f;

        private float currentBuildYRotation;

        private GameObject ghostInstance;
        private MeshRenderer[] ghostRenderers;

        private static readonly int TINT = Shader.PropertyToID("_Tint");
        private static readonly int FRESNEL = Shader.PropertyToID("_FresnelColor");

        private bool controlsEnabled = true;
        public bool ControlsEnabled => controlsEnabled;

        private IInteractable currentHighlightedInteractable;

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

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
        }

        protected override void Start()
        {
            base.Start();

            if (PlayerInput != null)
                UsingController = PlayerInput.currentControlScheme == "Gamepad";

            StateMachine.Initialize(IdleState);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (inputReader == null)
                inputReader = GetComponent<PlayerInputReader>();

            if (inputReader != null)
            {
                inputReader.MoveChanged += HandleMoveChanged;
                inputReader.InteractPressed += TryInteract;
                inputReader.ActionPressed += UseTool;
                inputReader.AttackPressed += HandleAttackInput;
                inputReader.CancelPressed += HandleCancelInput;

                if (inputReader.PlayerInput != null)
                    inputReader.PlayerInput.onControlsChanged += OnControlsChanged;
            }

            Bus<ToolEquipEvent>.OnEvent += HandleToolEquipped;
            Bus<SeedEquipEvent>.OnEvent += HandleSeedEquipped;
            Bus<EnablePlayerMovementEvent>.OnEvent += HandleEnablePlayerMovement;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (inputReader != null)
            {
                inputReader.MoveChanged -= HandleMoveChanged;
                inputReader.InteractPressed -= TryInteract;
                inputReader.ActionPressed -= UseTool;
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

            UpdateInteractableHighlight();

            HandleDebugInputs();

            CreateGhost();
            HandleGhost();
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
            if (!controlsEnabled)
                return;

            AttackBuffered = true;
        }

        private void HandleCancelInput()
        {
            if (!controlsEnabled)
                return;

            DestroyGhost();
        }

        private void OnControlsChanged(PlayerInput input)
        {
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
            }
        }

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

                if (StateMachine.CurrentState == ManagementState)
                    StateMachine.ChangeState(IdleState);
                else
                    StateMachine.ChangeState(ManagementState);
            }

            if (Keyboard.current.kKey.isPressed)
                GridInfo.Instance.GrowCrop();
#endif
        }

        private void HandleSeedEquipped(SeedEquipEvent evt)
        {
            EquippedSeed = evt.Seed;
        }

        private void HandleToolEquipped(ToolEquipEvent evt)
        {
            CurrentTool = evt.Tool;
        }

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

        private void UseTool()
        {
            if (!controlsEnabled)
                return;

            if (InManagementState)
            {
                Build();
                return;
            }

            GrowBlock block = GetBlock();

            if (block == null)
                return;

            block.UseContextAction(EquippedSeed);
        }

        private void Build()
        {
            GrowBlock block = GetBlock();

            if (block == null || !block.IsActive)
                return;

            if (EquippedBuildable == null || EquippedBuildable.BuildablePrefab == null)
                return;

            if (!EquippedBuildable.CanAfford())
                return;

            GameObject builtObject = Instantiate(
                EquippedBuildable.BuildablePrefab,
                block.transform.position,
                Quaternion.Euler(0f, currentBuildYRotation, 0f));

            builtObject.GetComponent<BaseBuildable>().Build();

            block.ResetBlock();
            block.HasBuildable = true;

            EquippedBuildable.RemoveRequiredMaterials();

            Bus<CurrencyUpdatedEvent>.Raise(
                new CurrencyUpdatedEvent(-EquippedBuildable.Cost));
        }

        private void TryInteract()
        {
            if (!controlsEnabled)
                return;

            IInteractable closestInteractable = GetClosestInteractable();

            if (closestInteractable == null)
                return;

            closestInteractable.Interact(this);
        }

        public void TryCutGrass(Vector3 hitPoint)
        {
            if (GrassCutter == null)
                return;

            GrassCutter.CutGrass(LastFacingDir);
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

        public GrowBlock GetBlock()
        {
            bool usingController =
                PlayerInput != null &&
                PlayerInput.currentControlScheme == "Gamepad";

            return usingController
                ? GridManager.Instance.GetBlockController()
                : GridManager.Instance.GetBlock();
        }

        private void CreateGhost()
        {
            if (!InManagementState)
                return;

            if (ghostInstance != null)
                return;

            if (EquippedBuildable == null || EquippedBuildable.BuildablePrefab == null)
                return;

            ghostInstance = Instantiate(EquippedBuildable.BuildablePrefab);
            ghostRenderers = ghostInstance.GetComponentsInChildren<MeshRenderer>(true);
        }

        private void HandleGhost()
        {
            if (ghostInstance == null || inputReader == null)
                return;

            Ray cameraRay =
                Helpers.Camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, floorLayer))
            {
                GrowBlock growBlock = GridManager.Instance.GetBlock();

                if (growBlock != null)
                    ghostInstance.transform.position = growBlock.transform.position;
                else
                    ghostInstance.transform.position = hit.point;

                HandleBuildRotationInput();

                ghostInstance.transform.rotation =
                    Quaternion.Euler(0f, currentBuildYRotation, 0f);

                UpdateGhostColor();
            }
        }

        private void HandleBuildRotationInput()
        {
            if (!InManagementState)
                return;

            if (Keyboard.current == null)
                return;

            if (Keyboard.current.qKey.wasPressedThisFrame)
                currentBuildYRotation -= buildRotationStep;

            if (Keyboard.current.eKey.wasPressedThisFrame)
                currentBuildYRotation += buildRotationStep;

            currentBuildYRotation = Mathf.Repeat(currentBuildYRotation, 360f);
        }

        private void UpdateGhostColor()
        {
            if (EquippedBuildable == null || ghostRenderers == null || ghostRenderers.Length == 0)
                return;

            BaseBuildable buildable =
                EquippedBuildable.BuildablePrefab.GetComponent<BaseBuildable>();

            if (buildable == null)
                return;

            bool allRestrictionsPass = buildable.AllRestrictionsPass();

            Color tintColor =
                allRestrictionsPass ? availableToPlaceTintColor : errorTintColor;

            Color fresnelColor =
                allRestrictionsPass ? availableToPlaceFresnelColor : errorFresnelColor;

            foreach (MeshRenderer renderer in ghostRenderers)
            {
                if (renderer == null)
                    continue;

                foreach (Material material in renderer.materials)
                {
                    if (material == null)
                        continue;

                    if (material.HasProperty(TINT))
                        material.SetColor(TINT, tintColor);

                    if (material.HasProperty(FRESNEL))
                        material.SetColor(FRESNEL, fresnelColor);
                }
            }
        }

        public void DestroyGhost()
        {
            if (ghostInstance == null)
                return;

            Destroy(ghostInstance);

            ghostInstance = null;
            ghostRenderers = null;
        }

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

        public void LoadData(GameData data)
        {
            if (data.playerPosition != Vector3.zero)
                transform.position = data.playerPosition;
        }

        public void SaveData(ref GameData data)
        {
            data.playerPosition = transform.position;
        }
    }
}