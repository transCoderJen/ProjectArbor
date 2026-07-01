using System.Collections.Generic;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
using UnityEngine;

namespace ShiftedSignal.Garden.GridSystem
{
    public class GridInfo : Singleton<GridInfo>, ISaveManager
    {
        #region Fields

        [Header("Grid Save Data")]
        public bool HasGrid;

        public List<InfoRow> Grid = new();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (HasGrid)
            {
                if (GridManager.Instance != null)
                    GridManager.Instance.RequestGridRestore();

                return;
            }

            if (SaveManager.Instance != null &&
                SaveManager.Instance.gameData != null &&
                SaveManager.Instance.gameData.gridRows != null &&
                SaveManager.Instance.gameData.gridRows.Count > 0)
            {
                return;
            }

            if (GridManager.Instance != null &&
                GridManager.Instance.BlockRows != null &&
                GridManager.Instance.BlockRows.Count > 0)
            {
                CreateGrid();
            }
        }

        #endregion

        #region Grid Creation

        public void CreateGrid()
        {


            Grid.Clear();
            HasGrid = true;

            if (GridManager.Instance == null)
            {
                return;
            }

            for (int y = 0; y < GridManager.Instance.BlockRows.Count; y++)
            {
                Grid.Add(new InfoRow());

                for (int x = 0; x < GridManager.Instance.BlockRows[y].Blocks.Count; x++)
                {
                    GrowBlock physicalBlock = GridManager.Instance.BlockRows[y].Blocks[x];
                    BlockInfo newInfo = CreateInfoFromBlock(physicalBlock);

                    Grid[y].Blocks.Add(newInfo);
                }
            }
        }

        private BlockInfo CreateInfoFromBlock(GrowBlock block)
        {
            BlockInfo info = new()
            {
                CurrentStage = block.CurrentStage,
                IsWatered = block.IsWatered,
                IsActive = block.IsActive,
                Health = block.Health,
                SeedItemID = block.Seed != null ? block.Seed.ItemID : ""
            };

            SaveBuildableInfo(block, info, "CreateInfoFromBlock");

            return info;
        }

        #endregion

        #region Runtime Updates

        public void UpdateInfo(GrowBlock block, int xPos, int yPos)
        {
            if (block == null)
                return;

            if (!IsValidGridPosition(xPos, yPos))
                return;

            BlockInfo info = Grid[yPos].Blocks[xPos];

            info.CurrentStage = block.CurrentStage;
            info.IsWatered = block.IsWatered;
            info.IsActive = block.IsActive;
            info.Health = block.Health;
            info.SeedItemID = block.Seed != null ? block.Seed.ItemID : "";

            SaveBuildableInfo(block, info, "UpdateInfo");
        }

        public void UpdateInfoFromGrid()
        {
            EnsureGridMatchesScene();

            if (GridManager.Instance == null)
            {
                return;
            }

            for (int y = 0; y < GridManager.Instance.BlockRows.Count; y++)
            {
                for (int x = 0; x < GridManager.Instance.BlockRows[y].Blocks.Count; x++)
                {
                    GrowBlock block = GridManager.Instance.BlockRows[y].Blocks[x];
                    UpdateInfo(block, x, y);
                }
            }

        }

