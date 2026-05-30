using System.Collections;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.Wolf
{
    public class EnemyWolf : Enemy
    {
        #region States

        public WolfIdleState IdleState { get; private set; }
        public WolfMoveState MoveState { get; private set; }
        public WolfAttackState1 AttackState1 { get; private set; }
        public WolfChaseState ChaseState { get; private set; }

        #endregion

        [Header("Wolf Wander")]
        [SerializeField] private float WanderRadius = 8f;
        [SerializeField] private float MinIdleWaitTime = 1f;
        [SerializeField] private float MaxIdleWaitTime = 3f;
        [SerializeField] private float WanderPointReachDistance = 0.35f;
        [SerializeField] private int WanderPointAttempts = 12;

        [Header("Lunge Attack")]
        [SerializeField] private float LungeDistance = 2.25f;
        [SerializeField] private float LungeSpeed = 18f;
        [SerializeField] private float LungeRecoveryTime = 0.65f;
        [SerializeField] private float AttackStopDistanceFromPlayer = 0.65f;
        [SerializeField] private float LungeTimeout = 2f;

        [Header("Chase")]
        public float LoseTargetTime = 3f;

        [Header("Lunge Recovery Circle")]
        [SerializeField] private float RecoveryCircleRadius = 1.75f;
        [SerializeField] private float RecoveryCircleSpeed = 4f;
        [SerializeField] private bool CircleClockwise = true;

        [Header("Lunge Randomization")]
        [SerializeField] private Vector2 LungeDistanceRange = new Vector2(8f, 12f);
        [SerializeField] private Vector2 LungeSpeedRange = new Vector2(20f, 30f);
        [SerializeField] private Vector2 RecoveryCircleRadiusRange = new Vector2(7f, 13f);
        [SerializeField] private Vector2 RecoveryCircleSpeedRange = new Vector2(10f, 20f);

        private Coroutine lungeCoroutine;

        public bool IsLunging { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            Agent.updateRotation = false;
            Agent.speed = moveSpeed;

            IdleState = new WolfIdleState(this, StateMachine, "Idle", this);
            MoveState = new WolfMoveState(this, StateMachine, "Move", this);
            AttackState1 = new WolfAttackState1(this, StateMachine, "Attack1", this);
            ChaseState = new WolfChaseState(this, StateMachine, "Move", this);
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
                EndLunge();
                yield break;
            }

            FaceTarget(target);

            Vector3 directionToPlayer = target.position - transform.position;
            directionToPlayer.y = 0f;

            if (directionToPlayer.sqrMagnitude <= 0.01f)
                directionToPlayer = FacingDir;

            directionToPlayer.Normalize();

            Vector3 desiredEndPosition =
                target.position - directionToPlayer * AttackStopDistanceFromPlayer;

            desiredEndPosition.y = transform.position.y;

            Vector3 maxLungePosition =
                transform.position + directionToPlayer * LungeDistance;

            maxLungePosition.y = transform.position.y;

            Vector3 endPosition =
                Vector3.Distance(transform.position, desiredEndPosition) < LungeDistance
                    ? desiredEndPosition
                    : maxLungePosition;

            if (NavMesh.SamplePosition(endPosition, out NavMeshHit hit, 1.25f, NavMesh.AllAreas))
                endPosition = hit.position;

            Vector3 flatEndPosition = GetFlatPosition(endPosition);

            
            float lungeTime = 0f;

            while (Vector3.Distance(GetFlatPosition(transform.position), flatEndPosition) > 0.05f && lungeTime < LungeTimeout)
            {
                lungeTime += Time.deltaTime;
                
                Vector3 currentPosition = transform.position;
                Vector3 nextFlatPosition = Vector3.MoveTowards(
                    GetFlatPosition(currentPosition),
                    flatEndPosition,
                    LungeSpeed * Time.deltaTime);

                transform.position = new Vector3(
                    nextFlatPosition.x,
                    currentPosition.y,
                    nextFlatPosition.z);

                Debug.Log("Lunging");
                yield return null;
            }

            transform.position = new Vector3(
                endPosition.x,
                transform.position.y,
                endPosition.z);

            yield return CircleAroundTarget(target);

            EndLunge();

            StateMachine.ChangeState(IdleState);
        }

        private IEnumerator CircleAroundTarget(Transform target)
        {
            float timer = 0f;

            while (timer < LungeRecoveryTime && target != null)
            {
                Vector3 directionFromPlayer = transform.position - target.position;
                directionFromPlayer.y = 0f;

                if (directionFromPlayer.sqrMagnitude <= 0.01f)
                    directionFromPlayer = -FacingDir;

                directionFromPlayer.Normalize();

                Vector3 tangentDirection = CircleClockwise
                    ? new Vector3(directionFromPlayer.z, 0f, -directionFromPlayer.x)
                    : new Vector3(-directionFromPlayer.z, 0f, directionFromPlayer.x);

                Vector3 desiredPosition =
                    target.position +
                    directionFromPlayer * RecoveryCircleRadius +
                    tangentDirection * RecoveryCircleSpeed * Time.deltaTime;

                desiredPosition.y = transform.position.y;

                if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 1.25f, NavMesh.AllAreas))
                {
                    Vector3 flatTargetPosition = GetFlatPosition(hit.position);
                    Vector3 flatCurrentPosition = GetFlatPosition(transform.position);

                    Vector3 nextFlatPosition = Vector3.MoveTowards(
                        flatCurrentPosition,
                        flatTargetPosition,
                        RecoveryCircleSpeed * Time.deltaTime);

                    transform.position = new Vector3(
                        nextFlatPosition.x,
                        transform.position.y,
                        nextFlatPosition.z);
                }

                FaceTarget(target);

                timer += Time.deltaTime;
                yield return null;
            }
        }

        private Vector3 GetFlatPosition(Vector3 position)
        {
            return new Vector3(position.x, 0f, position.z);
        }

        private void EndLunge()
        {
            Agent.isStopped = false;
            IsLunging = false;
            lungeCoroutine = null;
        }


        public void RandomizeLungeAttackValues(Transform target)
        {
            RecoveryCircleRadius = Random.Range(
                RecoveryCircleRadiusRange.x,
                RecoveryCircleRadiusRange.y
            );

            RecoveryCircleSpeed = Random.Range(
                RecoveryCircleSpeedRange.x,
                RecoveryCircleSpeedRange.y
            );

            CircleClockwise = Random.value > 0.5f;

            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            float randomizedExtraDistance = Random.Range(
                LungeDistanceRange.x,
                LungeDistanceRange.y
            );

            LungeDistance = distanceToTarget + randomizedExtraDistance;

            LungeSpeed = Random.Range(
                LungeSpeedRange.x,
                LungeSpeedRange.y
            );
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, WanderRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, AttackTriggerRadius);
        }
    }
}