using System.Collections.Generic;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.GridSystem
{
    
    public class GridInfo : Singleton<GridInfo>, ISaveManager
    {
        public bool HasGrid;

        public List<InfoRow> Grid = new List<InfoRow>();

        public void Start()
        {
            if(!HasGrid)
            {
                CreateGrid();
            }
        }
        
        public void CreateGrid()
        {
            HasGrid = true;
            for (int y = 0; y < GridManager.Instance.BlockRows.Count; y++)
            {
                Grid.Add(new InfoRow());
                for (int x = 0; x < GridManager.Instance.BlockRows[y].Blocks.Count; x++)
                {
                    // Get a reference to the physical block in the scene
                    GrowBlock physicalBlock = GridManager.Instance.BlockRows[y].Blocks[x];
                    
                    // Create the new info and copy the scene's starting values into it
                    BlockInfo newInfo = new()
                    {
                        CurrentStage = physicalBlock.CurrentStage,
                        IsWatered = physicalBlock.IsWatered,
                        
                        // This prevents your Activation Blocks from being wiped out
                        IsActive = physicalBlock.IsActive,
                        Health = physicalBlock.health
                    };
                    
                    if (physicalBlock.Seed != null)
                        newInfo.SeedItemID = physicalBlock.Seed.ItemID;
                    else
                        newInfo.SeedItemID = "";

                    // Add the populated info to our grid system
                    Grid[y].Blocks.Add(newInfo);
                }
            }
        }

        public void UpdateInfo(GrowBlock Block, int xPos, int yPos)
        {
            BlockInfo info = Grid[yPos].Blocks[xPos];
            info.CurrentStage = Block.CurrentStage;
            info.IsWatered = Block.IsWatered;
            info.IsActive = Block.IsActive;
            info.Health = Block.health;

            // Save the seed's ID if one is planted
            if (Block.Seed != null)
                info.SeedItemID = Block.Seed.ItemID;
            else
                info.SeedItemID = "";
        }

        [ContextMenu("Grow Crop")]
        public void GrowCrop()
        {
            for (int y = 0; y < Grid.Count; y++)
            {
                for (int x = 0; x < Grid[y].Blocks.Count; x++)
                {
                    //TODO Randomize chance based of seed stats
                    if (Grid[y].Blocks[x].IsWatered)
                    {
                        switch (Grid[y].Blocks[x].CurrentStage)
                        {
                            case GrowBlock.GrowthStage.Planted:
                                Grid[y].Blocks[x].CurrentStage = GrowBlock.GrowthStage.Growing1;
                                break;
                            case GrowBlock.GrowthStage.Growing1:
                                Grid[y].Blocks[x].CurrentStage = GrowBlock.GrowthStage.Growing2;
                                break;
                            case GrowBlock.GrowthStage.Growing2:
                                Grid[y].Blocks[x].CurrentStage = GrowBlock.GrowthStage.Ripe;
                                break;
                        }

                        Grid[y].Blocks[x].IsWatered = false;
                    }
                }
            }

            GridManager.Instance.UpdateGrid();
        }

        public void DestroyGrid()
        {
            Grid.Clear();
            HasGrid = false;
        }

        void Update()
        {
            if (Keyboard.current.yKey.wasPressedThisFrame)
            {
                GrowCrop();
            }
        }

        public void LoadData(GameData data)
        {
            // Check if there's existing grid data in the save file
            if (data.gridRows != null && data.gridRows.Count > 0)
            {
                this.Grid = data.gridRows;
                this.HasGrid = true;
                
                // Force the GridManager to immediately visually update the blocks 
                // using the newly loaded GridInfo data.
                if (GridManager.Instance != null)
                {
                    GridManager.Instance.UpdateGrid();
                }
            }
        }

        public void SaveData(ref GameData data)
        {
            data.gridRows = this.Grid;
        }
    }

    [System.Serializable]
    public class BlockInfo
    {
        public bool IsWatered;
        public GrowBlock.GrowthStage CurrentStage;
        
        // Add these new properties:
        public string SeedItemID; 
        public bool IsActive;
        public int Health;
    }

    [System.Serializable]
    public class InfoRow
    {
        public List<BlockInfo> Blocks = new List<BlockInfo>();
    }
}