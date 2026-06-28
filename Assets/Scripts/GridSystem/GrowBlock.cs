using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Effects;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Units;
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
            Ripe,
            Dead
        }

        #region Runtime State

        public GrowthStage CurrentStage;

        public int CurrentPlantHealth;
        public float GrowthProgress;
        public bool IsWatered;
        public bool IsFertilized;

        public bool PreventUse;
        public bool IsActive;
        public bool IsActivationBlock;

        public int Health = 100;

        #endregion

        #region Plant Data

        public ItemData_Seed Seed;
        public GameObject SpawnedPlant;
        private Worker reservedWorker;

        public bool IsReserved => reservedWorker != null;

        #endregion

        #region Visuals

        public SpriteRenderer SR;
        public SpriteRenderer CropSprite;

        [SerializeField] private SpriteRenderer SelectionBox;

        public Sprite GridIndicator;
        public Sprite BlockActiveSprite;
        public Sprite SoilTilledSprite;
        public Sprite SoilWateredSprite;

        #endregion

        #region Grid

        [SerializeField] private Vector2Int GridPosition;
        [SerializeField] private LayerMask ObjectMask;

        #endregion

        #region Buildables

        [Header("Buildable")]
        [SerializeField] private BaseBuilding currentBuildable;

        public BaseBuilding CurrentBuildable => currentBuildable;
        public bool HasBuildable => currentBuildable != null;

        #endregion

        #region Plant Queries

        public bool HasCrop =>
            Seed != null;

        public bool IsGrowing =>
            HasCrop &&
            CurrentStage >= GrowthStage.Planted &&
            CurrentStage <= GrowthStage.Growing2;

        public bool IsDead =>
            CurrentStage == GrowthStage.Dead;

        public bool IsReadyToHarvest =>
            HasCrop &&
            CurrentStage == GrowthStage.Ripe;

        public bool CanReceiveFarmCare =>
            HasCrop &&
            IsGrowing &&
            !IsDead &&
            !HasBuildable &&
            IsActive;

        public bool NeedsWater =>
            CanReceiveFarmCare &&
            Seed.RequiresWater &&
            !IsWatered;

        public bool NeedsFertilizer =>
            HasCrop &&
            IsGrowing &&
            !IsDead &&
            Seed.RequiresFertilizer &&
            !IsFertilized;

        public bool HasActionableFarmTask =>
            CanReceiveFarmCare &&
            (NeedsWater || NeedsFertilizer);
        
        public bool IsReservedByAnotherWorker(Worker worker)
        {
            return reservedWorker != null && reservedWorker != worker;
        }

        public bool TryReserveFarmTask(Worker worker)
        {
            if (worker == null)
                return false;

            if (IsReservedByAnotherWorker(worker))
                return false;

            if (!HasActionableFarmTask)
                return false;

            reservedWorker = worker;
            return true;
        }

        public void ReleaseFarmTask(Worker worker)
        {
            if (worker == null)
                return;

            if (reservedWorker != worker)
                return;

            reservedWorker = null;
        }
        #endregion

        #region Grid Position

        public Vector2Int GetGridPosition()
        {
            return GridPosition;
        }

        public void SetGridPosition(int x, int z)
        {
            GridPosition = new Vector2Int(x, z);
        }

        #endregion

        #region Buildable State

        public void SetBuildable(BaseBuilding buildable)
        {
            currentBuildable = buildable;

            IsActive = false;

            HideGroundSprite();
            UpdateGridInfo();
        }

        public void ClearBuildable(bool reactivateBlock = true)
        {
            currentBuildable = null;

            if (reactivateBlock && !PreventUse)
                IsActive = true;

            ShowGroundSprite();
            SetSoilSprite(false);
            Glow(false);

            UpdateGridInfo();
        }

        public void ClearBuildableWithoutSaving(bool reactivateBlock = true)
        {
            currentBuildable = null;

            if (reactivateBlock && !PreventUse)
                IsActive = true;

            ShowGroundSprite();
            SetSoilSprite(false);
            Glow(false);
        }

        #endregion

        #region Activation

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
                    Vector2Int targetPosition = new(x, y);

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
            if (active && !IsActive && !PreventUse)
                Bus<UnlockFarmingAreaEvent>.Raise(new UnlockFarmingAreaEvent());

            IsActive = active;

            if (!IsActive)
                Glow(false);

            UpdateGridInfo();
        }

        #endregion

        #region Player Interaction

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

            if (CurrentStage == GrowthStage.Dead)
            {
                ClearDeadCrop();
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
                    if (Inventory.Instance.HasItem(equippedSeed) &&
                        equippedSeed.AllRestrictionsPass(GridPosition.x, GridPosition.y))
                    {
                        PlantCrop(equippedSeed);
                        Inventory.Instance.RemoveItem(equippedSeed);
                    }

                    return;
                }
            }

            if (IsGrowing && !IsWatered)
                WaterSoil();
        }

        #endregion

        #region Soil And Planting

        public void PloughSoil()
        {
            if (CurrentStage != GrowthStage.Barren)
                return;

            if (!IsBlockClear())
                return;

            CurrentStage = GrowthStage.Ploughed;
            IsWatered = false;
            IsFertilized = false;
            GrowthProgress = 0f;
            CurrentPlantHealth = 0;

            SetSoilSprite();

            if (SR != null && SR.material != null)
                SR.material.SetFloat("_Alpha", 1f);

            if (Player.Instance != null && Player.Instance.GrassCutter != null)
            {
                Player.Instance.GrassCutter.CutGrass(
                    transform.position,
                    GridManager.Instance.CellSize,
                    CutShape.Box);
            }
        }

        public void WaterSoil()
        {
            TryWater();
        }

        public bool TryWater()
        {
            if (CurrentStage == GrowthStage.Ploughed)
            {
                IsWatered = true;
                UpdateVisuals();
                return true;
            }

            if (!NeedsWater)
                return false;

            IsWatered = true;
            UpdateVisuals();

            return true;
        }

        public bool TryFertilize()
        {
            if (!NeedsFertilizer)
                return false;

            IsFertilized = true;
            UpdateVisuals();

            return true;
        }

        public void PlantCrop(ItemData_Seed seed)
        {
            if (seed == null)
                return;

            if (CurrentStage != GrowthStage.Ploughed)
                return;

            if (!IsWatered)
                return;

            Seed = seed;
            CurrentStage = GrowthStage.Planted;

            CurrentPlantHealth = Seed.MaxPlantHealth;
            GrowthProgress = 0f;
            IsFertilized = false;

            UpdateVisuals();
        }

        #endregion

        #region Growth

        public void TickGrowth()
        {
            if (!HasCrop || !IsGrowing)
                return;

            if (Seed.RequiresWater && !IsWatered)
                return;

            if (Seed.RequiresFertilizer && !IsFertilized)
                return;

            GrowthProgress += 10f;

            if (GrowthProgress < Seed.GetStageDuration(CurrentStage))
                return;

            AdvanceCrop();
        }

        public void AdvanceStage()
        {
            if (CurrentStage == GrowthStage.Dead)
                return;

            CurrentStage++;

            if (CurrentStage > GrowthStage.Ripe)
                CurrentStage = GrowthStage.Barren;
        }

        public void AdvanceCrop()
        {
            if (!HasCrop)
                return;

            if (!IsWatered)
                return;

            if (Seed.RequiresFertilizer && !IsFertilized)
                return;

            if (CurrentStage == GrowthStage.Planted ||
                CurrentStage == GrowthStage.Growing1 ||
                CurrentStage == GrowthStage.Growing2)
            {
                CurrentStage++;
                IsWatered = false;
                IsFertilized = false;
                GrowthProgress = 0f;

                UpdateVisuals();
            }
        }

        public void DamageCrop(int damage)
        {
            if (!HasCrop)
                return;

            CurrentPlantHealth -= damage;

            if (CurrentPlantHealth <= 0)
            {
                CurrentPlantHealth = 0;
                CurrentStage = GrowthStage.Dead;
                IsWatered = false;
                IsFertilized = false;
                GrowthProgress = 0f;
            }

            UpdateVisuals();
        }

        public void ClearDeadCrop()
        {
            if (!IsDead)
                return;

            ResetCrop();
            CurrentStage = GrowthStage.Ploughed;
            UpdateVisuals();
        }

        #endregion

        #region Harvesting

        public void HarvestCrop()
        {
            if (CurrentStage != GrowthStage.Ripe)
                return;

            if (Seed == null)
                return;

            if (Seed.CropType == CropType.Harvestable)
            {
                Seed.AddHarvestToInventory();

                if (Seed.HarvestBehavior == HarvestBehavior.PlantRemains)
                {
                    CurrentStage = Seed.RegrowStage;
                    IsWatered = false;
                    IsFertilized = false;
                    GrowthProgress = 0f;
                    CurrentPlantHealth = Seed.MaxPlantHealth;

                    UpdateVisuals();
                    return;
                }

                Seed = null;
                CurrentStage = GrowthStage.Ploughed;
                IsWatered = false;
                IsFertilized = false;
                GrowthProgress = 0f;
                CurrentPlantHealth = 0;

                if (CropSprite != null)
                    CropSprite.sprite = null;

                UpdateVisuals();
                return;
            }

            ResetCrop();
            CurrentStage = GrowthStage.Ploughed;
            UpdateVisuals();
        }

        #endregion

        #region Visuals

        public void SetSoilSprite(bool saveInfo = true)
        {
            if (SR == null)
                return;

            if (CurrentStage == GrowthStage.Barren)
            {
                SR.sprite = IsActive ? BlockActiveSprite : null;
            }
            else
            {
                SR.sprite = IsWatered ? SoilWateredSprite : SoilTilledSprite;
            }

            if (saveInfo)
                UpdateGridInfo();
        }

        public void UpdateCropSprite(bool saveInfo = true)
        {
            if (CropSprite == null)
                return;

            switch (CurrentStage)
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

                case GrowthStage.Dead:
                    CropSprite.sprite = Seed != null ? Seed.CropPlantedSprite : null;
                    break;
            }

            if (saveInfo)
                UpdateGridInfo();
        }

        private void UpdateVisuals(bool saveInfo = true)
        {
            SetSoilSprite(false);
            UpdateCropSprite(false);

            if (saveInfo)
                UpdateGridInfo();
        }

        public void UpdateSelectionBoxColor()
        {
            if (SelectionBox == null || ColorManager.Instance == null)
                return;

            SelectionBox.material.color = ColorManager.Instance.SelectionBoxBorder;
        }

        public void Glow(bool glow)
        {
            if (SelectionBox != null)
                SelectionBox.enabled = glow;
        }

        public void HideGroundSprite()
        {
            if (SR != null)
                SR.enabled = false;
        }

        public void ShowGroundSprite()
        {
            if (SR != null)
                SR.enabled = true;
        }

        #endregion

        #region Reset

        public void ResetBlock()
        {
            ResetCrop();

            PreventUse = false;
            IsActivationBlock = false;

            Glow(false);
            UpdateGridInfo();
        }

        public void ResetCrop()
        {
            if (SpawnedPlant != null)
            {
                ObjectPoolManager.ReturnObjectToPool(SpawnedPlant);
                SpawnedPlant = null;
            }

            Seed = null;

            if (CropSprite != null)
                CropSprite.sprite = null;

            CurrentStage = GrowthStage.Barren;
            IsWatered = false;
            IsFertilized = false;
            GrowthProgress = 0f;
            CurrentPlantHealth = 0;
            Health = 100;

            SetSoilSprite(false);
            UpdateGridInfo();
        }

        #endregion

        #region Helpers

        private bool IsBlockClear()
        {
            if (GridManager.Instance == null)
                return false;

            float cellSize = GridManager.Instance.CellSize;

            Vector3 center = transform.position;
            Vector3 halfExtents = new(
                cellSize * 0.5f,
                cellSize * 0.5f,
                cellSize * 0.5f);

            bool any = Physics.CheckBox(
                center,
                halfExtents,
                Quaternion.identity,
                ObjectMask,
                QueryTriggerInteraction.Ignore);

            return !any;
        }

        public void UpdateGridInfo()
        {
            if (GridInfo.Instance == null)
                return;

            GridInfo.Instance.UpdateInfo(this, GridPosition.x, GridPosition.y);
        }

        #endregion
    }
}