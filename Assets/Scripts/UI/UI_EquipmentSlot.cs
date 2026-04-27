using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignalGames.GOF.ItemsAndInventory;
using UnityEngine.EventSystems;

namespace ShiftedSignal.Garden.UserInterface
{
    public class UI_EquipmentSlot : UI_ItemSlot
    {
        public EquipmentType slotType;

        private void OnValidate()
        {
            gameObject.name = "Equipment slot - " + slotType.ToString();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (item == null || item.data == null)
                return;

            ItemData_Equipment equipmentData = item.data as ItemData_Equipment;
            if (equipmentData == null)
            {
                UnityEngine.Debug.LogError("Failed to cast item.data to ItemData_Equipment");
                return;
            }

            Inventory.Instance.UnequipItem(equipmentData);

            Inventory.Instance.AddItem(equipmentData);

            // ui.itemTooltip.HideToolTip();
            
            CleanUpSlot();
        }
    }
}
