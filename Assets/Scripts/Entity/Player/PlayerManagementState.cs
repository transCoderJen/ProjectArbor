using ShiftedSignal.Garden.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    public class PlayerManagementState : PlayerState
    {
        public PlayerManagementState(
            Player player,
            PlayerStateMachine stateMachine,
            string animBoolName)
            : base(player, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Time.timeScale = 0f;

            CameraManager.Instance.SwitchCamera(
                CameraManager.VirtualCameraType.FreeLook);

            Player.StopMovement();
            Player.InManagementState = true;
        }

        public override void Update()
        {
            base.Update();

            if (Keyboard.current != null &&
                Keyboard.current.gKey.wasPressedThisFrame)
            {
                TryExitManagementMode();
                return;
            }

            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TryExitManagementMode();
                return;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            Player.StopMovement();
        }

        public override void Exit()
        {
            base.Exit();

            Player.InManagementState = false;
            Player.DestroyGhost();

            Time.timeScale = 1f;

            CameraManager.Instance.ResetOffsetsAndSwitchCamera(
                CameraManager.VirtualCameraType.Player);
        }

        private void TryExitManagementMode()
        {
            if (CameraIsBusy())
                return;

            StateMachine.ChangeState(Player.IdleState);
        }

        private bool CameraIsBusy()
        {
            return CameraManager.Instance != null &&
                   CameraManager.Instance.IsTransitioning;
        }
    }
}