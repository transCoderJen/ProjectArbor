using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.Wolf
{
    public class WolfAttackState1 : EnemyState
    {
        protected EnemyWolf Enemy;

        private Transform target;

        public WolfAttackState1(
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

            target = Enemy.GetPlayerInAttackRange();

            if (target == null)
            {
                StateMachine.ChangeState(Enemy.IdleState);
                return;
            }

            Enemy.lastTimeAttacked = Time.time;
            Enemy.AttackTimer = Enemy.AttackCoolDown;

            Enemy.FaceTarget(target);
            Enemy.StartLunge(target);
        }

        public override void Update()
        {
            base.Update();

            if (Enemy.IsLunging)
                return;

            StateMachine.ChangeState(Enemy.IdleState);
        }

        public override void Exit()
        {
            base.Exit();
            target = null;
        }
    }
}