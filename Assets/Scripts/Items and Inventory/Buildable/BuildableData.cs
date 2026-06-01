using System;
using UnityEngine;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignalGames.GOF.ItemsAndInventory;

namespace ShiftedSignal.Garden.ItemsAndInventory
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
        public string BuildableName;
        public Sprite Icon;
        public string ItemID;
        public GameObject BuildablePrefab;
        public RequiredMaterial[] requiredMaterials;

        public int Cost;

        public bool CanAfford()
        {
            if (PlayerManager.Instance.Currency < Cost)
            {
                Debug.Log("Not Enough Gold to Build " + BuildableName);
                return false;
            }

            if (!HasRequiredMaterials())
            {
                Debug.Log("Not Enough Materials to Build " + BuildableName);
                return false;
            }

            return true;
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

                for (int j = 0; j < required.amount; j++)
                {
                    Inventory.Instance.RemoveItem(required.Material, true);
                }
            }
        }
    }
}