using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.TechTree
{
    public abstract class UpgradeSO : UnlockableSO, IModifier
    {
        [field: SerializeField] public string PropertyPath { get; private set; }

        public abstract void Apply(AbstractUnitSO unit);
    }
}