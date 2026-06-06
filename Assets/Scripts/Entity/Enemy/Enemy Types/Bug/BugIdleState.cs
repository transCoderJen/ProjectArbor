using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Tools;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.BugSpace
{
    public class BugIdleState : EnemyState
    {
        protected EnemyBug Enemy;

        private float repathTimer;

        private Vector3 spawnAnchor;
        private Vector3 currentWanderTarget;

        public BugIdleState(
            Enemy enemyBase,
            EnemyStateMachine stateMachine,
            string animBoolName,
            EnemyBug enemy) : base(enemyBase, stateMachine, animBoolName)
        {
            Enemy = enemy;
        }

        public override void Enter()
        {
            base.Enter();

            repathTimer = 0f;

            spawnAnchor = Enemy.transform.position;
            currentWanderTarget = spawnAnchor;

            Enemy.SetBoidData(BugBoidMode.Idle, spawnAnchor);
            Enemy.ApplyBoidAgentSettings();
        }

        public override void Update()
        {
            base.Update();

            Enemy.Hover();

            bool foundPlayer = CheckIfWithinChaseRange();
            bool foundCrop = CheckIfWithinCropRange();

            if (foundCrop && foundPlayer)
            {
                if (Random.value >= 0.5f)
                    Enemy.StateMachine.ChangeState(Enemy.ChaseState);
                else
                    Enemy.StateMachine.ChangeState(Enemy.CropState);

                return;
            }

            if (foundPlayer)
            {
                Enemy.StateMachine.ChangeState(Enemy.ChaseState);
                return;
            }

            if (foundCrop)
            {
                Enemy.StateMachine.ChangeState(Enemy.CropState);
                return;
            }

            Enemy.SetBoidData(BugBoidMode.Idle, spawnAnchor);
            ApplyJobWanderDestination();
        }

        private void ApplyJobWanderDestination()
        {
            BugBoidManager boids = BugBoidManager.Instance;

            if (boids == null)
                return;

            repathTimer -= Time.deltaTime;

            if (repathTimer > 0f)
                return;

            repathTimer = Random.Range(
                boids.RepathRate * 0.75f,
                boids.RepathRate * 1.25f
            );

            Vector3 direction = Enemy.BoidDirection;

            Vector3 anchorPull = spawnAnchor - Enemy.transform.position;
            anchorPull.y = 0f;

            float distanceFromAnchor = anchorPull.magnitude;

            if (distanceFromAnchor > boids.WanderDistance)
                direction += anchorPull.normalized * 3f;

            direction += GetNavMeshEdgeAvoidance() * 3f;

            if (direction.sqrMagnitude < 0.01f)
                direction = GetRandomFlatDirection();

            direction.y = 0f;
            direction.Normalize();

            Vector3 desiredPoint;

            if (distanceFromAnchor < boids.WanderDistance * 0.75f)
            {
                Vector2 randomCircle = Random.insideUnitCircle * boids.WanderDistance;
                desiredPoint = spawnAnchor + new Vector3(randomCircle.x, 0f, randomCircle.y);
            }
            else
            {
                desiredPoint = Enemy.transform.position + direction * boids.WanderDistance;
            }

            desiredPoint.y = Enemy.transform.position.y;

            if (NavMesh.SamplePosition(
                    desiredPoint,
                    out NavMeshHit hit,
                    boids.WanderDistance,
                    NavMesh.AllAreas))
            {
                currentWanderTarget = hit.position;
                currentWanderTarget.y = Enemy.transform.position.y;

                Enemy.Agent.SetDestination(currentWanderTarget);
            }
        }

        private Vector3 GetNavMeshEdgeAvoidance()
        {
            if (NavMesh.FindClosestEdge(
                    Enemy.transform.position,
                    out NavMeshHit edgeHit,
                    NavMesh.AllAreas))
            {
                float edgeDistance = edgeHit.distance;
                float avoidDistance = 4f;

                if (edgeDistance < avoidDistance)
                {
                    Vector3 awayFromEdge = Enemy.transform.position - edgeHit.position;
                    awayFromEdge.y = 0f;

                    if (awayFromEdge.sqrMagnitude > 0.01f)
                    {
                        float strength = 1f - edgeDistance / avoidDistance;
                        return awayFromEdge.normalized * strength;
                    }
                }
            }

            return Vector3.zero;
        }

        private Vector3 GetRandomFlatDirection()
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;

            return new Vector3(
                randomCircle.x,
                0f,
                randomCircle.y
            );
        }

        private bool CheckIfWithinChaseRange()
        {
            Collider[] hits = Physics.OverlapSphere(
                Enemy.transform.position,
                Enemy.ChaseTriggerRadius,
                Enemy.WhatIsPlayer
            );

            foreach (Collider hit in hits)
            {
                if (hit.TryGetComponent(out Player _))
                    return true;
            }

            return false;
        }

        private bool CheckIfWithinCropRange()
        {
            Collider[] hits = Physics.OverlapSphere(
                Enemy.transform.position,
                Enemy.ChaseTriggerRadius,
                Enemy.WhatIsCrop
            );

            foreach (Collider hit in hits)
            {
                if (!hit.TryGetComponent(out GrowBlock growBlock))
                    continue;

                if ((int)growBlock.CurrentStage >= 2)
                    return true;
            }

            return false;
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}