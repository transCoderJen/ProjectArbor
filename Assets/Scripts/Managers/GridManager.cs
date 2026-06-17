using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Misc;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShiftedSignal.Garden.Managers
{
    public class GridManager : Singleton<GridManager>
    {
        #region Fields

        [Header("Grid Settings")]
        [field: SerializeField] public float CellSize { get; private set; } = 4f;

        [SerializeField] private Transform MinPoint;
        [SerializeField] private Transform MaxPoint;
        [SerializeField] private GrowBlock BaseGridBlock;
        [SerializeField] private Transform GridParent;
        [SerializeField] private Vector2Int gridSize;

        [Header("Layers")]
        [SerializeField] private LayerMask GridBlockers;
        [SerializeField] private LayerMask InteractionLayer;

        [Header("Buildable Restore Database")]
        [SerializeField] private List<BuildableData> buildableDatabase = new();

        [Header("Debug")]
        [SerializeField] private bool logGridRestore;
        [SerializeField] private bool logBuildableRestore;

        [Header("Runtime")]
        public List<BlockRow> BlockRows = new();

        private GrowBlock currentHoveredBlock;
        private Coroutine restoreRoutine;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            RequestGridRestore();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            FillBuildableDatabase();
        }
#endif

        private void Update()
        {
            UpdateHoveredBlock();
        }

        #endregion

        #region Restore Request

        public void RequestGridRestore()
        {
            if (restoreRoutine != null)
                StopCoroutine(restoreRoutine);

            restoreRoutine = StartCoroutine(RestoreGridWhenReady());

            Debug.Log("[GridManager] RequestGridRestore called.");
        }

        private IEnumerator RestoreGridWhenReady()
        {
            yield return null;

            int attempts = 0;
            const int maxAttempts = 30;

            while (attempts < maxAttempts)
            {
                if (CanRestoreGrid())
                {
                    UpdateGrid();
                    restoreRoutine = null;
                    yield break;
                }

                attempts++;
                yield return null;
            }

            Debug.LogWarning(
                "[GridManager] Could not restore grid. GridInfo or BlockRows were not ready.",
                this);

            restoreRoutine = null;
        }

        private bool CanRestoreGrid()
        {
            if (GridInfo.Instance == null)
                return false;

            if (!GridInfo.Instance.HasGrid)
                return false;

            if (GridInfo.Instance.Grid == null || GridInfo.Instance.Grid.Count == 0)
                return false;

            if (BlockRows == null || BlockRows.Count == 0)
                return false;

            return true;
        }

        #endregion

        #region Hover

        private void UpdateHoveredBlock()
        {
            if (PlayerManager.Instance == null || PlayerManager.Instance.Player == null)
                return;

            GrowBlock newHoveredBlock = PlayerManager.Instance.Player.UsingController
                ? GetBlockController()
                : GetBlock();

            if (newHoveredBlock == currentHoveredBlock)
                return;

            if (currentHoveredBlock != null)
                currentHoveredBlock.Glow(false);

            currentHoveredBlock = newHoveredBlock;

            if (currentHoveredBlock != null &&
                currentHoveredBlock.IsActive &&
                !currentHoveredBlock.PreventUse)
            {
                currentHoveredBlock.Glow(true);
            }
        }

        #endregion

        #region Generate / Destroy Grid

        [ContextMenu("Generate Grid")]
        private void GenerateGrid()
        {
            DestroyGrid();
            CreateNewGridParent();

            MinPoint.position = SnapToGrid(MinPoint.position);
            MaxPoint.position = SnapToGrid(MaxPoint.position);

            float halfCellSize = CellSize / 2f;
            Vector3 startPoint = MinPoint.position + new Vector3(halfCellSize, 0f, halfCellSize);

            gridSize = new Vector2Int(
                Mathf.RoundToInt((MaxPoint.position.x - MinPoint.position.x) / CellSize),
                Mathf.RoundToInt((MaxPoint.position.z - MinPoint.position.z) / CellSize));

            for (int z = 0; z < gridSize.y; z++)
            {
                BlockRows.Add(new BlockRow());

                for (int x = 0; x < gridSize.x; x++)
                {
                    Vector3 spawnPos =
                        startPoint + new Vector3(x * CellSize, transform.position.y, z * CellSize);

                    GrowBlock newBlock = Instantiate(
                        BaseGridBlock,
                        spawnPos,
                        Quaternion.Euler(90f, 0f, 0f),
                        GridParent);

                    newBlock.Glow(false);
                    newBlock.transform.localScale = Vector3.one * (CellSize / 4f);
                    newBlock.SetGridPosition(x, z);
                    newBlock.IsActive = false;

                    if (Physics.CheckBox(
                            spawnPos,
                            GetCellHalfExtents(),
                            Quaternion.identity,
                            GridBlockers,
                            QueryTriggerInteraction.Collide))
                    {
                        newBlock.PreventUse = true;
                        newBlock.gameObject.SetActive(false);
                    }

                    BlockRows[z].Blocks.Add(newBlock);
                }
            }

            if (GridInfo.Instance != null)
                GridInfo.Instance.CreateGrid();
        }

        [ContextMenu("Destroy Grid")]
        private void DestroyGrid()
        {
            BlockRows.Clear();

            if (GridParent != null)
            {
                DestroyImmediate(GridParent.gameObject);
                GridParent = null;
            }

            if (GridInfo.Instance != null)
                GridInfo.Instance.DestroyGrid();
        }

        private void CreateNewGridParent()
        {
            GameObject newParent = new("Grid Parent");
            newParent.transform.SetParent(transform);
            newParent.transform.localPosition = Vector3.zero;
            newParent.transform.localRotation = Quaternion.identity;
            newParent.transform.localScale = Vector3.one;

            GridParent = newParent.transform;
        }

        #endregion

        #region Grid Update / Restore

        [ContextMenu("Update Grid")]
        public void UpdateGrid()
        {
            Debug.Log(
                $"[GridManager] UpdateGrid | " +
                $"HasGrid={GridInfo.Instance?.HasGrid} | " +
                $"GridRows={GridInfo.Instance?.Grid?.Count ?? -1} | " +
                $"BlockRows={BlockRows?.Count ?? -1}"
            );
            if (!CanRestoreGrid())
            {
                if (logGridRestore)
                    Debug.Log("[GridManager] UpdateGrid skipped. Grid not ready.", this);

                return;
            }

            int rowCount = Mathf.Min(BlockRows.Count, GridInfo.Instance.Grid.Count);

            for (int y = 0; y < rowCount; y++)
            {
                if (BlockRows[y] == null || GridInfo.Instance.Grid[y] == null)
                    continue;

                int columnCount = Mathf.Min(
                    BlockRows[y].Blocks.Count,
                    GridInfo.Instance.Grid[y].Blocks.Count);

                for (int x = 0; x < columnCount; x++)
                {
                    GrowBlock block = BlockRows[y].Blocks[x];
                    BlockInfo storedBlock = GridInfo.Instance.Grid[y].Blocks[x];

                    if (block == null || storedBlock == null)
                        continue;

                    RestoreBlockState(block, storedBlock);
                    RestoreSeed(block, storedBlock);
                    RestoreBuildableFromInfo(block, storedBlock);

                    block.SetSoilSprite(false);
                    block.UpdateCropSprite(false);
                    block.Glow(false);
                }
            }

            RefreshAllFencePostConnections();

            if (logGridRestore)
                Debug.Log("[GridManager] Grid restore complete.", this);
        }

        private void RestoreBlockState(GrowBlock block, BlockInfo storedBlock)
        {
            block.IsWatered = storedBlock.IsWatered;
            block.CurrentStage = storedBlock.CurrentStage;
            block.IsActive = storedBlock.IsActive;
            block.health = storedBlock.Health;
        }

        private void RestoreSeed(GrowBlock block, BlockInfo storedBlock)
        {
            block.Seed = null;

            if (string.IsNullOrEmpty(storedBlock.SeedItemID))
                return;

            if (Inventory.Instance == null)
                return;

            foreach (ItemData item in Inventory.Instance.itemDataBase)
            {
                if (item == null)
                    continue;

                if (item.ItemID == storedBlock.SeedItemID)
                {
                    block.Seed = item as ItemData_Seed;
                    return;
                }
            }
        }

        private void RestoreBuildableFromInfo(GrowBlock block, BlockInfo info)
        {
            if (block == null || info == null)
                return;

            if (block.CurrentBuildable != null)
                Destroy(block.CurrentBuildable.gameObject);

            block.ClearBuildableWithoutSaving(false);

            if (string.IsNullOrEmpty(info.BuildableItemID))
                return;

            BuildableData buildableData = FindBuildableDataByItemID(info.BuildableItemID);

            if (buildableData == null)
            {
                Debug.LogWarning(
                    $"[RestoreBuildable] No BuildableData found for ItemID: {info.BuildableItemID}",
                    this);

                DebugLogBuildableDatabase();
                return;
            }

            if (buildableData.BuildablePrefab == null)
            {
                Debug.LogWarning(
                    $"[RestoreBuildable] BuildableData has no prefab: {buildableData.name}",
                    buildableData);

                return;
            }

            GameObject builtObject = Instantiate(
                buildableData.BuildablePrefab,
                block.transform.position,
                Quaternion.Euler(0f, info.BuildableYRotation, 0f));

            BaseBuildable buildable = builtObject.GetComponent<BaseBuildable>();

            if (buildable == null)
            {
                Debug.LogWarning(
                    $"[RestoreBuildable] Prefab missing BaseBuildable: {builtObject.name}",
                    builtObject);

                Destroy(builtObject);
                return;
            }

            buildable.SetOccupiedBlock(block);
            buildable.RestoreFromSave(info.BuildableHP);

            block.SetBuildable(buildable);

            if (buildable is FencePost2D)
            {
                FencePost2D.RefreshNeighbors(block);
            }

            if (!string.IsNullOrEmpty(info.BuildableItemID))
            {
                Debug.Log(
                    $"[RestoreBuildable] Attempting restore at {block.GetGridPosition()} " +
                    $"ItemID={info.BuildableItemID}"
                );
            }
        }

        private BuildableData FindBuildableDataByItemID(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
                return null;

            foreach (BuildableData buildable in buildableDatabase)
            {
                if (buildable == null)
                    continue;

                if (buildable.ItemID == itemID)
                    return buildable;
            }

            return null;
        }

        private void RefreshAllFencePostConnections()
        {
            for (int y = 0; y < BlockRows.Count; y++)
            {
                for (int x = 0; x < BlockRows[y].Blocks.Count; x++)
                {
                    GrowBlock block = BlockRows[y].Blocks[x];

                    if (block == null)
                        continue;

                    if (block.CurrentBuildable is FencePost2D fencePost)
                        fencePost.RefreshConnections(block);
                }
            }
        }

        private void DebugLogBuildableDatabase()
        {
            if (buildableDatabase == null || buildableDatabase.Count == 0)
            {
                Debug.LogWarning("[BuildableDatabase] Empty or null.", this);
                return;
            }

            Debug.Log($"[BuildableDatabase] Count={buildableDatabase.Count}", this);

            foreach (BuildableData data in buildableDatabase)
            {
                if (data == null)
                {
                    Debug.Log("[BuildableDatabase] NULL entry.", this);
                    continue;
                }

                Debug.Log(
                    $"[BuildableDatabase] Data={data.name} | " +
                    $"ItemID={data.ItemID} | " +
                    $"Prefab={(data.BuildablePrefab != null ? data.BuildablePrefab.name : "NULL")}",
                    data);
            }
        }

        #endregion

        #region Selection Box

        public void UpdateSelectionBoxColors()
        {
            for (int y = 0; y < BlockRows.Count; y++)
            {
                for (int x = 0; x < BlockRows[y].Blocks.Count; x++)
                {
                    if (BlockRows[y].Blocks[x] != null)
                        BlockRows[y].Blocks[x].UpdateSelectionBoxColor();
                }
            }
        }

        #endregion

        #region Block Lookup

        public GrowBlock GetBlockFromWorldPosition(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - MinPoint.position;

            int x = Mathf.FloorToInt(localPos.x / CellSize);
            int y = Mathf.FloorToInt(localPos.z / CellSize);

            return GetBlock(x, y);
        }

        public GrowBlock GetBlock()
        {
            if (Camera.main == null || Mouse.current == null)
                return null;

            Plane groundPlane = new(Vector3.up, MinPoint.position);
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPos = ray.GetPoint(distance);
                return GetBlockFromWorldPosition(worldPos);
            }

            return null;
        }

        public GrowBlock GetBlock(int x, int y)
        {
            if (y < 0 || y >= BlockRows.Count)
                return null;

            BlockRow row = BlockRows[y];

            if (row == null || row.Blocks == null)
                return null;

            if (x < 0 || x >= row.Blocks.Count)
                return null;

            return row.Blocks[x];
        }

        public GrowBlock GetBlockController()
        {
            if (PlayerManager.Instance == null || PlayerManager.Instance.Player == null)
                return null;

            return GetBlockFromWorldPosition(
                PlayerManager.Instance.Player.GrowBlockCheck.position);
        }

        #endregion

        #region Neighbors

        public GrowBlock GetNeighbor(GrowBlock block, Vector2Int direction)
        {
            if (block == null)
                return null;

            Vector2Int gridPosition = block.GetGridPosition();

            return GetBlock(
                gridPosition.x + direction.x,
                gridPosition.y + direction.y);
        }

        public GrowBlock GetNorthNeighbor(GrowBlock block) => GetNeighbor(block, Vector2Int.up);
        public GrowBlock GetSouthNeighbor(GrowBlock block) => GetNeighbor(block, Vector2Int.down);
        public GrowBlock GetEastNeighbor(GrowBlock block) => GetNeighbor(block, Vector2Int.right);
        public GrowBlock GetWestNeighbor(GrowBlock block) => GetNeighbor(block, Vector2Int.left);

        #endregion

        #region Activation

        public void ActivateBlocksInRadius(Vector3 worldPosition, float radius)
        {
            float radiusSqr = radius * radius;

            for (int y = 0; y < BlockRows.Count; y++)
            {
                for (int x = 0; x < BlockRows[y].Blocks.Count; x++)
                {
                    GrowBlock block = BlockRows[y].Blocks[x];

                    if (block == null || block.PreventUse)
                        continue;

                    float distanceSqr = (block.transform.position - worldPosition).sqrMagnitude;

                    if (distanceSqr <= radiusSqr)
                    {
                        block.IsActive = true;
                        block.SetSoilSprite(false);
                        block.UpdateSelectionBoxColor();

                        block.UpdateGridInfo();
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private Vector3 SnapToGrid(Vector3 pos)
        {
            return new Vector3(
                Mathf.Round(pos.x / CellSize) * CellSize,
                transform.position.y,
                Mathf.Round(pos.z / CellSize) * CellSize);
        }

        private Vector3 GetCellHalfExtents()
        {
            return Vector3.one * (CellSize * 0.5f * 0.9f);
        }

#if UNITY_EDITOR

        [ContextMenu("Fill Buildable Database")]
        private void FillBuildableDatabase()
        {
            buildableDatabase.Clear();

            string[] assetGuids =
                AssetDatabase.FindAssets(
                    "t:BuildableData",
                    new[] { "Assets/Data/Buildable" });

            foreach (string guid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                BuildableData buildable =
                    AssetDatabase.LoadAssetAtPath<BuildableData>(path);

                if (buildable == null)
                    continue;

                buildableDatabase.Add(buildable);
            }

            EditorUtility.SetDirty(this);
        }

#endif

        #endregion
    }

    [System.Serializable]
    public class BlockRow
    {
        public List<GrowBlock> Blocks = new();
    }
}