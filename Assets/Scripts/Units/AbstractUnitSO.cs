using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.TechTree;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    public abstract class AbstractUnitSO : UnlockableSO
    {
        [field: Header("Prefab")]
        [field: SerializeField] public GameObject Prefab { get; private set; }

        [field: SerializeField] public int Health { get; private set; } = 100;

        [field: Header("Combat")]
        [field: SerializeField] public Owner Team { get; private set; } = Owner.Unowned;
        [field: SerializeField] public AttackConfigSO AttackConfig { get; private set; }

        // [field: SerializeField] public UpgradeSO[] Upgrades { get; private set; }     
        
        


    }
}