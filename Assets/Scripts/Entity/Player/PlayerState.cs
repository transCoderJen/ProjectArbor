using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerState
{
    protected PlayerStateMachine stateMachine;
    protected Player player;
    protected Rigidbody rb;

    private string animBoolName;

    protected float afterImageTimer = 0f;
    protected float stateTimer;
    protected bool triggerCalled;

    protected Vector2 cachedMoveInput;
    

    

    public PlayerState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
    {
        this.player = _player;
        this.stateMachine = _stateMachine;
        this.animBoolName = _animBoolName;
    }

    public virtual void Enter()
    {
        player.Anim.SetBool(animBoolName, true);
        rb = player.Rb;
        triggerCalled = false;
    }

    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;
        afterImageTimer += Time.deltaTime;

        if (player.MoveInput != null && player.MoveInput.action != null)
        {
            cachedMoveInput = player.MoveInput.action.ReadValue<Vector2>();
            
            if (cachedMoveInput.magnitude < 0.2f)
                cachedMoveInput = Vector2.zero;
        }
        else
        {
            cachedMoveInput = Vector2.zero;
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            player.StateMachine.ChangeState(player.ManagementState);
        }
    }

    public virtual void FixedUpdate()
    {
        
    }

    // private void OnMove(InputValue value)
    // {
    //     player.ApplyMovement(value.Get<Vector2>());
        
    // }

    public virtual void Exit()
    {
        player.Anim.SetBool(animBoolName, false);
    }

    public virtual void AnimationFinishedTrigger()
    {
        triggerCalled = true;
    }

    // protected void CreateTrailAfterImage()
    // {
    //     if (afterImageTimer > player.fx.afterImageRate)
    //     {
    //         player.fx.CreateAfterImageFX(player.transform);
    //         afterImageTimer = 0;
    //     }
    // }
}

