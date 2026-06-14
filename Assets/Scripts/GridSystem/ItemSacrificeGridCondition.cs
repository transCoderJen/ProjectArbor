using ShiftedSignal.Garden.ItemsAndInventory;
using UnityEngine;

namespace ShiftedSignal.Garden.GridSystem
{
    [CreateAssetMenu(
        fileName = "Item Sacrifice Grid Activation Condition",
        menuName = "Data/Grid Activation Conditions/Item Sacrifice")]
    public class ItemSacrificeGridActivationCondition : GridActivationCondition
    {
        [SerializeField] private ItemData requiredItem;
        [SerializeField] private int amountRequired = 1;

        public override bool CanActivate()
        {
            return Inventory.Instance.HasItem(requiredItem, amountRequired);
        }

        public override void ConsumeCost()
        {
            Inventory.Instance.RemoveItem(requiredItem, amountRequired);
        }
    }
}