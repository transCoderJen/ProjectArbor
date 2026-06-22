using System;
using System.Security.Cryptography.X509Certificates;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public enum BuildPlacementMode
    {
        GridOnly,
        Anywhere
    }

    [Serializable]
    public struct RequiredMaterial
    {
        public ItemData Material;
        public int Amount;
    }

    [Serializable]
    public struct TowerStats
    {
        [Header("Targeting")]
        public float AttackRange;
        public float AttackCooldown;

        [Header("Projectile")]
        public float ProjectileSpeed;

        [Range(0f, 100f)]
        public float ProjectileAccuracy;

        public float ProjectileBuildUpTime;
        public bool ProjectileRotate;
        public float ProjectileRotateAmount;
        public bool ProjectileBounce;
        public float ProjectileBounceForce;
        public float ProjectileLifetime;
    }

    [CreateAssetMenu(fileName = "New Buildable Data", menuName = "Data/Buildable")]
    public class BuildableData : UnitSO
    {
        [Header("Placement Rules")]
        public BuildPlacementMode PlacementMode = BuildPlacementMode.GridOnly;
        public bool RequiresActiveGrowBlock = true;
        public float XRotation = 30f;

        [Header("Crafting")]
        public RequiredMaterial[] RequiredMaterials;
        public int Cost;

        [Header("Tower Stats")]
        public bool HasTowerStats;
        public TowerStats BaseTowerStats;

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

            if (RequiredMaterials == null)
                return true;

            for (int i = 0; i < RequiredMaterials.Length; i++)
            {
                RequiredMaterial required = RequiredMaterials[i];

                if (required.Material == null)
                    continue;

                if (!Inventory.Instance.HasItem(required.Material, required.Amount))
                    return false;
            }

            return true;
        }

        public void RemoveRequiredMaterials()
        {
            if (Inventory.Instance == null)
                return;

            if (RequiredMaterials == null)
                return;

            for (int i = 0; i < RequiredMaterials.Length; i++)
            {
                RequiredMaterial required = RequiredMaterials[i];

                if (required.Material == null)
                    continue;

                Inventory.Instance.RemoveItem(required.Material, required.Amount);
            }
        }
    }
}