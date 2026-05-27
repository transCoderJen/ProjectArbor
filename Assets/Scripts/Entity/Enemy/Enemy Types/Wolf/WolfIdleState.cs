using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.Wolf
{
    public class WolfIdleState : EnemyState
    {
        protected EnemyWolf Enemy;

        private float waitTimer;
        private bool isWaiting;

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

            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;

                Enemy.Agent.isStopped = true;

                if (waitTimer <= 0f)
                    PickNewWanderPoint();

                return;
            }

            Enemy.Agent.isStopped = false;

            if (Enemy.HasReachedDestination())
            {
                isWaiting = true;
                waitTimer = Random.Range(Enemy.idleTime * 0.5f, Enemy.idleTime * 1.5f);
            }
        }

        public override void Exit()
        {
            base.Exit();
            Enemy.Agent.ResetPath();
        }

        private void PickNewWanderPoint()
        {
            isWaiting = false;

            if (Enemy.TryGetRandomWanderPoint(out Vector3 point))
                Enemy.Agent.SetDestination(point);
        }
    }
}