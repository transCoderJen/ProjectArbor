using System;
using ShiftedSignal.Garden.Shops;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface.Components
{
    [RequireComponent(typeof(Button))]
    public class UI_ShopSlot :MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Display")]
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI ownedAmountText;

        [Header("Components")]
        [SerializeField] private Button button;

        private ShopEntry entry;
        private Action<ShopEntry> purchaseCallback;
        private Tooltip tooltip;

        public ShopEntry Entry => entry;

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
            ShopEntry newEntry,
            int ownedAmount,
            Action<ShopEntry> onPurchase,
            Tooltip sharedTooltip)
        {
            entry = newEntry;
            purchaseCallback = onPurchase;
            tooltip = sharedTooltip;

            if (button == null)
                button = GetComponent<Button>();

            button.onClick.RemoveAllListeners();

            if (entry == null ||
                entry.Item == null)
            {
                Disable();
                return;
            }

            SetIcon(entry.Item.Icon);

            if (itemNameText != null)
                itemNameText.text = entry.Item.ItemName;

            if (priceText != null)
                priceText.text = $"${entry.Price}";

            SetOwnedAmount(ownedAmount);

            button.interactable = true;
            button.onClick.AddListener(HandleClicked);
        }

        public void SetOwnedAmount(int amount)
        {
            if (ownedAmountText != null)
                ownedAmountText.text = $"Owned: {amount}";
        }

        public void Disable()
        {
            entry = null;
            purchaseCallback = null;

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

            CancelInvoke(nameof(ShowTooltip));
        }

        public void OnPointerEnter(
            PointerEventData eventData)
        {
            if (tooltip == null ||
                entry == null ||
                entry.Item == null ||
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
            if (entry == null ||
                entry.Item == null)
            {
                return;
            }

            CancelInvoke(nameof(ShowTooltip));

            if (tooltip != null)
                tooltip.Hide();

            purchaseCallback?.Invoke(entry);
        }

        private void ShowTooltip()
        {
            if (tooltip == null ||
                entry == null ||
                entry.Item == null)
            {
                return;
            }

            tooltip.SetText(entry.Item);

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