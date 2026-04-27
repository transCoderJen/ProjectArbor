using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace
{
    public class EnemyStateMachine
    {
        public EnemyState CurrentState { get; private set; }

        public void Initialize(EnemyState _startState)
        {
            Debug.Log("State being initialized");
            CurrentState = _startState;
            CurrentState.Enter();
        }

        public void ChangeState(EnemyState _newState)
        {
            CurrentState.Exit();
            CurrentState = _newState;
            CurrentState.Enter();
        }

    }    
}
