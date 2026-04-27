using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Managers;
using UnityEngine;


namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.BugSpace
{
    public class BugIdleState : EnemyState
    {
        protected EnemyBug Enemy;
        private float repathTimer;
        private float noiseSeed;
        

        public BugIdleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, EnemyBug enemy) : base(_enemyBase, _stateMachine, _animBoolName)
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
                boids.MaxAvoidancePriority + 1);
        }

        public override void Update()
        {
            base.Update();
            Enemy.Hover();
            CheckIfWithinChaseRange();
            bool flowControl = BoidLogic();
            if (!flowControl)
            {
                return;
            }

        }

        private bool BoidLogic()
        {
            BugBoidManager boids = BugBoidManager.Instance;

            repathTimer -= Time.deltaTime;

            if (repathTimer > 0f)
                return false;

            repathTimer = boids.RepathRate;

            Vector3 direction = GetBoidDirection(boids);

            // Fallback if boids give no direction
            if (direction == Vector3.zero)
            {
                direction = new Vector3(
                    Random.Range(-1f, 1f),
                    0f,
                    Random.Range(-1f, 1f)
                ).normalized;
            }

            // Scale movement distance
            float wanderDistance = boids.WanderDistance;

            Vector3 targetPos = Enemy.transform.position + direction * wanderDistance;

            // Keep current hover Y (IMPORTANT)
            targetPos.y = Enemy.transform.position.y;

            // Optional: clamp to NavMesh
            if (UnityEngine.AI.NavMesh.SamplePosition(
                targetPos,
                out UnityEngine.AI.NavMeshHit hit,
                2f,
                UnityEngine.AI.NavMesh.AllAreas))
            {
                Enemy.Agent.SetDestination(hit.position);
            }
            else
            {
                Enemy.Agent.SetDestination(targetPos);
            }

            return true;
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
                buzz * boids.BuzzWeight;

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.01f)
                return Vector3.zero;

            return direction.normalized;
        }

        private void CheckIfWithinChaseRange()
        {
            Collider[] hits = Physics.OverlapSphere(Enemy.transform.position, Enemy.ChaseTriggerRadius, Enemy.WhatIsPlayer);

            foreach (var hit in hits)
            {
                if (hit.GetComponent<Player>() != null)
                {
                    Enemy.StateMachine.ChangeState(Enemy.ChaseState);
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
        }
        
    }
}