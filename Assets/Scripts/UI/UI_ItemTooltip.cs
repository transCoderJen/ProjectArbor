using ShiftedSignal.Garden.ItemsAndInventory;
using TMPro;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface
{
    public class UI_ItemTooltip : UI_Tooltip
    {
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemType;
        [SerializeField] private TextMeshProUGUI itemDescription;
        
        public void ShowToolTip(ItemData_Equipment item)
        {
            itemNameText.text = item.name;
            itemType.text = item.EquipmentType.ToString();
            itemDescription.text = item.GetDescription();

            AdjustPosition();
            gameObject.SetActive(true);
        }

        public void HideToolTip() => gameObject.SetActive(false);
    }
}
