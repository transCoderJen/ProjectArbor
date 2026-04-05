using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class GridManager : Singleton<GridManager>
{
    [field:SerializeField] public float CellSize { get; private set; } = 4f;
    [SerializeField] private Transform MinPoint, MaxPoint;
    [SerializeField] private GrowBlock BaseGridBlock;
    [SerializeField] private Transform GridParent;
    [SerializeField] private LayerMask GridBlockers;
    public List<BlockRow> BlockRows = new List<BlockRow>();

    [SerializeField] private Vector2Int gridSize;

    private void Start()
    {
        UpdateGrid();
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
                newBlock.SR.sprite = null;

                newBlock.SetGridPosition(x,z);
 
                BlockRows[z].Blocks.Add(newBlock);

                if (Physics.CheckBox(spawnPos, GetCellHalfExtents(), Quaternion.identity, GridBlockers))
                {

                    newBlock.PreventUse = true;   
                }
            }
        }
    }

    public void UpdateGrid()
    {
        if (GridInfo.Instance.HasGrid == true)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    BlockInfo storedBlock = GridInfo.Instance.Grid[y].Blocks[x];

                    BlockRows[y].Blocks[x].IsWatered = storedBlock.IsWatered;
                    BlockRows[y].Blocks[x].CurrentStage = storedBlock.CurrentStage;

                    BlockRows[y].Blocks[x].SetSoilSprite();
                    BlockRows[y].Blocks[x].UpdateCropSprite();    
                }
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

    public GrowBlock GetBlock(float x, float y)
    {
        Ray cameraRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue))
        {
            GrowBlock block = hit.collider.GetComponent<GrowBlock>();
            if (block != null)
            {
                return block;
            }
            return null;
        }

        return null;
    }
}

[System.Serializable]
public class BlockRow
{
    public List<GrowBlock> Blocks = new List<GrowBlock>();
}