using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Stats;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace
{    
    public class EnemyState
    {
        protected EnemyStateMachine StateMachine;
        protected Enemy EnemyBase;
        protected Rigidbody Rb;
        private string animBoolName;
        protected float StateTimer;
        protected bool TriggerCalled;

        public EnemyState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName)
        {
            this.EnemyBase = _enemyBase;
            this.StateMachine = _stateMachine;
            this.animBoolName = _animBoolName;   
        }
        
        public virtual void Enter()
        {
            EnemyBase.Anim.SetBool(animBoolName, true);
            Rb = EnemyBase.Rb;
            TriggerCalled = false;
        }

        public virtual void Update()
        {
            StateTimer -= Time.deltaTime;
        }

        public virtual void Exit()
        {
            if (EnemyBase.IsDead) return;
            EnemyBase.Anim.SetBool(animBoolName, false);
            // enemyBase.AssignLastAnimName(animBoolName);
        }

        public virtual void AnimationFinishedTrigger()
        {
            TriggerCalled = true;
        }
    }
}
