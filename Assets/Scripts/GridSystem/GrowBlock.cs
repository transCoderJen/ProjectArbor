using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Effects;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
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

        [Header("Buildable")]
        [SerializeField] private BaseBuilding currentBuildable;

        public BaseBuilding CurrentBuildable
        {
            get
            {
                if (currentBuildable == null)
                    return null;

                return currentBuildable;
            }

            private set
            {
                currentBuildable = value;
            }
        }

        public bool HasBuildable => CurrentBuildable != null;

        public Vector2Int GetGridPosition()
        {
            return GridPosition;
        }

        public void SetGridPosition(int x, int z)
        {
            GridPosition = new Vector2Int(x, z);
        }

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

        public void UpdateSelectionBoxColor()
        {
            if (SelectionBox == null || ColorManager.Instance == null)
                return;

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

        public void AdvanceStage()
        {
            CurrentStage++;

            if ((int)CurrentStage >= 6)
                CurrentStage = GrowthStage.Barren;
        }

        public void SetSoilSprite(bool saveInfo = true)
        {
            if (SR == null)
                return;

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
                    if (Inventory.Instance.HasItem(equippedSeed) &&
                        equippedSeed.AllRestrictionsPass(GridPosition.x, GridPosition.y))
                    {
                        PlantCrop(equippedSeed);
                        Inventory.Instance.RemoveItem(equippedSeed);
                    }

                    return;
                }
            }

            if (CurrentStage == GrowthStage.Planted ||
                CurrentStage == GrowthStage.Growing1 ||
                CurrentStage == GrowthStage.Growing2)
            {
                if (!IsWatered)
                    WaterSoil();
            }
        }

        public void PloughSoil()
        {
            if (CurrentStage != GrowthStage.Barren)
                return;

            if (!IsBlockClear())
                return;

            CurrentStage = GrowthStage.Ploughed;
            IsWatered = false;

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
            }

            if (saveInfo)
                UpdateGridInfo();
        }

        public void AdvanceCrop()
        {
            if (!IsWatered)
                return;

            if (CurrentStage == GrowthStage.Planted ||
                CurrentStage == GrowthStage.Growing1 ||
                CurrentStage == GrowthStage.Growing2)
            {
                CurrentStage++;
                IsWatered = false;

                SetSoilSprite();
                UpdateCropSprite();
            }
        }

        public void HarvestCrop()
        {
            if (CurrentStage != GrowthStage.Ripe)
                return;

            if (Seed == null)
                return;

            if (Seed.CropType == CropType.Resource)
            {
                CurrentStage = GrowthStage.Ploughed;
                IsWatered = false;

                SetSoilSprite();

                if (CropSprite != null)
                    CropSprite.sprite = null;

                Seed.AddResourcesToInventory();
                Seed = null;
            }
            else
            {
                ResetCrop();
                CurrentStage = GrowthStage.Ploughed;

                SetSoilSprite();

                if (SpawnedPlant != null)
                {
                    ObjectPoolManager.ReturnObjectToPool(SpawnedPlant);
                    SpawnedPlant = null;
                }

                Seed = null;
            }
        }

        public void UpdateGridInfo()
        {
            if (GridInfo.Instance == null)
                return;

            GridInfo.Instance.UpdateInfo(this, GridPosition.x, GridPosition.y);
        }

        public void Glow(bool glow)
        {
            if (SelectionBox != null)
                SelectionBox.enabled = glow;
        }

        public void DamageCrop(int damage)
        {
            Debug.Log("Crop being damaged");

            health -= damage;

            if (health <= 0)
            {
                ResetCrop();
                CurrentStage = GrowthStage.Ploughed;
            }

            UpdateCropSprite();
            UpdateGridInfo();
        }

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
            health = 100;

            SetSoilSprite(false);

            UpdateGridInfo();
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
    }
}