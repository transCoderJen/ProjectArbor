using ShiftedSignal.Garden.Managers;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    public class PlayerCommanderState : PlayerState
    {
        public PlayerCommanderState(
            Player player,
            PlayerStateMachine stateMachine,
            string animBoolName)
            : base(player, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Player.StopMovement();
            Player.DestroyGhost();

            PlayerCommanderController commanderController =
                Player.GetComponent<PlayerCommanderController>();

            if (commanderController != null)
                commanderController.EnterCommanderMode();

            CameraManager.Instance.SwitchCamera(
                CameraManager.VirtualCameraType.FreeLook);
        }

        public override void Update()
        {
            base.Update();

            Player.StopMovement();

            if (Keyboard.current != null &&
                Keyboard.current.hKey.wasPressedThisFrame)
            {
                TryExitCommanderMode();
                return;
            }

            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (Player.IsPlacingBuilding)
                {
                    Player.CancelBuildingPlacement();
                }
                else
                {
                    TryExitCommanderMode();
                    return;
                }
            }

            PlayerCommanderController commanderController =
                Player.GetComponent<PlayerCommanderController>();

            if (commanderController != null)
                commanderController.HandleCommanderUpdate();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            Player.StopMovement();
        }

        public override void Exit()
        {
            base.Exit();

            PlayerCommanderController commanderController =
                Player.GetComponent<PlayerCommanderController>();

            if (commanderController != null)
                commanderController.ExitCommanderMode();

            CameraManager.Instance.ResetOffsetsAndSwitchCamera(
                CameraManager.VirtualCameraType.Player);
        }

        private void TryExitCommanderMode()
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