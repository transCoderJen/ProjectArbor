using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.Wolf
{
    public class WolfIdleState : EnemyState
    {
        protected EnemyWolf Enemy;

        private float waitTimer;

        public WolfIdleState(
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

            Enemy.Agent.isStopped = true;
            Enemy.Agent.ResetPath();

            waitTimer = Random.Range(Enemy.idleTime * 0.5f, Enemy.idleTime * 1.5f);
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

            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                StateMachine.ChangeState(Enemy.MoveState);
            }
        }

        public override void Exit()
        {
            base.Exit();

            Enemy.Agent.isStopped = false;
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