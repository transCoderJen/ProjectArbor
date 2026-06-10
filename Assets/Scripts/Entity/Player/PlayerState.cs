using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    public class PlayerState
    {
        protected PlayerStateMachine StateMachine;
        protected Player Player;
        protected Rigidbody Rb;

        private readonly string animBoolName;

        protected float AfterImageTimer = 0f;
        protected float StateTimer;
        protected bool TriggerCalled;

        protected Vector2 CachedMoveInput;

        public PlayerState(
            Player _player,
            PlayerStateMachine _stateMachine,
            string _animBoolName)
        {
            Player = _player;
            StateMachine = _stateMachine;
            animBoolName = _animBoolName;
        }

        public virtual void Enter()
        {
            Player.Anim.SetBool(animBoolName, true);
            Rb = Player.Rb;
            TriggerCalled = false;
        }

        public virtual void Update()
        {
            if (!Player.ControlsEnabled)
                return;

            StateTimer -= Time.deltaTime;
            AfterImageTimer += Time.deltaTime;

            CachedMoveInput = Player.CachedMoveInput;

            if (CachedMoveInput.magnitude < 0.2f)
                CachedMoveInput = Vector2.zero;
        }

        public virtual void FixedUpdate()
        {
        }

        public virtual void Exit()
        {
            Player.Anim.SetBool(animBoolName, false);
        }

        public virtual void AnimationFinishedTrigger()
        {
            TriggerCalled = true;
        }
    }
}