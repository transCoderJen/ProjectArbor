using Unity.VisualScripting;
using UnityEngine;
using ShiftedSignal.Garden.Managers;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{    
    public class PlayerIdleState : PlayerState
    {
        public PlayerIdleState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
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

            if (Player.AttackBuffered)
            {
                Player.StateMachine.ChangeState(Player.AttackState);
            }

            if (Player.AttackInput.action.WasPressedThisFrame())
            {
                Player.StateMachine.ChangeState(Player.AttackState);   
            }
            
            if (CachedMoveInput != Vector2.zero)
            {
                Player.StateMachine.ChangeState(Player.MoveState);
            }

        }
    }
}
