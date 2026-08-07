using System;
using ShiftedSignal.Garden.ItemsAndInventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface.Components
{
    [RequireComponent(typeof(Button))]
    public class UI_ShopSlot :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("Display")]
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI ownedAmountText;

        [Header("Components")]
        [SerializeField] private Button button;

        private ItemData item;
        private Action<ItemData> purchaseCallback;
        private Tooltip tooltip;

        public ItemData Item => item;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(ShowTooltip));
        }

        public void Setup(
            ItemData newItem,
            int ownedAmount,
            Action<ItemData> onPurchase,
            Tooltip sharedTooltip)
        {
            item = newItem;
            purchaseCallback = onPurchase;
            tooltip = sharedTooltip;

            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
                button.onClick.RemoveAllListeners();

            if (item == null)
            {
                Disable();
                return;
            }

            SetIcon(item.Icon);

            if (itemNameText != null)
                itemNameText.text = item.ItemName;

            if (priceText != null)
                priceText.text = $"${item.BaseValue}";

            SetOwnedAmount(
                ownedAmount);

            if (button != null)
            {
                button.interactable = true;

                button.onClick.AddListener(
                    HandleClicked);
            }
        }

        public void SetOwnedAmount(
            int amount)
        {
            if (ownedAmountText != null)
            {
                ownedAmountText.text =
                    $"Owned: {Mathf.Max(0, amount)}";
            }
        }

        public void Disable()
        {
            item = null;
            purchaseCallback = null;

            CancelInvoke(nameof(ShowTooltip));

            if (button != null)
            {
                button.interactable = false;
                button.onClick.RemoveAllListeners();
            }

            SetIcon(null);

            if (itemNameText != null)
                itemNameText.text = string.Empty;

            if (priceText != null)
                priceText.text = string.Empty;

            if (ownedAmountText != null)
                ownedAmountText.text = string.Empty;
        }

        public void OnPointerEnter(
            PointerEventData eventData)
        {
            if (tooltip == null ||
                item == null ||
                button == null ||
                !button.interactable)
            {
                return;
            }

            CancelInvoke(
                nameof(ShowTooltip));

            Invoke(
                nameof(ShowTooltip),
                tooltip.HoverDelay);
        }

        public void OnPointerExit(
            PointerEventData eventData)
        {
            CancelInvoke(
                nameof(ShowTooltip));

            if (tooltip != null)
                tooltip.Hide();
        }

        private void HandleClicked()
        {
            if (item == null)
                return;

            CancelInvoke(
                nameof(ShowTooltip));

            if (tooltip != null)
                tooltip.Hide();

            purchaseCallback?.Invoke(
                item);
        }

        private void ShowTooltip()
        {
            if (tooltip == null ||
                item == null)
            {
                return;
            }

            tooltip.SetText(
                item);

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