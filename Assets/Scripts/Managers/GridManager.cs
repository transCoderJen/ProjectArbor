using System.Collections.Generic;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Misc;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.Managers
{ 
    public class GridManager : Singleton<GridManager>
    {
        [field:SerializeField] public float CellSize { get; private set; } = 4f;
        [SerializeField] private Transform MinPoint, MaxPoint;
        [SerializeField] private GrowBlock BaseGridBlock;
        [SerializeField] private Transform GridParent;
        [SerializeField] private LayerMask GridBlockers;
        [SerializeField] private LayerMask ActivationBlocks;
        [SerializeField] private LayerMask InteractionLayer;
        private GrowBlock currentHoveredBlock;
        public List<BlockRow> BlockRows = new List<BlockRow>();

        [SerializeField] private Vector2Int gridSize;

        private void Start()
        {
            UpdateGrid();
        }

        private void Update()
        {
            UpdateHoveredBlock();
        }

        private void UpdateHoveredBlock()
        {
            GrowBlock newHoveredBlock;

            if (PlayerManager.Instance.Player.UsingController)
                newHoveredBlock = GetBlockController();
            else
                newHoveredBlock = GetBlock();

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

        private Vector3 SnapToGrid(Vector3 pos)
        {
            return new Vector3(
                Mathf.Round(pos.x / CellSize) * CellSize,
                transform.position.y,
                Mathf.Round(pos.z / CellSize) * CellSize
            );
        }

        private Vector3 GetCellHalfExtents()
        {
            return Vector3.one * (CellSize * 0.5f * 0.9f);
        }

    #region Generate/Destroy Grid
        [ContextMenu("Generate Grid")]
        private void GenerateGrid()
        {
            DestroyGrid();
            CreateNewGridParent();
            
            MinPoint.position = SnapToGrid(MinPoint.position);
            MaxPoint.position = SnapToGrid(MaxPoint.position);

            float halfCellSize = CellSize / 2;
            Vector3 startPoint = MinPoint.position + new Vector3(halfCellSize, 0f, halfCellSize);

            gridSize = new Vector2Int(
                Mathf.RoundToInt((MaxPoint.position.x - MinPoint.position.x) / CellSize),
                Mathf.RoundToInt((MaxPoint.position.z - MinPoint.position.z) / CellSize));

            for (int z = 0; z < gridSize.y; z++)
            {
                BlockRows.Add(new BlockRow());

                for (int x = 0; x < gridSize.x; x++)
                {
                    Vector3 spawnPos = startPoint + new Vector3(x * CellSize, transform.position.y, z * CellSize);
                    GrowBlock newBlock = Instantiate(BaseGridBlock, spawnPos, Quaternion.Euler(90f,0f,0f), GridParent);
                    // newBlock.SR.sprite = null;
                    newBlock.Glow(false);
                    newBlock.transform.localScale = Vector3.one * (CellSize / 4f);

                    newBlock.SetGridPosition(x,z);
    
                    BlockRows[z].Blocks.Add(newBlock);

                    newBlock.IsActive = false;

                    if (Physics.CheckBox(spawnPos, GetCellHalfExtents(), Quaternion.identity, GridBlockers, QueryTriggerInteraction.Collide))
                    {
                        newBlock.PreventUse = true;
                        newBlock.gameObject.SetActive(false);
                    }

                    if (Physics.CheckBox(spawnPos, GetCellHalfExtents(), Quaternion.identity, ActivationBlocks, QueryTriggerInteraction.Collide))
                    {
                        newBlock.IsActive = true;
                        newBlock.IsActivationBlock = true;
                    }
                }
            }
        }

        [ContextMenu("Update Grid")]
        public void UpdateGrid()
        {
            if (GridInfo.Instance.HasGrid == true)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    for (int x = 0; x < gridSize.x; x++)
                    {
                        BlockInfo storedBlock = GridInfo.Instance.Grid[y].Blocks[x];
                        GrowBlock block = BlockRows[y].Blocks[x];
                        
                        block.IsWatered = storedBlock.IsWatered;
                        block.CurrentStage = storedBlock.CurrentStage;
                        block.IsActive = storedBlock.IsActive;
                        block.health = storedBlock.Health;

                        // Look up the seed from the inventory database using the saved ItemID
                        if (!string.IsNullOrEmpty(storedBlock.SeedItemID))
                        {
                            foreach(var item in Inventory.Instance.itemDataBase)
                            {
                                if (item.ItemID == storedBlock.SeedItemID)
                                {
                                    block.Seed = item as ItemsAndInventory.ItemData_Seed;
                                    break;
                                }
                            }
                        }
                        
                        block.SetSoilSprite(false);
                        block.UpdateCropSprite(false);
                        block.Glow(false); // Reset visual selection state
                    }
                }
            }
        }

        public void UpdateSelectionBoxColors()
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    BlockRows[y].Blocks[x].UpdateSelectionBoxColor();
                }
            }
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
            {
                GridInfo.Instance.CreateGrid();
            }
            else
            {
                Debug.LogWarning("GridInfo.Instance is null. Grid data was not created.");
            }
        }

        private void CreateNewGridParent()
        {
            GameObject newParent = new GameObject("Grid Parent");
            newParent.transform.SetParent(transform);
            newParent.transform.localPosition = Vector3.zero;
            newParent.transform.localRotation = Quaternion.identity;
            newParent.transform.localScale = Vector3.one;

            GridParent = newParent.transform;
        }
    #endregion

        public GrowBlock GetBlockFromWorldPosition(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - MinPoint.position;

            int x = Mathf.FloorToInt(localPos.x / CellSize);
            int y = Mathf.FloorToInt(localPos.z / CellSize);

            return GetBlock(x, y);
        }

        // Mouse GetBlock
        public GrowBlock GetBlock()
        {
            Plane groundPlane = new Plane(Vector3.up, MinPoint.position);

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPos = ray.GetPoint(distance);
                return GetBlockFromWorldPosition(worldPos);
            }

            return null;
        }

        // Controller GetBlock
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
            return GetBlockFromWorldPosition(
                PlayerManager.Instance.Player.GrowBlockCheck.position);
        }
    }

    [System.Serializable]
    public class BlockRow
    {
        public List<GrowBlock> Blocks = new List<GrowBlock>();
    }
}