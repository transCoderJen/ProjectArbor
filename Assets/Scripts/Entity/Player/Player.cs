using ShiftedSignal.Garden.Effects;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Interfaces;

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

    public class Player : Entity, IHealable
    {
        public static Player Instance { get; private set; }

        [Header("Attack Details")]
        public Vector2[] AttackMovement;
        public float CounterAttackDuration;

        #region === Input ===

        [Header("Input")]
        [SerializeField] private InputActionReference actionInput;
        public InputActionReference ActionInput => actionInput;

        [SerializeField] private InputActionReference moveInput;
        public InputActionReference MoveInput => moveInput;

        [SerializeField] private InputActionReference attackInput;
        public InputActionReference AttackInput => attackInput;

        [SerializeField] private InputActionReference interactInput;
        public InputActionReference InteractInput => interactInput;

        public PlayerInput PlayerInput { get; private set; }

        #endregion

        #region === Components & References ===

        [Header("Components")]
        public TerrainGrassCutter GrassCutter;
        [SerializeField] private LayerBasedParticleSpawner particleSpawner;

        [Header("Transforms")]
        public Transform ToolIndicator;
        public Transform GrowBlockCheck;

        [Header("Settings")]
        public float GrowBlockCheckDistance;

        #endregion

        #region === Equipment ===

        [Header("Equipment")]
        public ItemData_Equipment EquippedWeapon;
        public ItemData_Seed EquippedSeed;

        [Header("Interact")]
        [SerializeField] private float interactRadius = 2f;
        [SerializeField] private LayerMask interactLayer;

        [HideInInspector] public bool AttackBuffered = false;

        public ToolType CurrentTool;

        #endregion

        #region === State Machine ===

        [Header("State Machine")]
        public PlayerStateMachine StateMachine { get; private set; }
        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerManagementState ManagementState { get; private set; }
        public PlayerAttackState AttackState { get; private set; }

        public Vector2 CachedMoveInput;

        #endregion

        #region === Unity Lifecycle ===

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            base.Awake();

            StateMachine = new PlayerStateMachine();
            IdleState = new PlayerIdleState(this, StateMachine, "Idle");
            MoveState = new PlayerMoveState(this, StateMachine, "Move");
            ManagementState = new PlayerManagementState(this, StateMachine, "Idle");
            AttackState = new PlayerAttackState(this, StateMachine, "Attack");

            PlayerInput = GetComponent<PlayerInput>();
        }

        protected override void Start()
        {
            base.Start();
            StateMachine.Initialize(IdleState);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (PlayerInput == null)
                PlayerInput = GetComponent<PlayerInput>();

            if (PlayerInput != null)
                PlayerInput.onControlsChanged += OnControlsChanged;

            Bus<WeaponEquipEvent>.OnEvent += HandleWeaponEquipped;
            Bus<ToolEquipEvent>.OnEvent += HandleToolEquipped;
            Bus<SeedEquipEvent>.OnEvent += HandleSeedEquipped;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (PlayerInput != null)
                PlayerInput.onControlsChanged -= OnControlsChanged;

            Bus<WeaponEquipEvent>.OnEvent -= HandleWeaponEquipped;
            Bus<ToolEquipEvent>.OnEvent -= HandleToolEquipped;
            Bus<SeedEquipEvent>.OnEvent -= HandleSeedEquipped;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        protected override void Update()
        {
            base.Update();

            StateMachine.CurrentState?.Update();

            if (actionInput != null && actionInput.action.WasPressedThisFrame())
            {
                UseTool();
            }

            if (Keyboard.current != null && Keyboard.current.kKey.isPressed)
            {
                GridInfo.Instance.GrowCrop();
            }

            HandleDebugInputs();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            StateMachine.CurrentState?.FixedUpdate();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateGrowBlockCheckPosition();
        }

        #endregion

        #region === Input Handling ===

        private void OnInteract(InputValue value)
        {
            TryInteract();
        }

        private void OnControlsChanged(PlayerInput input)
        {
        }

        private void HandleDebugInputs()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.eKey.wasPressedThisFrame)
                UseTool();

            if (Keyboard.current.numpad1Key.wasPressedThisFrame)
                CurrentTool = ToolType.Plough;

            if (Keyboard.current.numpad2Key.wasPressedThisFrame)
                CurrentTool = ToolType.Blood;

            if (Keyboard.current.numpad3Key.wasPressedThisFrame)
                CurrentTool = ToolType.Seeds;

            if (Keyboard.current.oem4Key.wasPressedThisFrame)
                CurrentTool = ToolType.Basket;
        }

        #endregion

        #region === Event Handlers ===

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

        #region === Movement ===

        public override void ApplyMovement(Vector2 input, bool normalized = true)
        {
            base.ApplyMovement(input, normalized);
            UpdateGrowBlockCheckPosition();
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

        #region === Tool Logic ===

        private void UseTool()
        {
            GrowBlock block = GetBlock();

            if (block == null)
                return;

            block.UseContextAction(EquippedSeed);
        }

        private void TryInteract()
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                interactRadius,
                interactLayer);

            foreach (Collider hit in hits)
            {
                if (hit.TryGetComponent(out IInteractable interactable))
                {
                    if (interactable.IsPlayerNear())
                    {
                        interactable.Interact(this);
                        break;
                    }
                }
            }
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

        public void TryCutGrass(Vector3 hitPoint)
        {
            GrassCutter.CutGrass(LastFacingDir);
        }

        #endregion

        #region === Effects ===

        public override void DamageEffect(bool Knockback, Transform Attacker = null)
        {
            base.DamageEffect(Knockback, Attacker);
        }

        #endregion

        #region === Animation ===

        public void AnimationTrigger()
        {
            StateMachine.CurrentState.AnimationFinishedTrigger();
        }

        public void Heal(int HealAmount)
        {
            Stats.IncreaseHealthBy(HealAmount);
            Fx.CreatePopUpText(HealAmount.ToString(), Color.blue);
        }

        #endregion
    }
}