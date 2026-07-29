using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.UserInterface.Managers;

namespace ShiftedSignal.Garden.UserInterface.Components
{
    public class UI_ItemSlot :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] protected Image itemImage;
        [SerializeField] protected TextMeshProUGUI itemText;

        public InventoryItem item;

        protected virtual bool UsesTooltip => true;

        private const float HoverCooldown = 0.75f;

        private int lastClickFrame = -1;

        private float hoverTimer;
        private bool isHovering;
        private bool tooltipShown;

        private void Update()
        {
            if (!UsesTooltip ||
                !isHovering ||
                tooltipShown)
            {
                return;
            }

            hoverTimer += Time.unscaledDeltaTime;

            if (hoverTimer >= HoverCooldown)
                ShowTooltip();
        }

        public virtual void UpdateSlot(
            InventoryItem newItem)
        {
            item = newItem;

            if (itemImage == null ||
                itemText == null)
            {
                return;
            }

            if (item != null &&
                item.data != null)
            {
                itemImage.sprite = item.data.Icon;
                itemImage.color = Color.white;

                itemText.text =
                    item.stackSize > 1
                        ? item.stackSize.ToString()
                        : string.Empty;
            }
            else
            {
                CleanUpSlot();
            }
        }

        public virtual void CleanUpSlot()
        {
            item = null;

            ResetHoverState();
            HideTooltip();

            if (itemImage != null)
            {
                itemImage.sprite = null;
                itemImage.color = Color.clear;
            }

            if (itemText != null)
                itemText.text = string.Empty;
        }

        public virtual void OnPointerDown(
            PointerEventData eventData)
        {
            if (Time.frameCount == lastClickFrame)
                return;

            lastClickFrame = Time.frameCount;

            if (item == null ||
                item.data == null)
            {
                return;
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                Inventory.Instance.RemoveItem(
                    item.data);

                HideTooltip();
                return;
            }

            if (item.data.ItemType == ItemType.Seed)
            {
                HandleSeedClick(eventData);

                HideTooltip();
                return;
            }

            // Materials and other inventory items
            // currently have no click behavior.
            HideTooltip();
        }

        public virtual void OnPointerEnter(
            PointerEventData eventData)
        {
            if (!UsesTooltip ||
                item == null ||
                item.data == null)
            {
                return;
            }

            isHovering = true;
            tooltipShown = false;
            hoverTimer = 0f;
        }

        public virtual void OnPointerExit(
            PointerEventData eventData)
        {
            ResetHoverState();
            HideTooltip();
        }

        protected virtual void ShowTooltip()
        {
            if (!UsesTooltip ||
                item == null ||
                item.data == null)
            {
                return;
            }

            tooltipShown = true;

            if (UI.Instance == null ||
                UI.Instance.ItemToolTip == null)
            {
                return;
            }

            UI.Instance.ItemToolTip.ShowToolTip(
                item.data);
        }

        protected virtual void HideTooltip()
        {
            if (UI.Instance == null ||
                UI.Instance.ItemToolTip == null)
            {
                return;
            }

            UI.Instance.ItemToolTip.HideToolTip();
        }

        protected void ResetHoverState()
        {
            isHovering = false;
            tooltipShown = false;
            hoverTimer = 0f;
        }

        private void HandleSeedClick(
            PointerEventData eventData)
        {
            if (eventData == null)
                return;

            if (eventData.button ==
                PointerEventData.InputButton.Left)
            {
                Bus<SeedEquipEvent>.Raise(
                    new SeedEquipEvent(
                        item.data));
            }
            else if (
                eventData.button ==
                PointerEventData.InputButton.Right)
            {
                Bus<AssignSeedToQuickSelectEvent>.Raise(
                    new AssignSeedToQuickSelectEvent(
                        item.data));
            }
        }
    }
}