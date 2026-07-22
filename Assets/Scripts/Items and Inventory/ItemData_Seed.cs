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

    [CreateAssetMenu(fileName = "New Item Data", menuName = "Data/Seed")]
    public class ItemData_Seed : ItemData
    {
        public CropType CropType;
        public Sprite CropPlantedSprite, 
                        CropGrowing1Sprite, 
                        CropGrowing2Sprite, 
                        CropRipeSprite;

        public GameObject RipePlant;

        public List<ItemData> Harvest = new List<ItemData>();

        public PlacementRestrictionsSO[] Restrictions;

        [Header("Plant Health")]
        public int MaxPlantHealth = 100;

        [Header("Harvest Behavior")]
        public HarvestBehavior HarvestBehavior = HarvestBehavior.ReplantRequired;
        [Header("Harvest Behavior")]
        public GrowBlock.GrowthStage RegrowStage = GrowBlock.GrowthStage.Growing2;

        [Header("Growth Timing")]
        public float PlantedStageMinutes = 60f;
        public float Growing1StageMinutes = 90f;
        public float Growing2StageMinutes = 120f;
        public float growthAmount = 10f;
        [field: SerializeField] public float FertilizerGrowthMultiplier { get; private set; } = 1.2f;

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
            foreach (ItemData data in Harvest)
            {
                Inventory.Instance.AddItem(data);
            }
        }

        public float GetStageDuration(GrowBlock.GrowthStage stage)
        {
            return stage switch
            {
                GrowBlock.GrowthStage.Planted => PlantedStageMinutes,
                GrowBlock.GrowthStage.Growing1 => Growing1StageMinutes,
                GrowBlock.GrowthStage.Growing2 => Growing2StageMinutes,
                _ => 0f
            };
        }

        public bool AllRestrictionsPass(int GridX, int GridY) =>
            Restrictions.Length == 0 || Restrictions.All(restriction => restriction.CanPlace(GridX, GridY));
    }
}