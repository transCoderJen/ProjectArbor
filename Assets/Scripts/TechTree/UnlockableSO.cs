using System.Collections.Generic;
using System.Linq;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.TechTree
{
    public abstract class UnlockableSO : ScriptableObject
    {
        [field: Header("Identity")]
        [field: SerializeField] public string ItemID { get; private set; }

        [field: Header("Display")]
        [field: SerializeField] public string Name { get; private set; } = "Unit";
        [field: SerializeField] public Sprite Icon { get; private set; }

        [field: Header("Stats")]
        [field: SerializeField] public float BuildTime { get; private set; } = 5f;
        [field: SerializeField] public SupplyCostSO SupplyCost { get; private set; }
        
        [field: SerializeField] protected List<UnlockableSO> unlockRequirements { get; private set; } = new();
        [field: SerializeField] public TechTreeSO TechTree { get; private set; }

        public IEnumerable<UnlockableSO> UnlockRequirements => unlockRequirements.ToList();

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