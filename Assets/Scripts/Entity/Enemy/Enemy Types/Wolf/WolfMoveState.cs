using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.Wolf
{
    public class WolfMoveState : EnemyState
    {
        private readonly EnemyWolf Enemy;

        public WolfMoveState(
            Enemy enemyBase,
            EnemyStateMachine stateMachine,
            string animBoolName,
            EnemyWolf enemy) : base(enemyBase, stateMachine, animBoolName)
        {
            Enemy = enemy;
        }

        public override void Enter()
        {
            base.Enter();

            Enemy.Agent.isStopped = false;
            PickNewWanderPoint();
        }

        public override void Update()
        {
            base.Update();

            Transform player = Enemy.GetPlayerInAttackRange();

            if (player != null)
            {
                StateMachine.ChangeState(Enemy.AttackState1);
                return;
            }

            if (CheckIfWithinChaseRange())
            {
                StateMachine.ChangeState(Enemy.ChaseState);
                return;
            }

            if (Enemy.HasReachedDestination())
            {
                StateMachine.ChangeState(Enemy.IdleState);
            }
        }

        public override void Exit()
        {
            base.Exit();

            Enemy.Agent.ResetPath();
        }

        private void PickNewWanderPoint()
        {
            if (Enemy.TryGetRandomWanderPoint(out Vector3 point))
            {
                Enemy.Agent.SetDestination(point);
            }
            else
            {
                StateMachine.ChangeState(Enemy.IdleState);
            }
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
    }
}