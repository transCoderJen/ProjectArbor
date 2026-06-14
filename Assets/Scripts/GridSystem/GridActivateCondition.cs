using UnityEngine;

namespace ShiftedSignal.Garden.GridSystem
{
    public abstract class GridActivationCondition : ScriptableObject
    {
        public abstract bool CanActivate();
        public virtual void ConsumeCost() { }
    }
}