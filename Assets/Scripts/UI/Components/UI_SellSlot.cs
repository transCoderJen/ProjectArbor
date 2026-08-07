using System;
using ShiftedSignal.Garden.ItemsAndInventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface.Components
{
    [RequireComponent(typeof(Button))]
    public class UI_SellSlot :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("Display")]
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI ownedAmountText;

        [Header("Components")]
        [SerializeField] private Button button;

        private InventoryItem inventoryItem;
        private Action<InventoryItem> sellCallback;
        private Tooltip tooltip;

        public InventoryItem InventoryItem =>
            inventoryItem;

        public ItemData Item =>
            inventoryItem?.data;

        public int OwnedAmount =>
            inventoryItem?.stackSize ?? 0;

        public int SellPrice =>
            inventoryItem?.data != null
                ? inventoryItem.data.SellPrice
                : 0;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(ShowTooltip));

            if (tooltip != null)
                tooltip.Hide();
        }

        public void Setup(
            InventoryItem newInventoryItem,
            Action<InventoryItem> onSellRequested,
            Tooltip sharedTooltip)
        {
            inventoryItem = newInventoryItem;
            sellCallback = onSellRequested;
            tooltip = sharedTooltip;

            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
                button.onClick.RemoveAllListeners();

            if (inventoryItem == null ||
                inventoryItem.data == null ||
                inventoryItem.stackSize <= 0)
            {
                Disable();
                return;
            }

            SetIcon(
                inventoryItem.data.Icon);

            RefreshOwnedAmount();

            if (button != null)
            {
                button.interactable = true;

                button.onClick.AddListener(
                    HandleClicked);
            }
        }

        public void RefreshOwnedAmount()
        {
            if (ownedAmountText != null)
            {
                ownedAmountText.text = OwnedAmount.ToString();
            }

            if (button != null)
            {
                button.interactable =
                    inventoryItem != null &&
                    inventoryItem.data != null &&
                    inventoryItem.stackSize > 0;
            }
        }

        public void Disable()
        {
            inventoryItem = null;
            sellCallback = null;

            CancelInvoke(nameof(ShowTooltip));

            if (button != null)
            {
                button.interactable = false;
                button.onClick.RemoveAllListeners();
            }

            SetIcon(null);

            if (ownedAmountText != null)
                ownedAmountText.text = string.Empty;

            if (tooltip != null)
                tooltip.Hide();
        }

        public void OnPointerEnter(
            PointerEventData eventData)
        {
            if (tooltip == null ||
                inventoryItem == null ||
                inventoryItem.data == null ||
                inventoryItem.stackSize <= 0 ||
                button == null ||
                !button.interactable)
            {
                return;
            }

            CancelInvoke(nameof(ShowTooltip));

            Invoke(
                nameof(ShowTooltip),
                tooltip.HoverDelay);
        }

        public void OnPointerExit(
            PointerEventData eventData)
        {
            CancelInvoke(nameof(ShowTooltip));

            if (tooltip != null)
                tooltip.Hide();
        }

        private void HandleClicked()
        {
            if (inventoryItem == null ||
                inventoryItem.data == null ||
                inventoryItem.stackSize <= 0)
            {
                return;
            }

            CancelInvoke(nameof(ShowTooltip));

            if (tooltip != null)
                tooltip.Hide();

            sellCallback?.Invoke(
                inventoryItem);
        }

        private void ShowTooltip()
        {
            if (tooltip == null ||
                inventoryItem == null ||
                inventoryItem.data == null)
            {
                return;
            }

            tooltip.SetText(
                inventoryItem.data,
                SellPrice);

            tooltip.RectTransform.position =
                (Vector2)Input.mousePosition +
                new Vector2(16f, -16f);

            tooltip.Show();
        }

        private void SetIcon(
            Sprite newIcon)
        {
            if (icon == null)
                return;

            if (newIcon == null)
            {
                icon.sprite = null;
                icon.enabled = false;
                return;
            }

            icon.sprite = newIcon;
            icon.enabled = true;
        }
    }
}