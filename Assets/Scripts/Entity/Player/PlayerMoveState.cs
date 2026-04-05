using Unity.VisualScripting;
using UnityEngine;

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

        
        player.ApplyMovement(cachedMoveInput);
    }

    public override void Update()
    {
        base.Update();


        if (cachedMoveInput == Vector2.zero)
        {
            player.StateMachine.ChangeState(player.IdleState);
        }
    }

    public override void Exit() 
    {
        base.Exit();
        
    }


}
