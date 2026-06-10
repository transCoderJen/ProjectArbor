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

        public PlayerInput PlayerInput { get; private set; }

        private void Awake()
        {
            PlayerInput = GetComponent<PlayerInput>();
        }

        #region Input Actions

        public void OnMove(InputValue value)
        {
            MoveInput = value.Get<Vector2>();
            MoveChanged?.Invoke(MoveInput);
        }

        public void OnInteract(InputValue value)
        {
            if (!value.isPressed)
                return;

            InteractPressed?.Invoke();
        }

        public void OnAction(InputValue value)
        {
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

        /// <summary>
        /// Mouse position for building placement.
        /// This should be bound to:
        /// Mouse/Position
        /// Pointer/Position
        /// </summary>
        public void OnPointerPosition(InputValue value)
        {
            PointerPosition = value.Get<Vector2>();
        }

        #endregion

        #region Helpers

        public void ClearMoveInput()
        {
            MoveInput = Vector2.zero;
            MoveChanged?.Invoke(Vector2.zero);
        }

        #endregion
    }
}