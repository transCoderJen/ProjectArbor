using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.Wolf
{
    public class WolfChaseState : EnemyState
    {
        protected EnemyWolf Enemy;

        private float loseTargetTimer;

        public WolfChaseState(
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

            loseTargetTimer = Enemy.LoseTargetTime;

            Enemy.Agent.isStopped = false;
        }

        public override void Update()
        {
            base.Update();

            Transform player = PlayerManager.Instance.Player.transform;

            if (player == null)
            {
                StateMachine.ChangeState(Enemy.MoveState);
                return;
            }

            bool inAttackRange = CheckIfWithinAttackRange();

            if (inAttackRange)
            {
                StateMachine.ChangeState(Enemy.AttackState1);
                return;
            }

            bool inChaseRange = CheckIfWithinChaseRange();

            if (inChaseRange)
            {
                loseTargetTimer = Enemy.LoseTargetTime;
                Enemy.Agent.SetDestination(player.position);
                return;
            }

            loseTargetTimer -= Time.deltaTime;

            if (loseTargetTimer <= 0f)
            {
                StateMachine.ChangeState(Enemy.MoveState);
                return;
            }

            Enemy.Agent.SetDestination(player.position);
        }

        public override void Exit()
        {
            base.Exit();

            Enemy.Agent.ResetPath();
        }

        private bool CheckIfWithinAttackRange()
        {
            Collider[] hits = Physics.OverlapSphere(
                Enemy.transform.position,
                Enemy.AttackTriggerRadius,
                Enemy.WhatIsPlayer
            );

            foreach (Collider hit in hits)
            {
                if (hit.TryGetComponent(out Player _))
                    return true;
            }

            return false;
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