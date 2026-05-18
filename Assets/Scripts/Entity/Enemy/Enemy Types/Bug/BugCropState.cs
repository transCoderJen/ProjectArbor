using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Managers;
using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.BugSpace
{
    public class BugCropState : EnemyState
    {
        protected EnemyBug Enemy;

        private const int CropOverlapSphereBufferSize = 50;

        private readonly Collider[] cropHits = new Collider[CropOverlapSphereBufferSize];

        private GrowBlock targetCrop;
        private GrowBlock lastTargetCrop;

        private float repathTimer;

        private float findNewTargetTimer;
        private readonly float findNewTargetRate = 5f;

        private float stoppingDistanceTimer;
        private readonly float stoppingDistanceUpdateRate = 0.5f;

        public BugCropState(
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
            findNewTargetTimer = 0f;
            lastTargetCrop = null;

            Enemy.ApplyBoidAgentSettings();

            FindTargetCrop();
        }

        public override void Update()
        {
            base.Update();

            Enemy.Hover();

            UpdateTarget();
            UpdateStoppingDistance();

            if (targetCrop == null || (int)targetCrop.CurrentStage < 2)
            {
                Enemy.StateMachine.ChangeState(Enemy.IdleState);
                return;
            }

            Vector3 cropPosition = targetCrop.transform.position;
            Enemy.SetBoidData(BugBoidMode.Crop, cropPosition);

            CheckIfHitCrop();
            ApplyJobDestination(cropPosition);
        }

        private void FindTargetCrop()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                Enemy.transform.position,
                Enemy.ChaseTriggerRadius,
                cropHits,
                Enemy.WhatIsCrop
            );

            GrowBlock bestCrop = null;
            float closestDistanceSqr = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = cropHits[i];

                if (!hit.TryGetComponent(out GrowBlock growBlock))
                    continue;

                if ((int)growBlock.CurrentStage < 2)
                    continue;

                if (hitCount > 1 && growBlock == lastTargetCrop)
                    continue;

                float distanceSqr = (growBlock.transform.position - Enemy.transform.position).sqrMagnitude;

                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    bestCrop = growBlock;
                }
            }

            targetCrop = bestCrop;
        }

        private void UpdateTarget()
        {
            findNewTargetTimer -= Time.deltaTime;

            if (findNewTargetTimer > 0f)
                return;

            findNewTargetTimer = findNewTargetRate;

            lastTargetCrop = targetCrop;

            FindTargetCrop();
        }

        private void UpdateStoppingDistance()
        {
            stoppingDistanceTimer -= Time.deltaTime;

            if (stoppingDistanceTimer > 0f)
                return;

            stoppingDistanceTimer = stoppingDistanceUpdateRate;

            Enemy.Agent.stoppingDistance = Random.value < 0.5f
                ? 0f
                : Random.Range(0f, 5f);
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

        private void CheckIfHitCrop()
        {
            if (targetCrop == null)
                return;

            float distanceToCrop = Vector3.Distance(
                targetCrop.transform.position,
                Enemy.transform.position
            );

            if (distanceToCrop > 1f)
                return;

            if (Enemy.AttackTimer >= 0f)
                return;

            targetCrop.DamageCrop(1);
            Enemy.AttackTimer = Enemy.AttackCoolDown;
        }

        public override void Exit()
        {
            base.Exit();

            targetCrop = null;
        }
    }
}