using System.Collections.Generic;
using ShiftedSignal.Garden.Shops;
using ShiftedSignal.Garden.UserInterface.Components;
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

                slot.Setup(
                    entry,
                    HandlePurchaseRequested,
                    tooltip);

                shopSlots.Add(slot);
            }
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

        private bool TryPurchase(
            ShopEntry entry)
        {
            // Replace with your currency and inventory logic.
            return false;
        }

        private void TriggerDialogue(
            string knotName)
        {
            if (string.IsNullOrWhiteSpace(knotName))
                return;

            // Connect to your dialogue system.
        }
    }
}