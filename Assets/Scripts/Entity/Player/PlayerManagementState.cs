using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
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
            Debug.Log("In Management state");
        }

        public override void Update()
        {
            base.Update();

            if (Keyboard.current == null)
            {
                
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscapePressed();
            }
        }

        private void HandleEscapePressed()
        {

            Player.CancelBuildingPlacement();

            Bus<ReturnToConstructionMenuEvent>.Raise(
                new ReturnToConstructionMenuEvent());

            TryExitManagementMode();
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