using ShiftedSignal.Garden.Effects;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Managers;
using UnityEngine.InputSystem;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.EventBus;

using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    public enum ToolType
    {
        Plough,
        Blood,
        Seeds,
        Basket
    }
    
    public class Player : Entity
    {   
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

        public PlayerInput playerInput { get; private set; }

        #endregion

        #region === Components & References ===

        [Header("Components")]
        public TerrainGrassCutter GrassCutter;
        [SerializeField] private LayerBasedParticleSpawner ParticleSpawner;

        [Header("Transforms")]
        public Transform ToolIndicator;
        public Transform GrowBlockCheck;

        [Header("Settings")]
        public float GrowBlockCheckDistance;


        #endregion

        #region === Equipment ===

        [Header("Equipment")]
        // public WeaponData ActiveWeapon;
        public ItemData_Equipment EquippedWeapon;

#region Input Buffers
    [HideInInspector] public bool AttackBuffered = false;
#endregion
        
        

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
            base.Awake();

            StateMachine = new PlayerStateMachine();
            IdleState = new PlayerIdleState(this, StateMachine, "Idle");
            MoveState = new PlayerMoveState(this, StateMachine, "Move");
            ManagementState = new PlayerManagementState(this, StateMachine, "Idle");
            AttackState = new PlayerAttackState(this, StateMachine, "Attack");

            playerInput = GetComponent<PlayerInput>();
            
        }

        protected override void Start()
        {
            base.Start();
            StateMachine.Initialize(IdleState);
        }

        private void OnEnable()
        {
            playerInput.onControlsChanged += OnControlsChanged;
            Bus<WeaponEquipEvent>.OnEvent += HandleWeaponEquipped;
            Bus<ToolEquipEvent>.OnEvent += HandleToolEquipped;
        }


        private void OnDisable()
        {
            playerInput.onControlsChanged -= OnControlsChanged;
            Bus<WeaponEquipEvent>.OnEvent += HandleWeaponEquipped;
            Bus<ToolEquipEvent>.OnEvent -= HandleToolEquipped;
        }

        private void HandleToolEquipped(ToolEquipEvent evt)
        {
            CurrentTool = evt.Tool;
        }

        private void HandleWeaponEquipped(WeaponEquipEvent evt)
        {
            EquippedWeapon = evt.Weapon;
        }


        protected override void Update()
        {
            base.Update();

            StateMachine.CurrentState?.Update();

            if (actionInput.action.WasPressedThisFrame())
            {
                UseTool();
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

        private void OnControlsChanged(PlayerInput input)
        {
            Debug.Log("Control scheme changed to: " + input.currentControlScheme);
        }

        private void HandleDebugInputs()
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                CurrentTool++;
                if ((int)CurrentTool >= 4)
                    CurrentTool = ToolType.Plough;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
                UseTool();

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                

            if (Keyboard.current.numpad1Key.wasPressedThisFrame)
                CurrentTool = ToolType.Plough;

            if (Keyboard.current.numpad2Key.wasPressedThisFrame)
                CurrentTool = ToolType.Blood;

            if (Keyboard.current.numpad3Key.wasPressedThisFrame)
                CurrentTool = ToolType.Seeds;

            if (Keyboard.current.oem4Key.wasPressedThisFrame)
                CurrentTool = ToolType.Plough;
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
            GrowBlockCheck.transform.position =
                transform.position +
                FacingDir * GrowBlockCheckDistance +
                Vector3.up * CheckHeight;
        }

        #endregion

        #region === Tool Logic ===

        private void UseTool()
        {
            GrowBlock block = GetBlock();
            if (block == null) return;

            switch (CurrentTool)
            {
                case ToolType.Plough:
                    block.PloughSoil();
                    break;
                case ToolType.Blood:
                    block.WaterSoil();
                    break;
                case ToolType.Seeds:
                    block.PlantCrop();
                    break;
                case ToolType.Basket:
                    block.HarvestCrop();
                    break;
            }
        }

        public GrowBlock GetBlock()
        {
            bool usingController = playerInput.currentControlScheme == "Gamepad";

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
            // fx.StartCoroutine(nameof(fx.FlashFX));
            // fx.NewFlashFX();
            base.DamageEffect(Knockback, Attacker);
        }

        #endregion

        #region === Animation ===

        public void AnimationTrigger()
        {
            StateMachine.CurrentState.AnimationFinishedTrigger();
        }

        #endregion
    }
}