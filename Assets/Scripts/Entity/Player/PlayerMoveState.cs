using Unity.VisualScripting;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    
    public class PlayerMoveState : PlayerState
    {
        public PlayerMoveState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            
            Player.ApplyMovement(CachedMoveInput);
        }

        public override void Update()
        {
            base.Update();

            if (Player.AttackInput.action.WasPressedThisFrame())
            {
                Player.StateMachine.ChangeState(Player.AttackState);   
            }

            if (CachedMoveInput == Vector2.zero)
            {
                Player.StateMachine.ChangeState(Player.IdleState);
            }
        }

        public override void Exit() 
        {
            base.Exit();
            
        }
    }
}
