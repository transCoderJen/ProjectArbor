using ShiftedSignal.Garden.Combat;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    public abstract class AbstractUnitSO : ScriptableObject
    {
        [field: Header("Identity")]
        [field: SerializeField] public string ItemID { get; private set; }

        [field: Header("Display")]
        [field: SerializeField] public string Name { get; private set; } = "Unit";
        [field: SerializeField] public Sprite Icon { get; private set; }

        [field: Header("Prefab")]
        [field: SerializeField] public GameObject Prefab { get; private set; }

        [field: Header("Stats")]
        [field: SerializeField] public int Health { get; private set; } = 100;
        [field: SerializeField] public float BuildTime { get; private set; } = 5f;
        [field: SerializeField] public SupplyCostSO SupplyCost { get; private set; }
        
        [field: Header("Combat")]
        [field: SerializeField] public CombatTeam Team { get; private set; } = CombatTeam.Neutral;
        [field: SerializeField] public AttackConfigSO AttackConfig { get; private set; }

        public bool CanAfford()
        {
            if (SupplyCost == null)
            {
                Debug.LogError($"{name} has no SupplyCostSO assigned.");
                return false;
            }
            
            return SupplyCost != null && SupplyCost.CanAfford();
        }

        public void SpendCost()
        {
            

            SupplyCost?.Spend();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(ItemID))
                ItemID = name;

            if (string.IsNullOrWhiteSpace(Name))
                Name = name;

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}