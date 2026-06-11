using System;
using ShiftedSignal.Garden.Effects;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using UnityEngine;

namespace ShiftedSignal.Garden.GridSystem
{
    public class GrowBlock : MonoBehaviour
    {
        public enum GrowthStage
        {
            Barren,
            Ploughed,
            Planted,
            Growing1,
            Growing2,
            Ripe
        }

        public GrowthStage CurrentStage;

        public SpriteRenderer SR;
        public Sprite GridIndicator;
        public Sprite BlockActiveSprite;
        public Sprite SoilTilledSprite;
        public Sprite SoilWateredSprite;

        public SpriteRenderer CropSprite;

        public ItemData_Seed Seed;
        public bool IsWatered;

        public bool PreventUse;
        public bool IsActive;
        public bool IsActivationBlock;

        [SerializeField] private Vector2Int GridPosition;

        [SerializeField] private SpriteRenderer SelectionBox;
        [SerializeField] private LayerMask GrowBlockMask;
        [SerializeField] private LayerMask ObjectMask;
        public int health = 100;
        public GameObject SpawnedPlant;
        public bool HasBuildable;

        public void UpdateSelectionBoxColor()
        {
            SelectionBox.material.color = ColorManager.Instance.SelectionBoxBorder;
        }

        public void InstantiateBlock(ItemData_Seed seed)
        {
            Seed = seed;
        }

        public void TriggerActivationBlock()
        {
            IsActive = false;
            Glow(false);
            UpdateGridInfo();

            const int activationRange = 5;

            for (int x = GridPosition.x - activationRange; x <= GridPosition.x + activationRange; x++)
            {
                for (int y = GridPosition.y - activationRange; y <= GridPosition.y + activationRange; y++)
                {
                    Vector2Int targetPosition = new Vector2Int(x, y);

                    if (Vector2Int.Distance(GridPosition, targetPosition) > activationRange)
                        continue;

                    GrowBlock block = GridManager.Instance.GetBlock(x, y);

                    if (block == null || block == this)
                        continue;

                    block.SetActiveBlock(true);
                    block.SetSoilSprite();
                }
            }

            
        }
        
        public void SetActiveBlock(bool active)
        {
            if (active && ! this.IsActive && !PreventUse)
            {
                Bus<UnlockFarmingAreaEvent>.Raise(new UnlockFarmingAreaEvent());
            }

            this.IsActive = active;

            if (!this.IsActive)
                Glow(false);

            UpdateGridInfo();
        }
        
        public void AdvanceStage()
        {
            CurrentStage ++;

            if ((int)CurrentStage >= 6)
            {
                CurrentStage = GrowthStage.Barren;
            }
        }

        public void SetSoilSprite(bool saveInfo = true)
        {
            if (CurrentStage == GrowthStage.Barren)
                SR.sprite = IsActive ? BlockActiveSprite : null;
            else
                SR.sprite = IsWatered ? SoilWateredSprite : SoilTilledSprite;

            if (saveInfo)
                UpdateGridInfo();
        }

        public void UseContextAction(ItemData_Seed equippedSeed)
        {
            if (PreventUse || !IsActive || HasBuildable)
                return;

            if (IsActivationBlock)
            {
                TriggerActivationBlock();
                return;
            }

            if (CurrentStage == GrowthStage.Barren)
            {
                PloughSoil();
                return;
            }

            if (CurrentStage == GrowthStage.Ripe)
            {
                HarvestCrop();
                return;
            }

            if (CurrentStage == GrowthStage.Ploughed)
            {
                if (!IsWatered)
                {
                    WaterSoil();
                    return;
                }

                if (equippedSeed != null)
                {
                    if (Inventory.Instance.HasItem(equippedSeed))
                    {
                        if (equippedSeed.AllRestrictionsPass(GridPosition.x, GridPosition.y))
                        {
                            PlantCrop(equippedSeed);
                            Inventory.Instance.RemoveItem(equippedSeed);
                        }
                    }
                    return;
                }
            }

            if (CurrentStage == GrowthStage.Planted ||
                CurrentStage == GrowthStage.Growing1 ||
                CurrentStage == GrowthStage.Growing2)
            {
                if (!IsWatered)
                {
                    WaterSoil();
                }
            }
        }

        public void PloughSoil()
        {
            if (CurrentStage == GrowthStage.Barren)
            {
                //Check if block is clear
                if (!IsBlockClear()) return;

                CurrentStage = GrowthStage.Ploughed;
                
                // Explicitly set the block to an unwatered state
                IsWatered = false; 
                
                SetSoilSprite();
                SR.material.SetFloat("_Alpha", 1f);
                Player.Instance.GrassCutter.CutGrass(transform.position, GridManager.Instance.CellSize, CutShape.Box);
            }
        }

