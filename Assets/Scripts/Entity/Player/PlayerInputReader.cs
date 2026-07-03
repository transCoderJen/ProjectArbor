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
        public event Action ActionPressed;
        public event Action AttackPressed;
        public event Action CancelPressed;

        public Vector2 MoveInput { get; private set; }
        public Vector2 PointerPosition { get; private set; }

        public bool InteractHeld { get; private set; }
        public bool ActionHeld { get; private set; }
        public bool AttackHeld =>
            PlayerInput != null &&
            PlayerInput.actions != null &&
            PlayerInput.actions["Attack"].IsPressed();

        public PlayerInput PlayerInput { get; private set; }

        private InputAction attackAction;
        private bool previousAttackHeld;
        private InputAction interactAction;
        private bool previousInteractHeld;

        private void Awake()
        {
            PlayerInput = GetComponent<PlayerInput>();

            if (PlayerInput != null && PlayerInput.actions != null)
            {
                attackAction = PlayerInput.actions["Attack"];
                interactAction = PlayerInput.actions["Interact"];
            }
        }

        private void Update()
        {
            UpdateHeldInputs();
        }

        private void UpdateHeldInputs()
        {
            if (interactAction != null)
            {
                InteractHeld = interactAction.IsPressed();

                if (InteractHeld && !previousInteractHeld)
                {
                    InteractPressed?.Invoke();
                    ActionPressed?.Invoke();
                }

                previousInteractHeld = InteractHeld;
            }

            if (attackAction == null)
                return;

            if (AttackHeld && !previousAttackHeld)
                AttackPressed?.Invoke();

            previousAttackHeld = AttackHeld;
        }

        public void OnMove(InputValue value)
        {
            MoveInput = value.Get<Vector2>();
            MoveChanged?.Invoke(MoveInput);
        }

        public void OnInteract(InputValue value)
        {
            // Let UpdateHeldInputs handle InteractHeld.
        }

        public void OnAction(InputValue value)
        {
            ActionHeld = value.isPressed;

            if (!value.isPressed)
                return;

            ActionPressed?.Invoke();
        }

        public void OnAttack(InputValue value)
        {
            if (!value.isPressed)
                return;

            AttackPressed?.Invoke();
        }

        public void OnCancel(InputValue value)
        {
            if (!value.isPressed)
                return;

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
            ActionHeld = false;
            previousAttackHeld = false;
        }
    }
}