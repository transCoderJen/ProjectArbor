using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Stats;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.BugSpace
{
    public class BugChaseState : EnemyState
    {
        protected EnemyBug Enemy;

        private float repathTimer;
        private float noiseSeed;

        public BugChaseState(
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
            noiseSeed = Random.Range(0f, 1000f);

            BugBoidManager boids = BugBoidManager.Instance;

            if (boids == null)
                return;

            Enemy.Agent.obstacleAvoidanceType = boids.AvoidanceType;
            Enemy.Agent.avoidancePriority = Random.Range(
                boids.MinAvoidancePriority,
                boids.MaxAvoidancePriority + 1
            );
        }

        public override void Update()
        {
            base.Update();

            Enemy.Hover();
            CheckIfHitPlayer();
            bool flowControl = BoidLogic();
            if (!flowControl)
            {
                return;
            }
        }

        private bool BoidLogic()
        {
            BugBoidManager boids = BugBoidManager.Instance;

            if (PlayerManager.Instance.Player == null)
            {
                Enemy.StateMachine.ChangeState(Enemy.IdleState);
                return false;
            }


            Vector3 playerPos = PlayerManager.Instance.Player.transform.position;

            if (boids == null)
            {
                Enemy.Agent.SetDestination(playerPos);
                return false;
            }

            repathTimer -= Time.deltaTime;

            if (repathTimer > 0f)
                return false;

            repathTimer = boids.RepathRate;

            float distanceToPlayer = Vector3.Distance(Enemy.transform.position, playerPos);

            Vector3 boidDirection = GetBoidDirection(boids);

            if (boidDirection == Vector3.zero)
            {
                boidDirection = playerPos - Enemy.transform.position;
                boidDirection.y = 0f;

                if (boidDirection.sqrMagnitude > 0.01f)
                    boidDirection.Normalize();
            }

            Vector3 destination;

            if (distanceToPlayer > boids.DirectChaseDistance)
            {
                destination = playerPos;
            }
            else
            {
                Vector3 boidDestination =
                    Enemy.transform.position + boidDirection * boids.ChaseStepDistance;

                destination = Vector3.Lerp(
                    boidDestination,
                    playerPos,
                    boids.PlayerPull
                );
            }

            if (NavMesh.SamplePosition(
                    destination,
                    out NavMeshHit hit,
                    boids.NavMeshSampleDistance,
                    NavMesh.AllAreas))
            {
                Enemy.Agent.SetDestination(hit.position);
            }
            else
            {
                Enemy.Agent.SetDestination(playerPos);
            }

            return true;
        }

        private void CheckIfHitPlayer()
        {
            Collider[] hits = Physics.OverlapSphere(Enemy.AttackCheck.transform.position, Enemy.AttackCheckRadius, Enemy.WhatIsPlayer);

            foreach (var hit in hits)
            {
                if (hit.GetComponent<Player>() != null)
                {
                    // Enemy.StateMachine.ChangeState(Enemy.AttackState);
                    PlayerStats playerStats = PlayerManager.Instance.Player.GetComponent<PlayerStats>();
                    Enemy.GetComponent<EnemyStats>().DoDamage(playerStats, false);
                }
            }
        }

        private void CheckIfWithinAttackRange()
        {
            Collider[] hits = Physics.OverlapSphere(Enemy.transform.position, Enemy.AttackTriggerRadius, Enemy.WhatIsPlayer);

            foreach (var hitt in hits)
            {
                if (hitt.GetComponent<Player>() != null)
                {
                    // Enemy.StateMachine.ChangeState(Enemy.AttackState);
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        private Vector3 GetBoidDirection(BugBoidManager boids)
        {
            Collider[] hits = Physics.OverlapSphere(
                Enemy.transform.position,
                boids.NeighborRadius,
                boids.BugLayer
            );

            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;

            int neighborCount = 0;

            foreach (Collider hit in hits)
            {
                if (hit.gameObject == Enemy.gameObject)
                    continue;

                if (!hit.TryGetComponent(out EnemyBug bug))
                    continue;

                Vector3 toSelf = Enemy.transform.position - bug.transform.position;
                toSelf.y = 0f;

                float distance = toSelf.magnitude;

                if (distance <= 0.01f)
                    continue;

                separation += toSelf.normalized / distance;

                Vector3 neighborVelocity = bug.Agent.velocity;
                neighborVelocity.y = 0f;

                if (neighborVelocity.sqrMagnitude > 0.01f)
                    alignment += neighborVelocity.normalized;

                cohesion += bug.transform.position;

                neighborCount++;
            }

            if (neighborCount > 0)
            {
                alignment /= neighborCount;

                Vector3 center = cohesion / neighborCount;
                cohesion = center - Enemy.transform.position;
                cohesion.y = 0f;

                if (cohesion.sqrMagnitude > 0.01f)
                    cohesion.Normalize();
            }

            Vector3 chase = PlayerManager.Instance.Player.transform.position - Enemy.transform.position;
            chase.y = 0f;

            if (chase.sqrMagnitude > 0.01f)
                chase.Normalize();

            Vector3 buzz = new Vector3(
                Mathf.PerlinNoise(Time.time * boids.BuzzSpeed, noiseSeed) - 0.5f,
                0f,
                Mathf.PerlinNoise(noiseSeed, Time.time * boids.BuzzSpeed) - 0.5f
            );

            if (buzz.sqrMagnitude > 0.01f)
                buzz.Normalize();

            Vector3 direction =
                separation * boids.SeparationWeight +
                alignment * boids.AlignmentWeight +
                cohesion * boids.CohesionWeight +
                chase * boids.ChaseWeight +
                buzz * boids.BuzzWeight;

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.01f)
                return Vector3.zero;

            return direction.normalized;
        }
    }
}