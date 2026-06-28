using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Misc;
using UnityEngine;
using UnityEngine.InputSystem;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.EventBus;




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
        [SerializeField] private List<BuildingSO> buildableDatabase = new();

        [Header("Debug")]
        [SerializeField] private bool logGridRestore = true;
        [SerializeField] private bool logBuildableRestore = false;
        [SerializeField] private bool allowGridHighlighting = true;

        [Header("Runtime")]
        [SerializeField] private int RowsPerFrame = 4;
        public List<BlockRow> BlockRows = new();

        private GrowBlock currentHoveredBlock;
        private Coroutine restoreRoutine;
        private Coroutine growthCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            RequestGridRestore();
        }

        private void OnEnable()
        {
            Bus<FarmGrowthTickEvent>.OnEvent += HandleFarmGrowthTick;

        }

        private void OnDisable()
        {
            Bus<FarmGrowthTickEvent>.OnEvent -= HandleFarmGrowthTick;
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
            if (logGridRestore)
            {
                Debug.Log(
                    $"[GridManager Restore] RequestGridRestore | " +
                    $"HasGrid={GridInfo.Instance?.HasGrid} | " +
                    $"SavedRows={GridInfo.Instance?.Grid?.Count ?? -1} | " +
                    $"SavedBuildables={CountSavedBuildables()} | " +
                    $"BlockRows={BlockRows?.Count ?? -1} | " +
                    $"DatabaseCount={buildableDatabase?.Count ?? -1}",
                    this);
            }

            if (restoreRoutine != null)
                StopCoroutine(restoreRoutine);

            restoreRoutine = StartCoroutine(RestoreGridWhenReady());
        }

        private IEnumerator RestoreGridWhenReady()
        {
            yield return null;

            int attempts = 0;
            const int maxAttempts = 30;
            string lastWaitReason = "";

            while (attempts < maxAttempts)
            {
                if (CanRestoreGrid(out string waitReason))
                {
                    if (logGridRestore)
                    {
                        Debug.Log(
                            $"[GridManager Restore] Ready after {attempts + 1} attempts | " +
                            $"SavedBuildables={CountSavedBuildables()}",
                            this);
                    }

                    UpdateGrid();
                    restoreRoutine = null;
                    yield break;
                }

                if (waitReason != lastWaitReason && logGridRestore)
                {
                    Debug.Log(
                        $"[GridManager Restore] Waiting: {waitReason}",
                        this);

                    lastWaitReason = waitReason;
                }

                attempts++;
                yield return null;
            }

            Debug.LogWarning(
                $"[GridManager Restore] FAILED after {maxAttempts} attempts | " +
                $"LastReason={lastWaitReason} | " +
                $"SavedBuildables={CountSavedBuildables()}",
                this);

            restoreRoutine = null;
        }

        private bool CanRestoreGrid(out string reason)
        {
            if (GridInfo.Instance == null)
            {
                reason = "GridInfo.Instance is null";
                return false;
            }

            if (!GridInfo.Instance.HasGrid)
            {
                reason = "GridInfo.HasGrid is false";
                return false;
            }

            if (GridInfo.Instance.Grid == null || GridInfo.Instance.Grid.Count == 0)
            {
                reason = "GridInfo.Grid is null or empty";
                return false;
            }

            if (BlockRows == null || BlockRows.Count == 0)
            {
                reason = "BlockRows are null or empty";
                return false;
            }

            reason = "";
            return true;
        }

        #endregion

        #region Hover

        public void SetCommanderGridMode(bool commanderMode)
        {
            SetGridHighlighting(!commanderMode);

            if (commanderMode)
                HideGrid();
            else
                ShowGrid();
        }

        public void SetGridHighlighting(bool enabled)
        {
            allowGridHighlighting = enabled;

            if (!enabled)
            {
                foreach (BlockRow row in BlockRows)
                {
                    if (row == null)
                        continue;

                    foreach (GrowBlock block in row.Blocks)
                    {
                        if (block == null)
                            continue;

                        block.Glow(false);
                    }
                }

                currentHoveredBlock = null;
            }
        }

        private void UpdateHoveredBlock()
        {
            if (Player.Instance == null 
                || !allowGridHighlighting)
                return;

            GrowBlock newHoveredBlock = Player.Instance.UsingController
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

        #region Grid Update / Restore / Visuals

        private void HandleFarmGrowthTick(FarmGrowthTickEvent evt)
        {
            if (growthCoroutine != null)
                StopCoroutine(growthCoroutine);

            growthCoroutine = StartCoroutine(ProcessFarmGrowthBatch());
        }

        private IEnumerator ProcessFarmGrowthBatch()
        {
            int processed = 0;

            foreach (BlockRow row in BlockRows)
            {
                if (row == null)
                    continue;

                foreach (GrowBlock block in row.Blocks)
                {
                    block?.TickGrowth();
                }

                processed++;

                if (processed >= RowsPerFrame)
                {
                    processed = 0;
                    yield return null;
                }
            }

            growthCoroutine = null;
        }

        [ContextMenu("Show Grid")]
        public void ShowGrid()
        {
            foreach (BlockRow row in BlockRows)
            {
                if (row == null)
                    continue;

                foreach (GrowBlock block in row.Blocks)
                {
                    if (block == null || block.SR == null)
                        continue;

                    if (block.CurrentStage == GrowBlock.GrowthStage.Barren)
                    {
                        block.SR.enabled = true;
                    }
                }
            }
        }

        [ContextMenu("Hide Grid")]
        public void HideGrid()
        {
            foreach (BlockRow row in BlockRows)
            {
                if (row == null)
                    continue;

                foreach (GrowBlock block in row.Blocks)
                {
                    if (block == null || block.SR == null)
                        continue;

                    if (block.CurrentStage == GrowBlock.GrowthStage.Barren)
                    {
                        block.SR.enabled = false;
                    }
                }
            }
        }

        [ContextMenu("Update Grid")]
        public void UpdateGrid()
        {
            float startTime = Time.realtimeSinceStartup;

            if (logGridRestore)
            {
                Debug.Log(
                    $"[GridManager Restore] UpdateGrid START | " +
                    $"HasGrid={GridInfo.Instance?.HasGrid} | " +
                    $"SavedRows={GridInfo.Instance?.Grid?.Count ?? -1} | " +
                    $"BlockRows={BlockRows?.Count ?? -1} | " +
                    $"SavedBuildables={CountSavedBuildables()}",
                    this);
            }

            if (!CanRestoreGrid(out string reason))
            {
                Debug.LogWarning(
                    $"[GridManager Restore] UpdateGrid skipped | Reason={reason}",
                    this);

                return;
            }

            int rowCount = Mathf.Min(BlockRows.Count, GridInfo.Instance.Grid.Count);

            int restoredBuildables = 0;
            int restoreFailures = 0;

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

                    if (RestoreBuildableFromInfo(block, storedBlock))
                        restoredBuildables++;
                    else if (!string.IsNullOrEmpty(storedBlock.BuildableItemID))
                        restoreFailures++;

                    block.SetSoilSprite(false);
                    block.UpdateCropSprite(false);
                    block.Glow(false);
                }
            }

            int refreshedFences = RefreshAllFencePostConnections();

            Debug.Log(
                $"[GridManager Restore] UpdateGrid COMPLETE | " +
                $"Time={(Time.realtimeSinceStartup - startTime):F3}s | " +
                $"SavedBuildables={CountSavedBuildables()} | " +
                $"RestoredBuildables={restoredBuildables} | " +
                $"RestoreFailures={restoreFailures} | " +
                $"RefreshedFences={refreshedFences}",
                this);
        }

        private void RestoreBlockState(GrowBlock block, BlockInfo storedBlock)
        {
            block.IsWatered = storedBlock.IsWatered;
            block.CurrentStage = storedBlock.CurrentStage;
            block.IsActive = storedBlock.IsActive;
            block.Health = storedBlock.Health;
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

        private bool RestoreBuildableFromInfo(GrowBlock block, BlockInfo info)
        {
            if (block == null || info == null)
                return false;

            block.ClearBuildableWithoutSaving(false);

            if (string.IsNullOrEmpty(info.BuildableItemID))
                return false;

            if (logBuildableRestore)
            {
                Debug.Log(
                    $"[Buildable Restore] FOUND saved buildable | " +
                    $"Grid={block.GetGridPosition()} | " +
                    $"ItemID={info.BuildableItemID} | " +
                    $"HP={info.BuildableHP} | " +
                    $"Rot={info.BuildableYRotation}",
                    this);                
            }

            BuildingSO buildableData = FindBuildableDataByItemID(info.BuildableItemID);

            if (buildableData == null)
            {
                Debug.LogWarning(
                    $"[Buildable Restore] FAILED lookup | " +
                    $"Grid={block.GetGridPosition()} | " +
                    $"ItemID={info.BuildableItemID} | " +
                    $"DatabaseCount={buildableDatabase?.Count ?? -1}",
                    this);

                DebugLogBuildableDatabase();
                return false;
            }

            if (buildableData.Prefab == null)
            {
                Debug.LogWarning(
                    $"[Buildable Restore] FAILED missing prefab | " +
                    $"ItemID={info.BuildableItemID} | " +
                    $"Data={buildableData.name}",
                    buildableData);

                return false;
            }

            float buildableStart = Time.realtimeSinceStartup;

            GameObject builtObject = Instantiate(
                buildableData.Prefab,
                block.transform.position,
                Quaternion.Euler(buildableData.XRotation, info.BuildableYRotation, 0f));

            Debug.Log(
                $"Instantiate {buildableData.name}: " +
                $"{(Time.realtimeSinceStartup - buildableStart) * 1000f:F2} ms");

            BaseBuilding buildable = builtObject.GetComponent<BaseBuilding>();

            if (buildable == null)
            {
                Debug.LogWarning(
                    $"[Buildable Restore] FAILED prefab missing BaseBuildable | " +
                    $"Object={builtObject.name}",
                    builtObject);

                Destroy(builtObject);
                return false;
            }

            buildable.SetOccupiedBlock(block);
            buildable.RestoreFromSave(info.BuildableHP);
            block.SetBuildable(buildable);

            if (logBuildableRestore)
            {
                Debug.Log(
                    $"[Buildable Restore] SUCCESS | " +
                    $"Grid={block.GetGridPosition()} | " +
                    $"Object={builtObject.name} | " +
                    $"Data={buildableData.name}",
                    builtObject);
            }

            return true;
        }

        private BuildingSO FindBuildableDataByItemID(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
                return null;

            foreach (BuildingSO buildable in buildableDatabase)
            {
                if (buildable == null)
                    continue;

                if (buildable.ItemID == itemID)
                    return buildable;
            }

            return null;
        }

        private int RefreshAllFencePostConnections()
        {
            int refreshedCount = 0;

            for (int y = 0; y < BlockRows.Count; y++)
            {
                if (BlockRows[y] == null)
                    continue;

                for (int x = 0; x < BlockRows[y].Blocks.Count; x++)
                {
                    GrowBlock block = BlockRows[y].Blocks[x];

                    if (block == null)
                        continue;

                    if (block.CurrentBuildable is FencePost2D fencePost)
                    {
                        fencePost.RefreshConnections(block);
                        refreshedCount++;
                    }
                }
            }

            if (refreshedCount > 0)
            {
                Debug.Log(
                    $"[Fence Restore] Refreshed fence connections | Count={refreshedCount}",
                    this);
            }

            return refreshedCount;
        }

        private int CountSavedBuildables()
        {
            if (GridInfo.Instance == null || GridInfo.Instance.Grid == null)
                return 0;

            int count = 0;

            for (int y = 0; y < GridInfo.Instance.Grid.Count; y++)
            {
                InfoRow row = GridInfo.Instance.Grid[y];

                if (row == null || row.Blocks == null)
                    continue;

                for (int x = 0; x < row.Blocks.Count; x++)
                {
                    BlockInfo info = row.Blocks[x];

                    if (info == null)
                        continue;

                    if (!string.IsNullOrEmpty(info.BuildableItemID))
                        count++;
                }
            }

            return count;
        }

        private void DebugLogBuildableDatabase()
        {
            if (buildableDatabase == null || buildableDatabase.Count == 0)
            {
                Debug.LogWarning("[BuildableDatabase] Empty or null.", this);
                return;
            }

            Debug.Log($"[BuildableDatabase] Count={buildableDatabase.Count}", this);

            foreach (BuildingSO data in buildableDatabase)
            {
                if (data == null)
                {
                    Debug.Log("[BuildableDatabase] NULL entry.", this);
                    continue;
                }

                Debug.Log(
                    $"[BuildableDatabase] Data={data.name} | " +
                    $"ItemID={data.ItemID} | " +
                    $"Prefab={(data.Prefab != null ? data.Prefab.name : "NULL")}",
                    data);
            }
        }

        #endregion

        #region Selection Box

        public void UpdateSelectionBoxColors()
        {
            for (int y = 0; y < BlockRows.Count; y++)
            {
                if (BlockRows[y] == null)
                    continue;

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
            if (Player.Instance == null)
                return null;

            return GetBlockFromWorldPosition(
                Player.Instance.GrowBlockCheck.position);
        }

        public IEnumerable<GrowBlock> GetBlocksInRadius(Vector3 worldPosition, float radius)
        {
            if (BlockRows == null || BlockRows.Count == 0)
                yield break;

            Vector3 localPos = worldPosition - MinPoint.position;

            int centerX = Mathf.FloorToInt(localPos.x / CellSize);
            int centerY = Mathf.FloorToInt(localPos.z / CellSize);

            int cellRadius = Mathf.CeilToInt(radius / CellSize);
            float radiusSqr = radius * radius;

            for (int y = centerY - cellRadius; y <= centerY + cellRadius; y++)
            {
                if (y < 0 || y >= BlockRows.Count)
                    continue;

                BlockRow row = BlockRows[y];

                if (row == null || row.Blocks == null)
                    continue;

                for (int x = centerX - cellRadius; x <= centerX + cellRadius; x++)
                {
                    if (x < 0 || x >= row.Blocks.Count)
                        continue;

                    GrowBlock block = row.Blocks[x];

                    if (block == null)
                        continue;

                    float distanceSqr =
                        (block.transform.position - worldPosition).sqrMagnitude;

                    if (distanceSqr <= radiusSqr)
                        yield return block;
                }
            }
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
                if (BlockRows[y] == null)
                    continue;

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

    const string rootFolder = "Assets/Prefabs/Units/Buildings";

    if (!AssetDatabase.IsValidFolder(rootFolder))
    {
        Debug.LogError($"Buildable database folder not found: {rootFolder}", this);
        return;
    }

    string[] assetGuids =
        AssetDatabase.FindAssets(
            "t:BuildingSO",
            new[] { rootFolder });

    Debug.Log($"Found {assetGuids.Length} BuildingSO asset guids in {rootFolder}", this);

    foreach (string guid in assetGuids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);

        Debug.Log($"Checking asset at path: {path}", this);

        BuildingSO buildable =
            AssetDatabase.LoadAssetAtPath<BuildingSO>(path);

        if (buildable == null)
        {
            Debug.LogWarning($"Failed to load BuildingSO at path: {path}", this);
            continue;
        }

        Debug.Log($"Added buildable: {buildable.name}", buildable);

        buildableDatabase.Add(buildable);
    }

    Debug.Log($"Filled Buildable Database. Count={buildableDatabase.Count}", this);

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