        private bool IsBlockClear()
        {
            
            float cellSize = GridManager.Instance.CellSize;

            Vector3 center = transform.position;
            Vector3 halfExtents = new Vector3(cellSize * 0.5f, cellSize * 0.5f, cellSize * 0.5f);
            
            bool any = Physics.CheckBox(center, halfExtents, Quaternion.identity, ObjectMask, QueryTriggerInteraction.Ignore);

            return !any;
        }

        public void WaterSoil()
        {
            if (CurrentStage > GrowthStage.Barren)
            {
                IsWatered = true;

                SetSoilSprite();
            }
        }

        public void PlantCrop(ItemData_Seed seed)
        {
            if (CurrentStage == GrowthStage.Ploughed && IsWatered)
            {
                InstantiateBlock(seed);
                CurrentStage = GrowthStage.Planted;
                UpdateCropSprite();  
            }
        }

        public void UpdateCropSprite(bool saveInfo = true)
        {
            switch(CurrentStage)
            {
                case GrowthStage.Barren:
                case GrowthStage.Ploughed:
                    CropSprite.sprite = null;
                    break;

                case GrowthStage.Planted:
                    CropSprite.sprite = Seed != null ? Seed.CropPlantedSprite : null;
                    break;

                case GrowthStage.Growing1:
                    CropSprite.sprite = Seed != null ? Seed.CropGrowing1Sprite : null;
                    break;

                case GrowthStage.Growing2:
                    CropSprite.sprite = Seed != null ? Seed.CropGrowing2Sprite : null;
                    break;

                case GrowthStage.Ripe:
                    CropSprite.sprite = Seed != null ? Seed.CropRipeSprite : null;
                    break;
            }

            if (saveInfo)
                UpdateGridInfo();
        }

        public void AdvanceCrop()
        {
            if (IsWatered == true)
            {
                if (CurrentStage == GrowthStage.Planted
                    || CurrentStage == GrowthStage.Growing1 
                    || CurrentStage == GrowthStage.Growing2)
                {
                    CurrentStage++;

                    IsWatered = false;
                    SetSoilSprite();
                    UpdateCropSprite();
                }
            }
        }

        public void HarvestCrop()
        {
            if(CurrentStage == GrowthStage.Ripe)
            {
                if (Seed.CropType == CropType.Resource)
                {
                    CurrentStage = GrowthStage.Ploughed;
                    IsWatered = false; // Reset water state upon harvesting
                    
                    SetSoilSprite();
                    CropSprite.sprite = null;
                    Seed.AddResourcesToInventory();
                    Seed = null;
                }
                else
                {
                    // TODO display harvest confirmation
                    //confirm
                    CurrentStage = GrowthStage.Ploughed;
                    IsWatered = false; // Reset water state upon harvesting
                    
                    SetSoilSprite();
                    CropSprite.sprite = null;
                    ObjectPoolManager.ReturnObjectToPool(SpawnedPlant);
                    SpawnedPlant = null;
                    Seed = null;
                    
                    // TODO add resources to inventory
                    //deny - do nothing
                }
            }
        }

        public void SetGridPosition(int x, int z)
        {
            GridPosition = new Vector2Int(x, z);
        }

        public void UpdateGridInfo()
        {
            GridInfo.Instance.UpdateInfo(this, GridPosition.x, GridPosition.y);
        }

        public void Glow(bool glow)
        {
            SelectionBox.enabled = glow;
        }

        public void DamageCrop(int damage)
        {
            Debug.Log("Crop being damaged");
            health -= damage;
            if (health <= 0)
            {
                CurrentStage = GrowthStage.Ploughed;
            }
            UpdateCropSprite();
            UpdateGridInfo();
        }

        /// <summary>
        /// Completely resets this grow block back to its default state.
        /// </summary>
        public void ResetBlock()
        {
            // Remove any spawned ripe plant
            if (SpawnedPlant != null)
            {
                ObjectPoolManager.ReturnObjectToPool(SpawnedPlant);
                SpawnedPlant = null;
            }

            // Reset crop data
            Seed = null;
            CropSprite.sprite = null;

            // Reset growth state
            CurrentStage = GrowthStage.Barren;
            IsWatered = false;

            // Reset flags
            PreventUse = false;
            HasBuildable = false;
            IsActivationBlock = false;
            IsActive = false;

            // Reset health
            health = 100;

            // Reset visuals
            SR.sprite = GridIndicator;

            if (SR.material != null)
            {
                SR.material.SetFloat("_Alpha", 1f);
            }

            // Remove selection glow
            Glow(false);

            // Save updated state
            UpdateGridInfo();
        }
    }
}