        private void SaveBuildableInfo(GrowBlock block, BlockInfo info, string source)
        {
            BaseBuilding buildable = block.CurrentBuildable;

            if (buildable == null)
            {
                // Debug.Log(
                //     $"SAVE BUILDABLE [{source}] {block.name}: No CurrentBuildable. " +
                //     $"ExistingSavedID='{info.BuildableItemID}'");

                if (!string.IsNullOrEmpty(info.BuildableItemID))
                {
                    // Debug.LogWarning(
                    //     $"SAVE BUILDABLE [{source}] {block.name}: Keeping old saved buildable ID " +
                    //     $"'{info.BuildableItemID}' because CurrentBuildable is null.");

                    return;
                }

                info.BuildableItemID = "";
                info.BuildableYRotation = 0f;
                info.BuildableHP = 0;
                return;
            }

            if (buildable.UnitSO == null)
            {
                // Debug.LogWarning(
                //     $"SAVE BUILDABLE [{source}] {block.name}: CurrentBuildable exists but has no BuildableData. " +
                //     $"BuildableObject='{buildable.name}', Type='{buildable.GetType().Name}', " +
                //     $"ExistingSavedID='{info.BuildableItemID}'");

                if (!string.IsNullOrEmpty(info.BuildableItemID))
                {
                    // Debug.LogWarning(
                    //     $"SAVE BUILDABLE [{source}] {block.name}: Keeping old saved buildable ID " +
                    //     $"'{info.BuildableItemID}' because BuildableData is null.");

                    return;
                }

                info.BuildableItemID = "";
                info.BuildableYRotation = 0f;
                info.BuildableHP = 0;
                return;
            }

            info.BuildableItemID = buildable.UnitSO.ItemID;
            info.BuildableYRotation = buildable.transform.eulerAngles.y;
            info.BuildableHP = buildable.CurrentHealth;

            // Debug.Log(
            //     $"SAVE BUILDABLE [{source}] {block.name}: Saved buildable. " +
            //     $"Object='{buildable.name}', Type='{buildable.GetType().Name}', " +
            //     $"ItemID='{info.BuildableItemID}', HP={info.BuildableHP}, " +
            //     $"MaxHealth={buildable.MaxHealth}, RotY={info.BuildableYRotation}");
        }

        #endregion

        #region Crop Growth

        [ContextMenu("Grow Crop")]
        public void GrowCrop()
        {
            for (int y = 0; y < Grid.Count; y++)
            {
                for (int x = 0; x < Grid[y].Blocks.Count; x++)
                {
                    BlockInfo block = Grid[y].Blocks[x];

                    if (!block.IsWatered)
                        continue;

                    switch (block.CurrentStage)
                    {
                        case GrowBlock.GrowthStage.Planted:
                            block.CurrentStage = GrowBlock.GrowthStage.Growing1;
                            break;

                        case GrowBlock.GrowthStage.Growing1:
                            block.CurrentStage = GrowBlock.GrowthStage.Growing2;
                            break;

                        case GrowBlock.GrowthStage.Growing2:
                            block.CurrentStage = GrowBlock.GrowthStage.Ripe;
                            break;
                    }

                    block.IsWatered = false;
                }
            }

            GridManager.Instance.UpdateGrid();
        }

        #endregion

        #region Validation

        private void EnsureGridMatchesScene()
        {
            if (Grid == null)
                Grid = new List<InfoRow>();

            if (GridManager.Instance == null)
            {
                return;
            }

            if (Grid.Count != GridManager.Instance.BlockRows.Count)
            {
                return;
            }

            for (int y = 0; y < GridManager.Instance.BlockRows.Count; y++)
            {
                if (Grid[y].Blocks.Count != GridManager.Instance.BlockRows[y].Blocks.Count)
                {
                    return;
                }
            }
        }

        private bool IsValidGridPosition(int x, int y)
        {
            if (Grid == null)
                return false;

            if (y < 0 || y >= Grid.Count)
                return false;

            if (Grid[y] == null || Grid[y].Blocks == null)
                return false;

            if (x < 0 || x >= Grid[y].Blocks.Count)
                return false;

            return true;
        }

        #endregion

        #region Save / Load

        public void LoadData(GameData data)
        {
            if (data.gridRows == null || data.gridRows.Count <= 0)
            {
                return;
            }

            Grid = data.gridRows;
            HasGrid = true;

            if (GridManager.Instance != null)
            {
                GridManager.Instance.RequestGridRestore();
            }
        }

        public void SaveData(ref GameData data)
        {
            UpdateInfoFromGrid();

            data.gridRows = Grid;
        }

        #endregion

        #region Reset

        public void DestroyGrid()
        {
            Grid.Clear();
            HasGrid = false;
        }

        #endregion
    }

    [System.Serializable]
    public class BlockInfo
    {
        [Header("Crop State")]
        public bool IsWatered;
        public GrowBlock.GrowthStage CurrentStage;
        public string SeedItemID;
        public int Health;

        [Header("Grid State")]
        public bool IsActive;

        [Header("Buildable State")]
        public string BuildableItemID;
        public float BuildableYRotation;
        public int BuildableHP;
    }

    [System.Serializable]
    public class InfoRow
    {
        public List<BlockInfo> Blocks = new();
    }
}