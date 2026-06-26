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

        [Header("Growth Timing")]
        public float PlantedStageDuration = 60f;
        public float Growing1StageDuration = 90f;
        public float Growing2StageDuration = 120f;

        [Header("Water")]
        public bool RequiresWater = true;
        public int WaterUnitsPerApplication = 1;

        [Header("Fertilizer")]
        public bool RequiresFertilizer;
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
                GrowBlock.GrowthStage.Planted => PlantedStageDuration,
                GrowBlock.GrowthStage.Growing1 => Growing1StageDuration,
                GrowBlock.GrowthStage.Growing2 => Growing2StageDuration,
                _ => 0f
            };
        }

        public bool AllRestrictionsPass(int GridX, int GridY) =>
            Restrictions.Length == 0 || Restrictions.All(restriction => restriction.CanPlace(GridX, GridY));
    }
}