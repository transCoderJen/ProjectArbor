using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        private readonly float hoverCooldown = 0.75f;
        private int lastClickFrame = -1;

        private float hoverTimer;
        private bool isHovering;
        private bool tooltipShown;

        private void Update()
        {
            if (!isHovering || tooltipShown)
                return;

            hoverTimer += Time.unscaledDeltaTime;

            if (hoverTimer >= hoverCooldown)
                ShowTooltip();
        }

        public void UpdateSlot(InventoryItem newItem)
        {
            item = newItem;

            if (itemImage == null || itemText == null)
                return;

            itemImage.color = Color.white;

            if (item != null && item.data != null)
            {
                itemImage.sprite = item.data.Icon;
                itemText.text = item.stackSize > 1 ? item.stackSize.ToString() : "";
            }
            else
            {
                CleanUpSlot();
            }
        }

        public void CleanUpSlot()
        {
            item = null;

            if (itemImage != null)
            {
                itemImage.sprite = null;
                itemImage.color = Color.clear;
            }

            if (itemText != null)
                itemText.text = "";
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (Time.frameCount == lastClickFrame)
                return;

            lastClickFrame = Time.frameCount;

            if (item == null || item.data == null)
                return;

            if (Input.GetKey(KeyCode.LeftControl))
            {
                Inventory.Instance.RemoveItem(item.data);
                HideTooltip();
                return;
            }

            if (item.data.ItemType == ItemType.Seed)
            {
                HandleSeedClick(eventData);
                HideTooltip();
                return;
            }

            // No equipment behavior anymore.
            // Materials and other inventory items only display tooltip / stack count.
            HideTooltip();
        }

        private void HandleSeedClick(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Bus<SeedEquipEvent>.Raise(
                    new SeedEquipEvent(item.data));
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                Bus<AssignSeedToQuickSelectEvent>.Raise(
                    new AssignSeedToQuickSelectEvent(item.data));
            }
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

            HideTooltip();
        }

        private void ShowTooltip()
        {
            if (item == null || item.data == null)
                return;

            tooltipShown = true;

            if (UI.Instance != null && UI.Instance.ItemToolTip != null)
                UI.Instance.ItemToolTip.ShowToolTip(item.data);
        }

        private void HideTooltip()
        {
            if (UI.Instance != null && UI.Instance.ItemToolTip != null)
                UI.Instance.ItemToolTip.HideToolTip();
        }
    }
}