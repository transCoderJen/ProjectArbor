using Unity.VisualScripting;
using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.StopMovement();
    }

    public override void Update()
    {
        base.Update();

        if (cachedMoveInput != Vector2.zero)
        {
            player.StateMachine.ChangeState(player.MoveState);
        }
    }
}
