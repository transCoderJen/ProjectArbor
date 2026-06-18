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
            Debug.Log(
                $"[GridInfo Start] HasGrid={HasGrid} | " +
                $"GridRows={Grid?.Count ?? -1} | " +
                $"SavedBuildables={CountSavedBuildables()}");

            if (HasGrid)
            {
                Debug.Log("[GridInfo Start] Existing grid found. Requesting restore.");

                if (GridManager.Instance != null)
                    GridManager.Instance.RequestGridRestore();

                return;
            }

            if (SaveManager.Instance != null &&
                SaveManager.Instance.gameData != null &&
                SaveManager.Instance.gameData.gridRows != null &&
                SaveManager.Instance.gameData.gridRows.Count > 0)
            {
                Debug.Log(
                    $"[GridInfo Start] SaveManager has gridRows={SaveManager.Instance.gameData.gridRows.Count}. " +
                    "Waiting for LoadData.");

                return;
            }

            if (GridManager.Instance != null &&
                GridManager.Instance.BlockRows != null &&
                GridManager.Instance.BlockRows.Count > 0)
            {
                Debug.Log("[GridInfo Start] No saved grid found. Creating NEW grid.");

                CreateGrid();
            }
        }

        #endregion

        #region Grid Creation

        public void CreateGrid()
        {
            Debug.Log("[GridInfo CreateGrid] Creating grid from current scene.");

            Grid.Clear();
            HasGrid = true;

            if (GridManager.Instance == null)
            {
                Debug.LogWarning("[GridInfo CreateGrid] GridManager.Instance is null.");
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

            Debug.Log(
                $"[GridInfo CreateGrid] Complete | Rows={Grid.Count} | " +
                $"SavedBuildables={CountSavedBuildables()}");
        }

        private BlockInfo CreateInfoFromBlock(GrowBlock block)
        {
            BlockInfo info = new()
            {
                CurrentStage = block.CurrentStage,
                IsWatered = block.IsWatered,
                IsActive = block.IsActive,
                Health = block.health,
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
            info.Health = block.health;
            info.SeedItemID = block.Seed != null ? block.Seed.ItemID : "";

            SaveBuildableInfo(block, info, "UpdateInfo");
        }

        public void UpdateInfoFromGrid()
        {
            Debug.Log(
                $"[GridInfo UpdateInfoFromGrid] START | " +
                $"BeforeSavedBuildables={CountSavedBuildables()}");

            EnsureGridMatchesScene();

            if (GridManager.Instance == null)
            {
                Debug.LogWarning("[GridInfo UpdateInfoFromGrid] GridManager.Instance is null.");
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

            Debug.Log(
                $"[GridInfo UpdateInfoFromGrid] END | " +
                $"AfterSavedBuildables={CountSavedBuildables()}");
        }

        private void SaveBuildableInfo(GrowBlock block, BlockInfo info, string source)
        {
            BaseBuildable buildable = block.CurrentBuildable;

            if (buildable == null)
            {
                if (!string.IsNullOrEmpty(info.BuildableItemID))
                {
                    Debug.LogWarning(
                        $"[GridInfo SaveBuildableInfo] {source} runtime buildable missing at {block.GetGridPosition()} | " +
                        $"Keeping SavedID={info.BuildableItemID}");

                    return;
                }

                info.BuildableItemID = "";
                info.BuildableYRotation = 0f;
                info.BuildableHP = 0;
                return;
            }

            if (buildable.BuildableData == null)
            {
                if (!string.IsNullOrEmpty(info.BuildableItemID))
                {
                    Debug.LogWarning(
                        $"[GridInfo SaveBuildableInfo] {source} BuildableData missing at {block.GetGridPosition()} | " +
                        $"Buildable={buildable.name} | Keeping SavedID={info.BuildableItemID}",
                        buildable);

                    return;
                }

                Debug.LogWarning(
                    $"[GridInfo SaveBuildableInfo] {source} found buildable with NULL data at {block.GetGridPosition()} | " +
                    $"Buildable={buildable.name}",
                    buildable);

                info.BuildableItemID = "";
                info.BuildableYRotation = 0f;
                info.BuildableHP = 0;
                return;
            }

            info.BuildableItemID = buildable.BuildableData.ItemID;
            info.BuildableYRotation = buildable.transform.eulerAngles.y;
            info.BuildableHP = buildable.CurrentHP;

            Debug.Log(
                $"[GridInfo SaveBuildableInfo] {source} SAVED buildable at {block.GetGridPosition()} | " +
                $"Buildable={buildable.name} | ItemID={info.BuildableItemID} | HP={info.BuildableHP}");
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
                Debug.LogWarning("[GridInfo EnsureGridMatchesScene] GridManager.Instance is null.");
                return;
            }

            if (Grid.Count != GridManager.Instance.BlockRows.Count)
            {
                Debug.LogWarning(
                    $"[GridInfo EnsureGridMatchesScene] ROW MISMATCH | " +
                    $"SavedRows={Grid.Count} | SceneRows={GridManager.Instance.BlockRows.Count} | " +
                    $"SavedBuildablesBefore={CountSavedBuildables()} | NOT recreating during debug.");

                return;
            }

            for (int y = 0; y < GridManager.Instance.BlockRows.Count; y++)
            {
                if (Grid[y].Blocks.Count != GridManager.Instance.BlockRows[y].Blocks.Count)
                {
                    Debug.LogWarning(
                        $"[GridInfo EnsureGridMatchesScene] COLUMN MISMATCH | Row={y} | " +
                        $"SavedColumns={Grid[y].Blocks.Count} | " +
                        $"SceneColumns={GridManager.Instance.BlockRows[y].Blocks.Count} | " +
                        $"SavedBuildablesBefore={CountSavedBuildables()} | NOT recreating during debug.");

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
            Debug.Log(
                $"[GridInfo LoadData] IncomingRows={data.gridRows?.Count ?? 0} | " +
                $"IncomingBuildables={CountBuildablesInRows(data.gridRows)}");

            if (data.gridRows == null || data.gridRows.Count <= 0)
            {
                Debug.Log("[GridInfo LoadData] No gridRows found. Skipping.");
                return;
            }

            Grid = data.gridRows;
            HasGrid = true;

            Debug.Log(
                $"[GridInfo LoadData] Loaded Rows={Grid.Count} | " +
                $"LoadedBuildables={CountSavedBuildables()}");

            if (GridManager.Instance != null)
            {
                Debug.Log("[GridInfo LoadData] Requesting GridManager restore.");
                GridManager.Instance.RequestGridRestore();
            }
            else
            {
                Debug.Log("[GridInfo LoadData] GridManager null. Restore will wait for GridInfo.Start/GridManager.Start.");
            }
        }

        public void SaveData(ref GameData data)
        {
            Debug.Log(
                $"[GridInfo SaveData] START | CurrentSavedBuildables={CountSavedBuildables()}");

            UpdateInfoFromGrid();

            Debug.Log(
                $"[GridInfo SaveData] AFTER UpdateInfoFromGrid | CurrentSavedBuildables={CountSavedBuildables()}");

            data.gridRows = Grid;

            Debug.Log(
                $"[GridInfo SaveData] WROTE TO GAMEDATA | GameDataBuildables={CountBuildablesInRows(data.gridRows)}");
        }

        #endregion

        #region Reset

        public void DestroyGrid()
        {
            Debug.LogWarning(
                $"[GridInfo DestroyGrid] Destroying grid | PreviousRows={Grid?.Count ?? -1} | " +
                $"PreviousBuildables={CountSavedBuildables()}");

            Grid.Clear();
            HasGrid = false;
        }

        #endregion

        #region Debug Helpers

        [ContextMenu("Debug Print Saved Buildables")]
        public void DebugPrintSavedBuildables()
        {
            if (Grid == null || Grid.Count == 0)
            {
                Debug.Log("[GridInfo] No saved grid data.");
                return;
            }

            int count = 0;

            for (int y = 0; y < Grid.Count; y++)
            {
                for (int x = 0; x < Grid[y].Blocks.Count; x++)
                {
                    BlockInfo info = Grid[y].Blocks[x];

                    if (string.IsNullOrEmpty(info.BuildableItemID))
                        continue;

                    count++;

                    Debug.Log(
                        $"[GridInfo Saved Buildable] ({x},{y}) " +
                        $"ItemID={info.BuildableItemID} " +
                        $"HP={info.BuildableHP} " +
                        $"Rotation={info.BuildableYRotation}");
                }
            }

            Debug.Log($"[GridInfo] Total saved buildables: {count}");
        }

        private int CountSavedBuildables()
        {
            return CountBuildablesInRows(Grid);
        }

        private int CountBuildablesInRows(List<InfoRow> rows)
        {
            if (rows == null)
                return 0;

            int count = 0;

            for (int y = 0; y < rows.Count; y++)
            {
                if (rows[y] == null || rows[y].Blocks == null)
                    continue;

                for (int x = 0; x < rows[y].Blocks.Count; x++)
                {
                    if (rows[y].Blocks[x] == null)
                        continue;

                    if (!string.IsNullOrEmpty(rows[y].Blocks[x].BuildableItemID))
                        count++;
                }
            }

            return count;
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