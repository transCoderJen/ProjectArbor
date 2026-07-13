using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Interfaces;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.Wolf
{
    public class WolfRaidState : EnemyState
    {
        private readonly EnemyWolf wolf;

        private float attackTimer;
        private float repathTimer;

        private Vector3 currentMovePoint;

        private const float DebugAttackInterval = 2f;
        private const int DebugAttackDamage = 1;

        public WolfRaidState(
            Enemy enemy,
            EnemyStateMachine stateMachine,
            string animBoolName,
            EnemyWolf wolf) 
            : base(enemy, stateMachine, animBoolName)
        {
            this.wolf = wolf;
        }

        public override void Enter()
        {
            base.Enter();

            attackTimer = 0f;
            repathTimer = 0f;

            wolf.Agent.isStopped = false;

            RefreshMovePoint();
        }

        public override void Update()
        {
            base.Update();

            if (!wolf.HasRaidTarget())
            {
                TryRetargetOrIdle();
                return;
            }

            wolf.FaceTarget(wolf.CurrentRaidTarget);

            repathTimer -= Time.deltaTime;

            if (repathTimer <= 0f)
            {
                RefreshMovePoint();
            }

            float distanceToMovePoint = Vector3.Distance(
                wolf.transform.position,
                currentMovePoint
            );

            if (distanceToMovePoint <= wolf.GetRaidAttackDistance())
            {
                HandleReachedRaidTarget();
                return;
            }

            wolf.Agent.isStopped = false;
            wolf.Agent.SetDestination(currentMovePoint);
        }

        public override void Exit()
        {
            base.Exit();

            wolf.Agent.isStopped = false;
        }

        private void RefreshMovePoint()
        {
            repathTimer = wolf.RaidTargetPointRefreshTime;

            if (!wolf.HasRaidTarget())
                return;

            bool foundPoint = wolf.TryGetRaidTargetMovePoint(
                wolf.CurrentRaidTarget,
                out currentMovePoint
            );

            if (!foundPoint)
            {
                currentMovePoint = wolf.CurrentRaidTarget.position;
            }

            wolf.Agent.isStopped = false;
            wolf.Agent.SetDestination(currentMovePoint);
        }

        private void HandleReachedRaidTarget()
        {
            wolf.Agent.ResetPath();
            wolf.Agent.isStopped = true;

            wolf.FaceTarget(wolf.CurrentRaidTarget);

            attackTimer -= Time.deltaTime;

            if (attackTimer > 0f)
                return;

            attackTimer = DebugAttackInterval;

            DamageRaidTarget();

            if (!CurrentRaidTargetStillValid())
            {
                TryRetargetOrIdle();
            }
        }

        private void DamageRaidTarget()
        {
            if (wolf.CurrentRaidTarget == null)
                return;

            IDamageable damageable =
                wolf.CurrentRaidTarget.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                Debug.LogWarning(
                    $"{wolf.CurrentRaidTarget.name} does not have an IDamageable component.",
                    wolf.CurrentRaidTarget
                );

                return;
            }

            damageable.TakeDamage(
                new DamageData(
                    DebugAttackDamage,
                    Owner.Enemy,
                    wolf.transform
                )
            );

            Debug.Log(
                $"{wolf.name} damaged {wolf.CurrentRaidTarget.name} for {DebugAttackDamage}.",
                wolf
            );
        }

        private bool CurrentRaidTargetStillValid()
        {
            if (wolf.CurrentRaidTarget == null)
                return false;

            IRaiderTarget raiderTarget =
                wolf.CurrentRaidTarget.GetComponentInParent<IRaiderTarget>();

            return raiderTarget != null && raiderTarget.IsValidTarget;
        }

        private void TryRetargetOrIdle()
        {
            wolf.TryFindAndSetBestRaiderTarget();

            if (!wolf.HasRaidTarget())
            {
                StateMachine.ChangeState(wolf.IdleState);
                return;
            }

            RefreshMovePoint();
        }
    }
}