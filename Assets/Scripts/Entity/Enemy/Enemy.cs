using System.Collections;
using ShiftedSignal.Garden.Effects;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.Tools;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Enemy : Entity
    {
        public LayerMask WhatIsPlayer;
        public LayerMask WhatIsCrop;
 
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
        [SerializeField] private int attackDamage = 1;
        public int AttackDamage => attackDamage;
        
        [HideInInspector] public float lastTimeAttacked;

        public NavMeshAgent Agent { get; private set; }

        public EnemyStateMachine StateMachine { get; private set; }
        public string lastAnimBoolName { get; private set; }

        [Header("State Triggers")]
        public float AttackTriggerRadius;
        public float ChaseTriggerRadius;

        
        [Header("Raid")]
        [SerializeField] protected Transform FarmTarget;
        [SerializeField] protected float RaidAttackDistance = 1.25f;
        
        [SerializeField] protected float raidTargetPointSampleRadius = 2f;
        [SerializeField] protected float raidTargetPointRefreshTime = 0.75f;

        public float RaidTargetPointRefreshTime => raidTargetPointRefreshTime;

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
            
        }

        protected override void Update()
        {
            // if (UI.IsMenuOpen())
            //     return;
                
            base.Update();

            AttackTimer -= Time.deltaTime;
            
            if (StateMachine.CurrentState != null)
                StateMachine.CurrentState.Update();
        }



        public override void DamageEffect(bool Knockback, Transform Attacker = null)
        {
            // fx.StartCoroutine(nameof(fx.FlashFX));
            Fx.NewFlashFX();
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

        public override void Die()
        {
            StartCoroutine(nameof(DelayedDeath));
        }

        public void SetFarmTarget(Transform farmtarget)
        {
            this.FarmTarget = farmtarget;
        }

        private IEnumerator DelayedDeath()
        {
            yield return Helpers.GetWait(.2f);
            base.Die();  
        }

        protected IRaiderTarget FindBestRaiderTarget()
        {
            MonoBehaviour[] sceneObjects =
                FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            IRaiderTarget bestTarget = null;
            int bestPriority = int.MinValue;
            float closestDistance = Mathf.Infinity;

            foreach (MonoBehaviour sceneObject in sceneObjects)
            {
                if (sceneObject is not IRaiderTarget target)
                    continue;

                if (!target.IsValidTarget || target.TargetTransform == null)
                    continue;

                float distance = Vector3.Distance(
                    transform.position,
                    target.TargetTransform.position
                );

                bool higherPriority =
                    target.Priority > bestPriority;

                bool samePriorityCloser =
                    target.Priority == bestPriority &&
                    distance < closestDistance;

                if (higherPriority || samePriorityCloser)
                {
                    bestTarget = target;
                    bestPriority = target.Priority;
                    closestDistance = distance;
                }
            }

            return bestTarget;
        }

        public bool TryGetRaidTargetMovePoint(Transform target, out Vector3 movePoint)
        {
            movePoint = target.position;

            if (target == null)
                return false;

            if (target.TryGetComponent(out Collider collider))
            {
                movePoint = collider.ClosestPoint(transform.position);
            }
            else
            {
                movePoint = target.position;
            }

            if (NavMesh.SamplePosition(
                    movePoint,
                    out NavMeshHit hit,
                    raidTargetPointSampleRadius,
                    NavMesh.AllAreas))
            {
                movePoint = hit.position;
                return true;
            }

            return false;
        }
    }
    
}
