using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignalGames.GOF.ItemsAndInventory;

namespace ShiftedSignal.Garden.UserInterface
{   
    public class UI_ItemSlot : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected Image itemImage;
        [SerializeField] protected TextMeshProUGUI itemText;
        protected UI ui;
        public InventoryItem item;

        protected virtual void Start()
        {
            ui = GetComponentInParent<UI>();
        }
        
        public void UpdateSlot(InventoryItem _newItem)
        {
            item = _newItem;

            itemImage.color = Color.white;
            
            if (item != null)
            {
                itemImage.sprite = item.data.Icon;

                if (item.stackSize > 1)
                {
                    itemText.text = item.stackSize.ToString();
                }
                else
                {
                    itemText.text = "";
                }
            }
        }

        public void CleanUpSlot()
        {
            item = null;
            itemImage.sprite = null;
            itemImage.color = Color.clear;

            itemText.text = "";
        }
        
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (item == null || item.data == null)
                return;

            if (Input.GetKey(KeyCode.LeftControl))
            {
                Inventory.Instance.RemoveItem(item.data);
                return;
            }
            
            if (item.data.ItemType == ItemType.Equipment)
                Inventory.Instance.EquipItem(item.data);
            
            //TODO ui.itemTooltip.HideToolTip();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            //TODO OnPointerEnter ShowTooltip
            // if (item != null && item.data != null && item.data.itemType != ItemType.Material)
                
            //     ui.itemTooltip.ShowToolTip(item.data as ItemData_Equipment);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //TODO OnPointerExit Hidetooltip
            // ui.itemTooltip.HideToolTip();
        }
    }
}
