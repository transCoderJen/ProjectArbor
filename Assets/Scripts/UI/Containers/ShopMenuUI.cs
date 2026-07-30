using System.Collections.Generic;
using Ink.Parsed;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Shops;
using ShiftedSignal.Garden.UserInterface.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface.Containers
{
    public class ShopMenuUI : MonoBehaviour
    {
        private const string InsufficientFundsKnot =
            "shop_insufficient_funds";

        [Header("Menu")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private Button closeButton;

        [Header("Scroll View")]
        [SerializeField] private RectTransform scrollViewRect;
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private VerticalLayoutGroup slotLayoutGroup;
        [SerializeField] private float slotHeight = 80f;
        [SerializeField] private int maximumVisibleSlots = 5;

        [Header("Slots")]
        [SerializeField] private Transform slotContainer;
        [SerializeField] private UI_ShopSlot shopSlotPrefab;

        [Header("Tooltip")]
        [SerializeField] private Tooltip tooltip;

        private readonly List<UI_ShopSlot> shopSlots = new();

        private ShopSO currentShop;

        private void Awake()
        {
            if (menuRoot != null)
                menuRoot.SetActive(false);

            if (tooltip != null)
                tooltip.Hide();

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void Open(ShopSO shop)
        {
            if (shop == null)
                return;

            if (menuRoot == null ||
                slotContainer == null ||
                shopSlotPrefab == null)
            {
                Debug.LogError(
                    $"{name} is missing one or more required shop UI references.",
                    this);

                return;
            }

            currentShop = shop;

            RefreshSlots();

            menuRoot.SetActive(true);
        }

        public void Close()
        {
            if (tooltip != null)
                tooltip.Hide();

            if (menuRoot != null)
                menuRoot.SetActive(false);

            ClearSlots();

            currentShop = null;
        }

        private void ResizeScrollView(int slotCount)
        {
            int visibleSlotCount = Mathf.Clamp(
                slotCount,
                1,
                maximumVisibleSlots);

            float spacing = slotLayoutGroup != null
                ? slotLayoutGroup.spacing
                : 0f;

            float verticalPadding = slotLayoutGroup != null
                ? slotLayoutGroup.padding.top +
                slotLayoutGroup.padding.bottom
                : 0f;

            float height =
                slotHeight * visibleSlotCount +
                spacing * Mathf.Max(0, visibleSlotCount - 1) +
                verticalPadding;

            scrollViewRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                height);
        }

        private void RefreshSlots()
        {
            ClearSlots();

            if (currentShop == null ||
                currentShop.Items == null)
            {
                return;
            }

            foreach (ShopEntry entry in currentShop.Items)
            {
                if (entry == null ||
                    !entry.IsValid)
                {
                    continue;
                }

                UI_ShopSlot slot = Instantiate(
                    shopSlotPrefab,
                    slotContainer);

                int ownedAmount =
                    Inventory.Instance.GetItemAmount(entry.Item);

                slot.Setup(
                    entry,
                    ownedAmount,
                    HandlePurchaseRequested,
                    tooltip);

                shopSlots.Add(slot);
            }

            ResizeScrollView(shopSlots.Count);
            scrollViewRect.GetComponent<ScrollRect>().vertical = shopSlots.Count >= 5;
        }

        private void ClearSlots()
        {
            if (tooltip != null)
                tooltip.Hide();

            foreach (UI_ShopSlot slot in shopSlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }

            shopSlots.Clear();
        }

        private void HandlePurchaseRequested(
            ShopEntry entry)
        {
            if (entry == null ||
                entry.Item == null)
            {
                return;
            }

            bool purchaseSucceeded =
                TryPurchase(entry);
            
            RefreshSlots();

            if (purchaseSucceeded)
            {
                TriggerDialogue(
                    entry.PurchaseDialogueKnot);
            }
            else
            {
                TriggerDialogue(
                    InsufficientFundsKnot);
            }
        }

        private bool TryPurchase(ShopEntry entry)
        {
            if (entry.Price <= PlayerManager.Instance.Currency)
            {
                Inventory.Instance.AddItem(entry.Item);
                Bus<CurrencyUpdatedEvent>.Raise(new CurrencyUpdatedEvent(-entry.Price));
                return true;
            }

            return false;
        }

        private void TriggerDialogue(string knotName)
        {
            if (string.IsNullOrWhiteSpace(knotName))
                return;

            Bus<EnterDialogueEvent>.Raise(
                new EnterDialogueEvent(knotName));
        }
    }
}