using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Misc;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Enemy : Entity
    {
        private const float TargetSearchInterval = 0.2f;

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

        [Header("Raid Pathing")]
        [SerializeField] private float maxAcceptableFarmPathExtraDistance = 50f;
        [SerializeField] private float fenceSearchRadius = 40f;

        private float nextTargetSearchTime;
        private IRaiderTarget cachedBestRaiderTarget;

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
            base.Update();

            AttackTimer -= Time.deltaTime;

            StateMachine.CurrentState?.Update();
        }

        public override void DamageEffect(bool Knockback, Transform Attacker = null)
        {
            Fx.NewFlashFX();
            base.DamageEffect(Knockback, Attacker);
        }

        public virtual void AssignLastAnimName(string _animBoolName)
        {
            lastAnimBoolName = _animBoolName;
        }

        public override void SlowEntityBy(float _slowPercentage, float _slowDuration)
        {
            moveSpeed *= 1 - _slowPercentage;
            Anim.speed *= 1 - _slowPercentage;

            if (Agent != null)
                Agent.speed = moveSpeed;

            Invoke(nameof(ReturnDefaultSpeed), _slowDuration);
        }

        protected override void ReturnDefaultSpeed()
        {
            base.ReturnDefaultSpeed();

            moveSpeed = defaultMoveSpeed;

            if (Agent != null)
                Agent.speed = defaultMoveSpeed;
        }

        public virtual void FreezeTime(bool _timeFrozen)
        {
            if (_timeFrozen)
            {
                moveSpeed = 0f;

                if (Agent != null)
                    Agent.speed = 0f;

                Anim.speed = 0f;
            }
            else
            {
                moveSpeed = defaultMoveSpeed;

                if (Agent != null)
                    Agent.speed = defaultMoveSpeed;

                Anim.speed = 1f;
            }
        }

        public virtual void FreezeTimeFor(float duration)
        {
            StartCoroutine(FreezeTimeCoroutine(duration));
        }

        protected virtual IEnumerator FreezeTimeCoroutine(float _seconds)
        {
            FreezeTime(true);

            yield return new WaitForSeconds(_seconds);

            FreezeTime(false);
        }

        public virtual void OpenCounterAttackWindow()
        {
            canBeStunned = true;

            if (counterImage != null)
                counterImage.SetActive(true);
        }

        public virtual void CloseCounterAttackWindow()
        {
            canBeStunned = false;

            if (counterImage != null)
                counterImage.SetActive(false);
        }

        public virtual bool CanBeStunned()
        {
            if (!canBeStunned)
                return false;

            CloseCounterAttackWindow();
            return true;
        }

        public void AnimationTrigger()
        {
            StateMachine.CurrentState?.AnimationFinishedTrigger();
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, AttackTriggerRadius);
            Gizmos.DrawWireSphere(transform.position, ChaseTriggerRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, fenceSearchRadius);
        }

        public override void Die()
        {
            StartCoroutine(nameof(DelayedDeath));
        }

        public void SetFarmTarget(Transform farmtarget)
        {
            FarmTarget = farmtarget;
        }

        private IEnumerator DelayedDeath()
        {
            yield return Helpers.GetWait(.2f);
            base.Die();
        }

        public IRaiderTarget FindBestRaiderTarget()
        {
            if (cachedBestRaiderTarget != null && cachedBestRaiderTarget.IsValidTarget)
            {
                if (Time.time < nextTargetSearchTime)
                    return cachedBestRaiderTarget;
            }

            nextTargetSearchTime = Time.time + TargetSearchInterval;
            cachedBestRaiderTarget = CalculateBestRaiderTarget();

            return cachedBestRaiderTarget;
        }

        private IRaiderTarget CalculateBestRaiderTarget()
        {
            IRaiderTarget farmTarget = FindClosestTargetOfType(RaiderTargetType.Farm);

            if (farmTarget != null)
            {
                if (ShouldTargetFarm(farmTarget))
                    return farmTarget;

                IRaiderTarget fenceTarget = FindBestFenceToBreakToward(farmTarget);

                if (fenceTarget != null)
                    return fenceTarget;
            }

            return FindBestGeneralRaidTarget();
        }

        private bool ShouldTargetFarm(IRaiderTarget farmTarget)
        {
            if (farmTarget == null || farmTarget.TargetTransform == null)
                return false;

            if (!TryGetRaidTargetMovePoint(farmTarget.TargetTransform, out Vector3 farmMovePoint))
                farmMovePoint = farmTarget.TargetTransform.position;

            if (!TryCalculateCompletePath(farmMovePoint, out NavMeshPath path))
                return false;

            float pathDistance = GetPathDistance(path);
            float directDistance = Vector3.Distance(transform.position, farmMovePoint);

            return pathDistance <= directDistance + maxAcceptableFarmPathExtraDistance;
        }

        private IRaiderTarget FindBestFenceToBreakToward(IRaiderTarget farmTarget)
        {
            IReadOnlyList<IRaiderTarget> targets = RaiderTargetRegistry.Targets;

            IRaiderTarget bestFence = null;
            float bestScore = float.MinValue;

            Vector3 farmPosition = farmTarget.TargetTransform.position;
            Vector3 directionToFarm = (farmPosition - transform.position).normalized;

            foreach (IRaiderTarget target in targets)
            {
                if (target == null)
                    continue;

                if (!target.IsValidTarget || target.TargetTransform == null)
                    continue;

                if (target.TargetType != RaiderTargetType.Fence)
                    continue;

                float distanceToFence = Vector3.Distance(
                    transform.position,
                    target.TargetTransform.position
                );

                if (distanceToFence > fenceSearchRadius)
                    continue;

                Vector3 directionToFence =
                    (target.TargetTransform.position - transform.position).normalized;

                float alignmentWithFarm =
                    Vector3.Dot(directionToFarm, directionToFence);

                float score =
                    target.Priority +
                    alignmentWithFarm * 50f -
                    distanceToFence;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestFence = target;
                }
            }

            return bestFence;
        }

        private IRaiderTarget FindBestGeneralRaidTarget()
        {
            IReadOnlyList<IRaiderTarget> targets = RaiderTargetRegistry.Targets;

            IRaiderTarget bestTarget = null;
            float bestScore = float.MinValue;

            foreach (IRaiderTarget target in targets)
            {
                if (target == null)
                    continue;

                if (!target.IsValidTarget || target.TargetTransform == null)
                    continue;

                float distance = Vector3.Distance(
                    transform.position,
                    target.TargetTransform.position
                );

                float score = target.Priority - distance;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }

            return bestTarget;
        }

        private IRaiderTarget FindClosestTargetOfType(RaiderTargetType targetType)
        {
            IReadOnlyList<IRaiderTarget> targets = RaiderTargetRegistry.Targets;

            IRaiderTarget bestTarget = null;
            float closestDistance = Mathf.Infinity;

            foreach (IRaiderTarget target in targets)
            {
                if (target == null)
                    continue;

                if (!target.IsValidTarget || target.TargetTransform == null)
                    continue;

                if (target.TargetType != targetType)
                    continue;

                float distance = Vector3.Distance(
                    transform.position,
                    target.TargetTransform.position
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = target;
                }
            }

            return bestTarget;
        }

        private bool TryCalculateCompletePath(Vector3 destination, out NavMeshPath path)
        {
            path = new NavMeshPath();

            if (Agent == null || !Agent.isOnNavMesh)
                return false;

            bool calculated = Agent.CalculatePath(destination, path);

            return calculated && path.status == NavMeshPathStatus.PathComplete;
        }

        private float GetPathDistance(NavMeshPath path)
        {
            if (path == null || path.corners == null || path.corners.Length < 2)
                return 0f;

            float distance = 0f;

            for (int i = 1; i < path.corners.Length; i++)
            {
                distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }

            return distance;
        }

        public bool TryGetRaidTargetMovePoint(Transform target, out Vector3 movePoint)
        {
            movePoint = target != null ? target.position : transform.position;

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