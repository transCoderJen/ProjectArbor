using System.Collections.Generic;
using UnityEngine.InputSystem;

    public class GridInfo : Singleton<GridInfo>
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
                    Grid[y].Blocks.Add(new BlockInfo());
                }
            }
        }

        public void UpdateInfo(GrowBlock Block, int xPos, int yPos)
        {
            Grid[yPos].Blocks[xPos].CurrentStage = Block.CurrentStage;
            Grid[yPos].Blocks[xPos].IsWatered = Block.IsWatered;
        }

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
    }

    [System.Serializable]
    public class BlockInfo
    {
        public bool IsWatered;
        public GrowBlock.GrowthStage CurrentStage;
    }

    [System.Serializable]
    public class InfoRow
    {
        public List<BlockInfo> Blocks = new List<BlockInfo>();
    }

