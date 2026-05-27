using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.Wolf
{
    public class EnemyWolf : Enemy
    {
        #region States
        public WolfIdleState IdleState { get; private set; }
        public WolfAttackState1 AttackState1 { get; private set; }
        #endregion

        [Header("Wolf Wander")]
        [SerializeField] private float WanderRadius = 8f;
        [SerializeField] private float MinIdleWaitTime = 1f;
        [SerializeField] private float MaxIdleWaitTime = 3f;
        [SerializeField] private float WanderPointReachDistance = 0.35f;
        [SerializeField] private int WanderPointAttempts = 12;

        [Header("Wolf Attack")]
        [SerializeField] private float LungeDistance = 1.75f;
        [SerializeField] private float LungeDuration = 0.18f;
        [SerializeField] private float LungeRecoveryTime = 0.45f;
        [SerializeField] private float AttackStopDistanceFromPlayer = 0.75f;

        private Coroutine lungeCoroutine;

        public bool IsLunging { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            Agent.updateRotation = false;
            Agent.speed = moveSpeed;

            IdleState = new WolfIdleState(this, StateMachine, "Idle", this);
            AttackState1 = new WolfAttackState1(this, StateMachine, "Attack1", this);
        }

        protected override void Start()
        {
            base.Start();
            StateMachine.Initialize(IdleState);
        }

        public bool TryGetRandomWanderPoint(out Vector3 point)
        {
            for (int i = 0; i < WanderPointAttempts; i++)
            {
                Vector3 randomDirection = Random.insideUnitSphere * WanderRadius;
                randomDirection.y = 0f;

                Vector3 samplePosition = transform.position + randomDirection;

                if (NavMesh.SamplePosition(samplePosition, out NavMeshHit hit, WanderRadius, NavMesh.AllAreas))
                {
                    point = hit.position;
                    return true;
                }
            }

            point = transform.position;
            return false;
        }

        public bool HasReachedDestination()
        {
            if (Agent.pathPending)
                return false;

            return Agent.remainingDistance <= WanderPointReachDistance;
        }

        public Transform GetPlayerInAttackRange()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, AttackTriggerRadius, WhatIsPlayer);

            if (hits.Length <= 0)
                return null;

            return hits[0].transform;
        }

        public void FaceTarget(Transform target)
        {
            if (target == null)
                return;

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.01f)
                return;

            Vector3 normalizedDirection = direction.normalized;

            FacingDir = normalizedDirection;
            LastFacingDir = new Vector2(normalizedDirection.x, normalizedDirection.z);

            Anim.SetFloat("MovementX", Mathf.Round(normalizedDirection.x));
            Anim.SetFloat("MovementY", Mathf.Round(normalizedDirection.z));
        }

        public void StartLunge(Transform target)
        {
            if (lungeCoroutine != null)
                StopCoroutine(lungeCoroutine);

            lungeCoroutine = StartCoroutine(LungeCoroutine(target));
        }

        private IEnumerator LungeCoroutine(Transform target)
        {
            IsLunging = true;

            Agent.ResetPath();
            Agent.isStopped = true;

            if (target == null)
            {
                IsLunging = false;
                yield break;
            }

            FaceTarget(target);

            Vector3 directionToPlayer = target.position - transform.position;
            directionToPlayer.y = 0f;

            if (directionToPlayer.sqrMagnitude <= 0.01f)
                directionToPlayer = FacingDir;

            directionToPlayer.Normalize();

            Vector3 desiredEndPosition = target.position - directionToPlayer * AttackStopDistanceFromPlayer;
            desiredEndPosition.y = transform.position.y;

            Vector3 maxLungePosition = transform.position + directionToPlayer * LungeDistance;

            Vector3 endPosition = Vector3.Distance(transform.position, desiredEndPosition) < LungeDistance
                ? desiredEndPosition
                : maxLungePosition;

            if (NavMesh.SamplePosition(endPosition, out NavMeshHit hit, 1.25f, NavMesh.AllAreas))
                endPosition = hit.position;

            Vector3 startPosition = transform.position;
            float timer = 0f;

            while (timer < LungeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / LungeDuration;

                transform.position = Vector3.Lerp(startPosition, endPosition, t);

                yield return null;
            }

            transform.position = endPosition;

            yield return new WaitForSeconds(LungeRecoveryTime);

            Agent.isStopped = false;
            IsLunging = false;
            lungeCoroutine = null;

            StateMachine.ChangeState(IdleState);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, WanderRadius);
        }
    }
}