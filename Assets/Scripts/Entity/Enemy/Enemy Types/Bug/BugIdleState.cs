using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using UnityEngine;


namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.BugSpace
{
    public class BugIdleState : EnemyState
    {
        protected EnemyBug Enemy;

        

        public BugIdleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, EnemyBug enemy) : base(_enemyBase, _stateMachine, _animBoolName)
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
            Enemy.Hover();
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