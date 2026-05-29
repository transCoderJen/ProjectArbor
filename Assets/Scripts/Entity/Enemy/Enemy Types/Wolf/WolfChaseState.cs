using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.Wolf
{
    public class WolfChaseState : EnemyState
    {
        protected EnemyWolf Enemy;
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
        }

        public override void Update()
        {
            base.Update();
            Enemy.Agent.SetDestination(PlayerManager.Instance.Player.transform.position);

            bool inAttackRange = CheckIfWithinAttackRange();

            if (inAttackRange)
            {
                Enemy.StateMachine.ChangeState(Enemy.AttackState1);
            }
        }

        public override void Exit()
        {
            base.Exit();
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
    }
}