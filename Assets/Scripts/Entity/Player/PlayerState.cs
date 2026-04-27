using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{    
    public class PlayerState
    {
        protected PlayerStateMachine StateMachine;
        protected Player Player;
        protected Rigidbody Rb;

        private string animBoolName;

        protected float AfterImageTimer = 0f;
        protected float StateTimer;
        protected bool TriggerCalled;

        protected Vector2 CachedMoveInput;
        

        

        public PlayerState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
        {
            this.Player = _player;
            this.StateMachine = _stateMachine;
            this.animBoolName = _animBoolName;
        }

        public virtual void Enter()
        {
            Player.Anim.SetBool(animBoolName, true);
            Rb = Player.Rb;
            TriggerCalled = false;
        }

        public virtual void Update()
        {
            StateTimer -= Time.deltaTime;
            AfterImageTimer += Time.deltaTime;

            if (Player.MoveInput != null && Player.MoveInput.action != null)
            {
                CachedMoveInput = Player.MoveInput.action.ReadValue<Vector2>();
                
                if (CachedMoveInput.magnitude < 0.2f)
                    CachedMoveInput = Vector2.zero;
            }
            else
            {
                CachedMoveInput = Vector2.zero;
            }

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                Player.StateMachine.ChangeState(Player.ManagementState);
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
            Player.Anim.SetBool(animBoolName, false);
        }

        public virtual void AnimationFinishedTrigger()
        {
            TriggerCalled = true;
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
}

