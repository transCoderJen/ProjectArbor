using System.Collections;
using ShiftedSignal.Garden.Effects;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace
{
    public class Enemy : Entity
    {
        public LayerMask WhatIsPlayer;
 
        [Header("Stunned Info")]
        public float stunDuration;
        public Vector2 stunDirection;
        protected bool canBeStunned;
        [SerializeField] protected GameObject counterImage;


        [Header("Move Info")]
        public float moveSpeed;
        public float idleTime;
        public float battleTime;
        private float defaultMoveSpeed;

        [Header("Attack Info")]
        
        [HideInInspector] public float lastTimeAttacked;

        public NavMeshAgent Agent { get; private set; }

        public EnemyStateMachine StateMachine { get; private set; }
        public EntityFX fx { get; private set; }
        public string lastAnimBoolName { get; private set; }

        [Header("State Triggers")]
        public float AttackTriggerRadius;
        public float ChaseTriggerRadius;

        protected override void Awake()
        {
            base.Awake();
            StateMachine = new EnemyStateMachine();
            Agent = GetComponent<NavMeshAgent>();
            defaultMoveSpeed = moveSpeed;
            Agent.speed = defaultMoveSpeed;
        }

        protected override void Start()
        {
            base.Start();
            fx = GetComponent<EntityFX>();
        }

        protected override void Update()
        {
            // if (UI.IsMenuOpen())
            //     return;
                
            base.Update();

            if (StateMachine == null)
            {
                Debug.Log("State mahchine is null");
            }
            if (StateMachine.CurrentState == null)
            {
                Debug.Log("Current state is null");
            }
            
            StateMachine.CurrentState.Update();
            if (transform.position.y < -20)
                Destroy(this.gameObject);
        }

        public override void DamageEffect(bool Knockback, Transform Attacker = null)
        {
            // fx.StartCoroutine(nameof(fx.FlashFX));
            fx.NewFlashFX();
            base.DamageEffect(Knockback, Attacker);
        }
        
        public virtual void AssignLastAnimName(string _animBoolName) => lastAnimBoolName = _animBoolName;

        public override void SlowEntityBy(float _slowPercentage, float _slowDuration)
        {
            moveSpeed = moveSpeed * (1 - _slowPercentage);
            Anim.speed = Anim.speed * (1 - _slowPercentage);

            Invoke("ReturnDefaultSpeed", _slowDuration);
        }
        
        protected override void ReturnDefaultSpeed()
        {
            base.ReturnDefaultSpeed();

            moveSpeed = defaultMoveSpeed;
        }

        public virtual void FreezeTime(bool _timeFrozen)
        {
            if (_timeFrozen)
            {
                moveSpeed = 0;
                Anim.speed = 0;
            }
            else if(!_timeFrozen)
            {
                moveSpeed = defaultMoveSpeed;
                Anim.speed = 1;
            }
        }

        public virtual void FreezeTimeFor(float duration) => StartCoroutine(FreezeTimeCoroutine(duration));
        
        protected virtual IEnumerator FreezeTimeCoroutine(float _seconds)
        {
            FreezeTime(true);

            yield return new WaitForSeconds(_seconds);

            FreezeTime(false);
        }
        
        #region Counter Attack Window
            
        public virtual void OpenCounterAttackWindow()
        {
            canBeStunned = true;
            counterImage.SetActive(true);
        }

        public virtual void CloseCounterAttackWindow()
        {
            canBeStunned = false;
            counterImage.SetActive(false);
        }
        #endregion

        public virtual bool CanBeStunned()
        {
            if (canBeStunned)
            {
                CloseCounterAttackWindow();
                return true;
            }

            return false;
        }

        public void AnimationTrigger() => StateMachine.CurrentState.AnimationFinishedTrigger();

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(transform.position, AttackTriggerRadius);
            Gizmos.DrawWireSphere(transform.position, ChaseTriggerRadius);
            
        }
    }
    
}
