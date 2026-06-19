using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    public class PlayerIdleState : PlayerState
    {
        public PlayerIdleState(
            Player _player,
            PlayerStateMachine _stateMachine,
            string _animBoolName)
            : base(_player, _stateMachine, _animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Player.StopMovement();
        }

        public override void Update()
        {
            base.Update();

            if (CameraIsBusy())
                return;

            if (Player.AttackBuffered)
            {
                Player.AttackBuffered = false;
                Player.StateMachine.ChangeState(Player.AttackState);
                return;
            }

            if (CachedMoveInput != Vector2.zero)
            {
                Player.StateMachine.ChangeState(Player.MoveState);
            }
        }

        private bool CameraIsBusy()
        {
            return CameraManager.Instance != null &&
                   CameraManager.Instance.IsTransitioning;
        }
    }
}