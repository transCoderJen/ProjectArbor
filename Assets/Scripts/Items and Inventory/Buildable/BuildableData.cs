using System;
using UnityEngine;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.ItemsAndInventory;

namespace ShiftedSignal.Garden.Buildable
{
    [Serializable]
    public struct RequiredMaterial
    {
        public ItemData Material;
        public int amount;
    }

    [CreateAssetMenu(fileName = "New Buildable Data", menuName = "Data/Buildable")]
    public class BuildableData : ScriptableObject
    {
        [Header("Identity")]
        public string ItemID;

        [Header("Display")]
        public string BuildableName;
        public Sprite Icon;

        [Header("Prefab")]
        public GameObject BuildablePrefab;

        [Header("Crafting")]
        public RequiredMaterial[] requiredMaterials;
        public int Cost;

        public bool CanAfford()
        {
            if (PlayerManager.Instance == null)
                return false;

            if (PlayerManager.Instance.Currency < Cost)
                return false;

            return HasRequiredMaterials();
        }

        private bool HasRequiredMaterials()
        {
            if (Inventory.Instance == null)
                return false;

            for (int i = 0; i < requiredMaterials.Length; i++)
            {
                RequiredMaterial required = requiredMaterials[i];

                if (required.Material == null)
                    continue;

                if (!Inventory.Instance.HasItem(required.Material, required.amount))
                    return false;
            }

            return true;
        }

        public void RemoveRequiredMaterials()
        {
            if (Inventory.Instance == null)
                return;

            for (int i = 0; i < requiredMaterials.Length; i++)
            {
                RequiredMaterial required = requiredMaterials[i];

                if (required.Material == null)
                    continue;

                Inventory.Instance.RemoveItem(required.Material, required.amount);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(ItemID))
                ItemID = name;

            if (string.IsNullOrWhiteSpace(BuildableName))
                BuildableName = name;

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}