using System.Collections.Generic;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Shops;
using ShiftedSignal.Garden.UserInterface.Components;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface.Containers
{
    public class ShopMenuUI : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private Button closeButton;

        [Header("Buy/Sell UI")]
        [SerializeField] private GameObject buyUI;
        [SerializeField] private GameObject sellUI;

        [Header("Buy Scroll View")]
        [SerializeField] private RectTransform buyScrollViewRect;
        [SerializeField] private VerticalLayoutGroup buyLayoutGroup;
        [SerializeField] private Transform buySlotContainer;
        [SerializeField] private UI_ShopSlot shopSlotPrefab;
        [SerializeField] private float buySlotHeight = 80f;
        [SerializeField] private int maximumVisibleBuySlots = 5;

        [Header("Sell Scroll View")]
        [SerializeField] private RectTransform sellScrollViewRect;
        [SerializeField] private GridLayoutGroup sellGridLayoutGroup;
        [SerializeField] private Transform sellSlotContainer;
        [SerializeField] private UI_SellSlot sellSlotPrefab;
        [SerializeField] private int maximumVisibleSellRows = 5;

        [Header("Tooltip")]
        [SerializeField] private Tooltip tooltip;

        private readonly List<UI_ShopSlot> buySlots =
            new();

        private readonly List<UI_SellSlot> sellSlots =
            new();

        private ShopSO currentShop;
        private ShopMode currentMode;

        private void Awake()
        {
            if (menuRoot != null)
                menuRoot.SetActive(false);

            if (buyUI != null)
                buyUI.SetActive(false);

            if (sellUI != null)
                sellUI.SetActive(false);

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

        public void Open(
            ShopSO shop,
            ShopMode mode)
        {
            if (shop == null)
            {
                Debug.LogWarning(
                    "[ShopMenuUI] Cannot open a null shop.",
                    this);

                return;
            }

            if (menuRoot == null)
            {
                Debug.LogError(
                    "[ShopMenuUI] Menu root is not assigned.",
                    this);

                return;
            }

            currentShop = shop;
            currentMode = mode;

            menuRoot.SetActive(true);

            SetShopMode(mode);
        }

        private void SetShopMode(
            ShopMode mode)
        {
            bool isBuying =
                mode == ShopMode.Buy;

            if (buyUI != null)
                buyUI.SetActive(isBuying);

            if (sellUI != null)
                sellUI.SetActive(!isBuying);

            if (tooltip != null)
                tooltip.Hide();

            if (isBuying)
            {
                ClearSellSlots();
                RefreshBuySlots();
            }
            else
            {
                ClearBuySlots();
                RefreshSellSlots();
            }
        }

        public void Close()
        {
            if (tooltip != null)
                tooltip.Hide();

            ClearBuySlots();
            ClearSellSlots();

            if (buyUI != null)
                buyUI.SetActive(false);

            if (sellUI != null)
                sellUI.SetActive(false);

            if (menuRoot != null)
                menuRoot.SetActive(false);

            string exitShopKnot =
                currentShop != null
                    ? currentShop.ExitShopKnot
                    : string.Empty;

            currentShop = null;
            currentMode = ShopMode.Buy;

            if (string.IsNullOrWhiteSpace(
                    exitShopKnot))
            {
                Debug.LogWarning(
                    "[ShopMenuUI] The shop has no exit dialogue knot.",
                    this);

                return;
            }

            TriggerDialogue(
                exitShopKnot);
        }

        #region Buy

        private void RefreshBuySlots()
        {
            ClearBuySlots();

            if (currentShop == null ||
                currentShop.Items == null ||
                buySlotContainer == null ||
                shopSlotPrefab == null)
            {
                return;
            }

            foreach (ItemData item
                     in currentShop.Items)
            {
                if (item == null)
                    continue;

                UI_ShopSlot slot =
                    Instantiate(
                        shopSlotPrefab,
                        buySlotContainer);

                int ownedAmount =
                    Inventory.Instance != null
                        ? Inventory.Instance
                            .GetItemAmount(item)
                        : 0;

                slot.Setup(
                    item,
                    ownedAmount,
                    HandlePurchaseRequested,
                    tooltip);

                buySlots.Add(slot);
            }

            UpdateBuyScrollView(
                buySlots.Count);
        }

        private void ClearBuySlots()
        {
            foreach (UI_ShopSlot slot
                     in buySlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }

            buySlots.Clear();
        }

        private void HandlePurchaseRequested(
            ItemData item)
        {
            if (item == null)
                return;

            bool purchaseSucceeded =
                TryPurchase(item);

            RefreshBuySlots();

            if (purchaseSucceeded)
            {
                TriggerItemDialogue(
                    item.BuyDialogueKnot);
            }
            else if (currentShop != null)
            {
                TriggerDialogue(
                    currentShop
                        .InsufficientFundsKnot);
            }
        }

        private bool TryPurchase(
            ItemData item)
        {
            if (item == null ||
                PlayerManager.Instance == null ||
                Inventory.Instance == null)
            {
                return false;
            }

            int buyPrice =
                item.BaseValue;

            if (buyPrice >
                PlayerManager.Instance.Currency)
            {
                return false;
            }

            Inventory.Instance.AddItem(
                item);

            if (item is ItemData_Seed seed)
            {
                Bus<AssignSeedToQuickSelectEvent>.Raise(
                    new AssignSeedToQuickSelectEvent(seed));
            }

            Bus<CurrencyUpdatedEvent>.Raise(
                new CurrencyUpdatedEvent(
                    -buyPrice));

            return true;
        }

        private void UpdateBuyScrollView(
            int slotCount)
        {
            if (buyScrollViewRect == null)
                return;

            int visibleSlotCount =
                Mathf.Clamp(
                    slotCount,
                    1,
                    maximumVisibleBuySlots);

            float spacing =
                buyLayoutGroup != null
                    ? buyLayoutGroup.spacing
                    : 0f;

            float verticalPadding =
                buyLayoutGroup != null
                    ? buyLayoutGroup.padding.top +
                      buyLayoutGroup.padding.bottom
                    : 0f;

            float height =
                buySlotHeight *
                visibleSlotCount +
                spacing *
                Mathf.Max(
                    0,
                    visibleSlotCount - 1) +
                verticalPadding;

            buyScrollViewRect
                .SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    height);
        }

        #endregion

        #region Sell

        private void RefreshSellSlots()
        {
            ClearSellSlots();

            if (Inventory.Instance == null ||
                sellSlotContainer == null ||
                sellSlotPrefab == null)
            {
                return;
            }

            PopulateSellSlots(
                Inventory.Instance
                    .GetInventoryList());

            PopulateSellSlots(
                Inventory.Instance
                    .GetStashList());

            PopulateSellSlots(
                Inventory.Instance
                    .GetSeedBankList());

            UpdateSellScrollView(
                sellSlots.Count);
        }

        private void PopulateSellSlots(
            List<InventoryItem> inventoryItems)
        {
            if (inventoryItems == null)
                return;

            foreach (InventoryItem inventoryItem
                     in inventoryItems)
            {
                if (inventoryItem == null ||
                    inventoryItem.data == null ||
                    inventoryItem.stackSize <= 0)
                {
                    continue;
                }

                UI_SellSlot slot =
                    Instantiate(
                        sellSlotPrefab,
                        sellSlotContainer);

                slot.Setup(
                    inventoryItem,
                    HandleSellRequested,
                    tooltip);

                sellSlots.Add(slot);
            }
        }

        private void ClearSellSlots()
        {
            foreach (UI_SellSlot slot
                     in sellSlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }

            sellSlots.Clear();
        }

        private void HandleSellRequested(InventoryItem inventoryItem)
        {
            if (inventoryItem == null ||
                inventoryItem.data == null ||
                Inventory.Instance == null)
            {
                return;
            }

            ItemData item =
                inventoryItem.data;

            if (inventoryItem.stackSize <= 0)
                return;

            int sellPrice =
                item.SellPrice;

            /*
            * Remove one item from the player's inventory.
            */
            Inventory.Instance.RemoveItem(
                item);

            /*
            * Give the player the sale value.
            */
            Bus<CurrencyUpdatedEvent>.Raise(
                new CurrencyUpdatedEvent(
                    sellPrice));

            /*
            * Rebuild the sell slots from the updated
            * inventory. If that was the last item in
            * the stack, its slot disappears.
            */
            RefreshSellSlots();

            /*
            * Resolve the merchant-specific dialogue:
            *
            * Fiona + SellLettuce
            * =
            * FionaSellLettuce
            */
            TriggerItemDialogue(
                item.SellDialogueKnot);
        }

        private void UpdateSellScrollView(
            int slotCount)
        {
            if (sellScrollViewRect == null ||
                sellGridLayoutGroup == null)
            {
                return;
            }

            int columnCount =
                GetSellColumnCount();

            int totalRows =
                Mathf.CeilToInt(
                    slotCount /
                    (float)columnCount);

            int visibleRows =
                Mathf.Clamp(
                    totalRows,
                    1,
                    maximumVisibleSellRows);

            float cellHeight =
                sellGridLayoutGroup
                    .cellSize.y;

            float verticalSpacing =
                sellGridLayoutGroup
                    .spacing.y;

            float verticalPadding =
                sellGridLayoutGroup
                    .padding.top +
                sellGridLayoutGroup
                    .padding.bottom;

            float height =
                cellHeight * visibleRows +
                verticalSpacing *
                Mathf.Max(
                    0,
                    visibleRows - 1) +
                verticalPadding;

            /*
             * Only resize the Scroll View.
             * Anchors, pivot, scrolling, etc.
             * remain controlled by the Inspector.
             */
            sellScrollViewRect
                .SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    height);
        }

        private int GetSellColumnCount()
        {
            if (sellGridLayoutGroup == null)
                return 1;

            if (sellGridLayoutGroup.constraint ==
                GridLayoutGroup.Constraint
                    .FixedColumnCount)
            {
                return Mathf.Max(
                    1,
                    sellGridLayoutGroup
                        .constraintCount);
            }

            RectTransform gridRect =
                sellGridLayoutGroup
                    .GetComponent<RectTransform>();

            if (gridRect == null)
                return 1;

            float availableWidth =
                gridRect.rect.width -
                sellGridLayoutGroup.padding.left -
                sellGridLayoutGroup.padding.right;

            float cellWidth =
                sellGridLayoutGroup
                    .cellSize.x;

            float horizontalSpacing =
                sellGridLayoutGroup
                    .spacing.x;

            if (cellWidth <= 0f)
                return 1;

            return Mathf.Max(
                1,
                Mathf.FloorToInt(
                    (availableWidth +
                     horizontalSpacing) /
                    (cellWidth +
                     horizontalSpacing)));
        }

        #endregion

        #region Dialogue

        private void TriggerItemDialogue(string itemDialogueKnot)
        {
            if (currentShop == null ||
                string.IsNullOrWhiteSpace(
                    itemDialogueKnot))
            {
                return;
            }

            string fullKnotName =
                currentShop.DialogueKnotPrefix +
                itemDialogueKnot;

            TriggerDialogue(fullKnotName);
        }

        private void TriggerDialogue(string knotName)
        {
            if (string.IsNullOrWhiteSpace(
                    knotName))
            {
                return;
            }

            Bus<PlayOrReplaceDialogueEvent>.Raise(
                new PlayOrReplaceDialogueEvent(
                    knotName));
        }

        #endregion
    }
}