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

        [Header("Equipment")]
        public ItemData_Equipment EquippedWeapon;
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

        private GameObject ghostInstance;
        private MeshRenderer ghostRenderer;

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
            {
                UsingController =
                    PlayerInput.currentControlScheme == "Gamepad";
            }

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

            Bus<WeaponEquipEvent>.OnEvent += HandleWeaponEquipped;
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

            Bus<WeaponEquipEvent>.OnEvent -= HandleWeaponEquipped;
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

        #region Input Events

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
            UsingController =
                input.currentControlScheme == "Gamepad";
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

        #endregion

        #region Legacy Debug Input

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

            // Toggle Management State
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (StateMachine.CurrentState == ManagementState)
                    StateMachine.ChangeState(IdleState);
                else
                    StateMachine.ChangeState(ManagementState);
            }

            if (Keyboard.current.kKey.isPressed)
                GridInfo.Instance.GrowCrop();
#endif
        }

        #endregion

        #region Event Handlers

        private void HandleSeedEquipped(SeedEquipEvent evt)
        {
            EquippedSeed = evt.Seed;
        }

        private void HandleToolEquipped(ToolEquipEvent evt)
        {
            CurrentTool = evt.Tool;
        }

        private void HandleWeaponEquipped(WeaponEquipEvent evt)
        {
            EquippedWeapon = evt.Weapon;
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
            if (terrain == null)
                return false;

            if (input == Vector2.zero)
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

        #region Tool / Build / Interact

        private void UseTool()
        {
            if (!controlsEnabled)
                return;

            if (InManagementState)
            {
                Build();
                return;
            }

            Debug.Log("Use tool");


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
                Quaternion.identity);

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

        #endregion

        #region Building Ghost

        private void CreateGhost()
        {
            if (!InManagementState)
                return;

            if (ghostInstance != null)
                return;

            if (EquippedBuildable == null || EquippedBuildable.BuildablePrefab == null)
                return;

            ghostInstance = Instantiate(EquippedBuildable.BuildablePrefab);
            ghostRenderer = ghostInstance.GetComponentInChildren<MeshRenderer>();
        }
        
        private void HandleGhost()
        {
            if (ghostInstance == null)
                return;

            if (inputReader == null)
                return;

            Ray cameraRay =
                    Helpers.Camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, floorLayer))
            {
                if (hit.collider.TryGetComponent(out GrowBlock growBlock))
                    ghostInstance.transform.position = growBlock.transform.position;
                else
                    ghostInstance.transform.position = hit.point;

                if (EquippedBuildable == null || ghostRenderer == null)
                    return;

                BaseBuildable buildable =
                    EquippedBuildable.BuildablePrefab.GetComponent<BaseBuildable>();

                if (buildable == null)
                    return;

                bool allRestrictionsPass = buildable.AllRestrictionsPass();

                ghostRenderer.material.SetColor(
                    TINT,
                    allRestrictionsPass ? availableToPlaceTintColor : errorTintColor);

                ghostRenderer.material.SetColor(
                    FRESNEL,
                    allRestrictionsPass ? availableToPlaceFresnelColor : errorFresnelColor);
            }
        }

        public void DestroyGhost()
        {
            if (ghostInstance == null)
                return;

            Destroy(ghostInstance);

            ghostInstance = null;
            ghostRenderer = null;
        }

        #endregion

        #region Effects / Animation / Save

        public override void DamageEffect(bool Knockback, Transform Attacker = null)
        {
            base.DamageEffect(Knockback, Attacker);
        }

        public void AnimationTrigger()
        {
            StateMachine.CurrentState.AnimationFinishedTrigger();
        }

        public void Heal(int HealAmount)
        {
            Stats.IncreaseHealthBy(HealAmount);
            Fx.CreatePopUpText(HealAmount.ToString(), Color.blue);
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

        #endregion
    }
}