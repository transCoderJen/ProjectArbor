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

        private const int PlayerOverlapSphereBufferSize = 32;

        private readonly Collider[] playerHits = new Collider[PlayerOverlapSphereBufferSize];

        private float repathTimer;

        private float stoppingDistanceTimer;
        private readonly float stoppingDistanceUpdateRate = 0.5f;

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
            stoppingDistanceTimer = 0f;

            Enemy.ApplyBoidAgentSettings();
        }

        public override void Update()
        {
            base.Update();

            Enemy.Hover();
            UpdateStoppingDistance();

            Player player = PlayerManager.Instance != null
                ? PlayerManager.Instance.Player
                : null;

            if (player == null)
            {
                Enemy.StateMachine.ChangeState(Enemy.IdleState);
                return;
            }

            Vector3 playerPosition = player.transform.position;

            Enemy.SetBoidData(BugBoidMode.Chase, playerPosition);

            CheckIfHitPlayer();
            ApplyJobDestination(playerPosition);
        }

        private void UpdateStoppingDistance()
        {
            stoppingDistanceTimer -= Time.deltaTime;

            if (stoppingDistanceTimer > 0f)
                return;

            stoppingDistanceTimer = stoppingDistanceUpdateRate;

            Enemy.Agent.stoppingDistance = Random.value < 0.5f
                ? 0f
                : Random.Range(0f, 15f);
        }

        private void ApplyJobDestination(Vector3 fallbackDestination)
        {
            BugBoidManager boids = BugBoidManager.Instance;

            if (boids == null)
            {
                Enemy.Agent.SetDestination(fallbackDestination);
                return;
            }

            repathTimer -= Time.deltaTime;

            if (repathTimer > 0f)
                return;

            repathTimer = boids.RepathRate;

            Vector3 destination = Enemy.BoidDestination;

            if (destination == Vector3.zero)
                destination = fallbackDestination;

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
                Enemy.Agent.SetDestination(fallbackDestination);
            }
        }

        private void CheckIfHitPlayer()
        {
            if (Enemy.AttackCheck == null)
                return;

            int hitCount = Physics.OverlapSphereNonAlloc(
                Enemy.AttackCheck.position,
                Enemy.AttackCheckRadius,
                playerHits,
                Enemy.WhatIsPlayer
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = playerHits[i];

                if (!hit.TryGetComponent(out Player player))
                    continue;

                CharacterHealth playerHealth = player.Health;

                if (playerHealth == null)
                    playerHealth = player.GetComponent<CharacterHealth>();

                if (playerHealth == null)
                    continue;

                int damage = Enemy.AttackDamage;

                playerHealth.TakeDamage(damage, false, Enemy.transform);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}