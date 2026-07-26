using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputReader : MonoBehaviour
    {
        public event Action<Vector2> MoveChanged;
        public event Action InteractPressed;
        public event Action InteractReleased;
        public event Action AttackPressed;
        public event Action AttackReleased;
        public event Action CancelPressed;

        public Vector2 MoveInput { get; private set; }
        public Vector2 PointerPosition { get; private set; }

        public bool InteractHeld { get; private set; }

        public bool AttackHeld =>
            attackAction != null &&
            attackAction.IsPressed();

        public bool AttackPressedThisFrame =>
            attackAction != null &&
            attackAction.WasPressedThisFrame();

        public bool AttackReleasedThisFrame =>
            attackAction != null &&
            attackAction.WasReleasedThisFrame();

        public PlayerInput PlayerInput { get; private set; }

        private InputAction attackAction;
        private InputAction interactAction;

        private bool previousAttackHeld;
        private bool previousInteractHeld;

        private void Awake()
        {
            PlayerInput = GetComponent<PlayerInput>();

            if (PlayerInput == null || PlayerInput.actions == null)
                return;

            attackAction = PlayerInput.actions["Attack"];
            interactAction = PlayerInput.actions["Interact"];
        }

        private void Update()
        {
            UpdateHeldInputs();
        }

        private void UpdateHeldInputs()
        {
            UpdateInteractInput();
            UpdateAttackInput();
        }

        private void UpdateInteractInput()
        {
            if (interactAction == null)
                return;

            InteractHeld = interactAction.IsPressed();

            if (InteractHeld && !previousInteractHeld)
                InteractPressed?.Invoke();

            if (!InteractHeld && previousInteractHeld)
                InteractReleased?.Invoke();

            previousInteractHeld = InteractHeld;
        }

        private void UpdateAttackInput()
        {
            if (attackAction == null)
                return;

            bool attackHeld = AttackHeld;

            if (attackHeld && !previousAttackHeld)
                AttackPressed?.Invoke();

            if (!attackHeld && previousAttackHeld)
                AttackReleased?.Invoke();

            previousAttackHeld = attackHeld;
        }

        public void OnMove(InputValue value)
        {
            MoveInput = value.Get<Vector2>();
            MoveChanged?.Invoke(MoveInput);
        }

        public void OnInteract(InputValue value)
        {
            // UpdateHeldInputs handles press, held, and release.
        }

        public void OnAttack(InputValue value)
        {
            // UpdateHeldInputs handles press, held, and release.
        }

        public void OnCancel(InputValue value)
        {
            if (value.isPressed)
                CancelPressed?.Invoke();
        }

        public void OnPointerPosition(InputValue value)
        {
            PointerPosition = value.Get<Vector2>();
        }

        public void ClearMoveInput()
        {
            MoveInput = Vector2.zero;
            MoveChanged?.Invoke(Vector2.zero);
        }

        public void ClearHeldInputs()
        {
            InteractHeld = false;

            previousInteractHeld = false;
            previousAttackHeld = false;
        }
    }
}