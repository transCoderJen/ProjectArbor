using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.Wolf
{
    public class WolfMoveState : EnemyState
    {
        EnemyWolf Enemy;
        public WolfMoveState(Enemy _enemyBase, 
            EnemyStateMachine _stateMachine, 
            string _animBoolName,
            EnemyWolf enemy) : base(_enemyBase, _stateMachine, _animBoolName)
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
        }

        public override void Exit()
        {
            base.Exit();
        }
        
    }
}