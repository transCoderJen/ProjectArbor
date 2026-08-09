using System;
using System.Collections.Generic;
using System.Linq;
using ShiftedSignal.Garden.GridSystem;
using UnityEngine;

namespace ShiftedSignal.Garden.ItemsAndInventory
{
    public enum CropType
    {
        Harvestable,
        Living
    }

    public enum HarvestBehavior
    {
        ReplantRequired,
        PlantRemains
    }

    [Serializable]
    public class HarvestEntry
    {
        public ItemData Item;

        [Min(1)]
        public int Amount = 1;

        [Range(0f, 100f)]
        public float HarvestChance = 100f;
    }

    [CreateAssetMenu(
        fileName = "New Item Data",
        menuName = "Data/Seed")]
    public class ItemData_Seed : ItemData
    {
        public CropType CropType;

        public Sprite CropPlantedSprite,
            CropGrowing1Sprite,
            CropGrowing2Sprite,
            CropRipeSprite;

        public GameObject RipePlant;

        [Header("Harvest")]
        public List<HarvestEntry> Harvest = new();

        public PlacementRestrictionsSO[] Restrictions;

        [Header("Plant Health")]
        public int MaxPlantHealth = 100;

        [Header("Harvest Behavior")]
        public HarvestBehavior HarvestBehavior =
            HarvestBehavior.ReplantRequired;

        public GrowBlock.GrowthStage RegrowStage =
            GrowBlock.GrowthStage.Growing2;

        [Header("Growth Timing")]
        public float PlantedStageMinutes = 60f;
        public float Growing1StageMinutes = 90f;
        public float Growing2StageMinutes = 120f;
        public float growthAmount = 10f;

        [field: SerializeField]
        public float FertilizerGrowthMultiplier
        {
            get;
            private set;
        } = 1.2f;

        [Header("Water")]
        public bool RequiresWater = true;
        public int WaterUnitsPerApplication = 1;

        [Header("Fertilizer")]
        public int FertilizerUnitsPerApplication = 1;

        [Header("Dry Damage")]
        public float DryTolerance = 120f;
        public int DryDamageAmount = 1;
        public float DryDamageInterval = 30f;

        public void AddHarvestToInventory()
        {
            if (Inventory.Instance == null ||
                Harvest == null)
            {
                return;
            }

            foreach (HarvestEntry harvestEntry in Harvest)
            {
                if (harvestEntry == null ||
                    harvestEntry.Item == null ||
                    harvestEntry.Amount <= 0)
                {
                    continue;
                }

                float roll =
                    UnityEngine.Random.Range(
                        0f,
                        100f);

                if (roll > harvestEntry.HarvestChance)
                    continue;

                Inventory.Instance.AddItem(
                    harvestEntry.Item,
                    harvestEntry.Amount);
            }
        }

        public float GetStageDuration(
            GrowBlock.GrowthStage stage)
        {
            return stage switch
            {
                GrowBlock.GrowthStage.Planted =>
                    PlantedStageMinutes,

                GrowBlock.GrowthStage.Growing1 =>
                    Growing1StageMinutes,

                GrowBlock.GrowthStage.Growing2 =>
                    Growing2StageMinutes,

                _ => 0f
            };
        }

        public bool AllRestrictionsPass(
            int GridX,
            int GridY) =>
            Restrictions.Length == 0 ||
            Restrictions.All(
                restriction =>
                    restriction.CanPlace(
                        GridX,
                        GridY));
    }
}