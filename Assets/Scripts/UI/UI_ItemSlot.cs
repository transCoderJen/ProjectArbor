using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;

namespace ShiftedSignal.Garden.UserInterface
{   
    public class UI_ItemSlot : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected Image itemImage;
        [SerializeField] protected TextMeshProUGUI itemText;

        public InventoryItem item;
        
        private float hoverCooldown = 0.75f;
        private int lastClickFrame = -1;

        private float hoverTimer;
        private bool isHovering;
        private bool tooltipShown;

        private void Update()
        {
            if (!isHovering || tooltipShown)
                return;

            hoverTimer += Time.deltaTime;

            if (hoverTimer >= hoverCooldown)
            {
                ShowTooltip();
            }
        }

        public void UpdateSlot(InventoryItem _newItem)
        {
            item = _newItem;

            itemImage.color = Color.white;

            if (item != null)
            {
                itemImage.sprite = item.data.Icon;

                if (item.stackSize > 1)
                    itemText.text = item.stackSize.ToString();
                else
                    itemText.text = "";
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
            if (Time.frameCount == lastClickFrame)
                return;

            lastClickFrame = Time.frameCount;

            if (eventData != null)
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                    Debug.Log("Left mouse button clicked");
                else if (eventData.button == PointerEventData.InputButton.Right)
                    Debug.Log("Right mouse button clicked");
                else
                    Debug.Log("Other mouse button clicked");
            }

            if (item == null || item.data == null)
                return;

            if (Input.GetKey(KeyCode.LeftControl))
            {
                Inventory.Instance.RemoveItem(item.data);
                UI.Instance.ItemToolTip.HideToolTip();
                return;
            }

            if (item.data.ItemType == ItemType.Equipment)
            {
                Inventory.Instance.EquipItem(item.data);
                UI.Instance.ItemToolTip.HideToolTip();
                return;
                
            }

            if (item.data.ItemType == ItemType.Seed)
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                    Bus<SeedEquipEvent>.Raise(new SeedEquipEvent(item.data));
                else if (eventData.button == PointerEventData.InputButton.Right)
                    Bus<AssignSeedToQuickSelectEvent>.Raise(new AssignSeedToQuickSelectEvent(item.data));
            }

            UI.Instance.ItemToolTip.HideToolTip();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (item == null || item.data == null)
                return;

            isHovering = true;
            tooltipShown = false;
            hoverTimer = 0f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            tooltipShown = false;
            hoverTimer = 0f;

            UI.Instance.ItemToolTip.HideToolTip();
        }

        private void ShowTooltip()
        {
            if (item == null || item.data == null)
                return;

            if (item.data.ItemType == ItemType.Material || item.data.ItemType == ItemType.Seed)
                return;

            tooltipShown = true;

            UI.Instance.ItemToolTip.ShowToolTip(item.data as ItemData_Equipment);
        }
    }
}