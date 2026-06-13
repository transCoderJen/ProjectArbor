using ShiftedSignal.Garden.ItemsAndInventory;
using TMPro;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface
{
    public class UI_ItemTooltip : UI_Tooltip
    {
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemTypeText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;

        public void ShowToolTip(ItemData item)
        {
            if (item == null)
            {
                HideToolTip();
                return;
            }

            if (itemNameText != null)
                itemNameText.text = item.ItemName;

            if (itemTypeText != null)
                itemTypeText.text = item.ItemType.ToString();

            if (itemDescriptionText != null)
                itemDescriptionText.text = item.GetDescription();

            AdjustPosition();
            gameObject.SetActive(true);
        }

        public void HideToolTip()
        {
            gameObject.SetActive(false);
        }
    }
}