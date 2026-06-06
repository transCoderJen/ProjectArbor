using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftedSignal.Garden.ItemsAndInventory
{
    public enum CropType
    {
        Resource,
        Alive
    }

    [CreateAssetMenu(fileName = "New Item Data", menuName = "Data/Seed")]
    public class ItemData_Seed : ItemData
    {
        public CropType CropType;
        public Sprite CropPlantedSprite, 
                        CropGrowing1Sprite, 
                        CropGrowing2Sprite, 
                        CropRipeSprite;

        public GameObject RipePlant;

        public List<ItemData> Resources = new List<ItemData>();

        public PlacementRestrictionsSO[] Restrictions;

        public void AddResourcesToInventory()
        {
            foreach (ItemData data in Resources)
            {
                Inventory.Instance.AddItem(data);
            }
        }

        public bool AllRestrictionsPass(int GridX, int GridY) =>
            Restrictions.Length == 0 || Restrictions.All(restriction => restriction.CanPlace(GridX, GridY));
    }
}