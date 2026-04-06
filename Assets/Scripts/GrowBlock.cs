using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public Sprite SoilTilledSprite;
    public Sprite SoilWateredSprite;

    public SpriteRenderer CropSprite;
    public Sprite CropPlantedSprite, CropGrowing1Sprite, CropGrowing2Sprite, CropRipeSprite;

    public bool IsWatered;

    public bool PreventUse;

    [SerializeField] private Vector2Int gridPosition;

    void Update()
    {
        if(Keyboard.current.nKey.wasPressedThisFrame)
        {
            AdvanceCrop();
        }
    }
    public void AdvanceStage()
    {
        CurrentStage ++;

        if ((int)CurrentStage >= 6)
        {
            CurrentStage = GrowthStage.Barren;
        }
    }

    public void SetSoilSprite()
    {
        if (CurrentStage == GrowthStage.Barren)
            SR.sprite = null;
        else
        {
            if (IsWatered)
            {
                SR.sprite = SoilWateredSprite;
            }
            else
            {
                SR.sprite = SoilTilledSprite;    
            }        
        }

        UpdateGridInfo();
    }

    public void PloughSoil()
    {
        if (CurrentStage == GrowthStage.Barren)
        {
            CurrentStage = GrowthStage.Ploughed;
            SetSoilSprite();

            PlayerManager.Instance.Player.GrassCutter.CutGrass(transform.position, GridManager.Instance.CellSize, CutShape.Box);
        }
    }

    public void WaterSoil()
    {
        IsWatered = true;

        SetSoilSprite();
    }

    public void PlantCrop()
    {
        if (CurrentStage == GrowthStage.Ploughed && IsWatered)
        {
            CurrentStage = GrowthStage.Planted;
            UpdateCropSprite();

            
        }

    }

    public void UpdateCropSprite()
    {
        switch(CurrentStage)
        {
            case GrowthStage.Planted:
                CropSprite.sprite = CropPlantedSprite;
                break;
            case GrowthStage.Growing1:
                CropSprite.sprite = CropGrowing1Sprite;
                break;
            case GrowthStage.Growing2:
                CropSprite.sprite = CropGrowing2Sprite;
                break;
            case GrowthStage.Ripe:
                CropSprite.sprite = CropRipeSprite;
                break;
        }

        UpdateGridInfo();
    }

    private void AdvanceCrop()
    {
        if (IsWatered == true)
        {
            if (CurrentStage == GrowthStage.Planted
                || CurrentStage == GrowthStage.Growing1 
                || CurrentStage == GrowthStage.Growing2)
            {
                CurrentStage++;

                IsWatered = false;
                SetSoilSprite();
                UpdateCropSprite();
            }
        }
    }

    public void HarvestCrop()
    {
        if(CurrentStage == GrowthStage.Ripe)
        {
            CurrentStage = GrowthStage.Ploughed;
            SetSoilSprite();
            CropSprite.sprite = null;
        }
    }

    public void SetGridPosition(int x, int z)
    {
        gridPosition = new Vector2Int(x, z);
    }

    public void UpdateGridInfo()
    {
        GridInfo.Instance.UpdateInfo(this, gridPosition.x, gridPosition.y);
    }
}