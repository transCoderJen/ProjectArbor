using System.Collections;
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

        private readonly List<UI_ShopSlot> buySlots = new();
        private readonly List<UI_SellSlot> sellSlots = new();

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

            foreach (ShopEntry entry in
                     currentShop.Items)
            {
                if (entry == null ||
                    !entry.IsValid)
                {
                    continue;
                }

                UI_ShopSlot slot =
                    Instantiate(
                        shopSlotPrefab,
                        buySlotContainer);

                int ownedAmount =
                    Inventory.Instance != null
                        ? Inventory.Instance.GetItemAmount(
                            entry.Item)
                        : 0;

                slot.Setup(
                    entry,
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
            ShopEntry entry)
        {
            if (entry == null ||
                entry.Item == null)
            {
                return;
            }

            bool purchaseSucceeded =
                TryPurchase(entry);

            RefreshBuySlots();

            if (purchaseSucceeded)
            {
                TriggerDialogue(
                    entry.PurchaseDialogueKnot);
            }
            else if (currentShop != null)
            {
                TriggerDialogue(
                    currentShop.InsufficientFundsKnot);
            }
        }

        private bool TryPurchase(
            ShopEntry entry)
        {
            if (PlayerManager.Instance == null ||
                Inventory.Instance == null)
            {
                return false;
            }

            if (entry.Price >
                PlayerManager.Instance.Currency)
            {
                return false;
            }

            Inventory.Instance.AddItem(
                entry.Item);

            Bus<CurrencyUpdatedEvent>.Raise(
                new CurrencyUpdatedEvent(
                    -entry.Price));

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

            ScrollRect scrollRect =
                buyScrollViewRect
                    .GetComponent<ScrollRect>();

            if (scrollRect != null)
            {
                scrollRect.vertical =
                    slotCount >
                    maximumVisibleBuySlots;

                scrollRect.StopMovement();
                scrollRect.verticalNormalizedPosition =
                    1f;
            }
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



            Debug.Log(
                $"[ShopMenuUI] Created {sellSlots.Count} sell slots. " +
                $"Sell container child count: " +
                $"{(sellSlotContainer != null ? sellSlotContainer.childCount : -1)}",
                this);

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

        private void HandleSellRequested(
            InventoryItem inventoryItem)
        {
            if (inventoryItem == null ||
                inventoryItem.data == null)
            {
                return;
            }

            /*
             * Placeholder:
             * This will open the sell-quantity panel.
             */
            Debug.Log(
                $"Selected {inventoryItem.data.ItemName} " +
                $"for selling. " +
                $"Owned: {inventoryItem.stackSize}. " +
                $"Sell price each: " +
                $"${inventoryItem.data.SellPrice}.",
                this);
        }

        private void UpdateSellScrollView(
    int slotCount)
{
    Debug.Log(
        $"[ShopMenuUI] UpdateSellScrollView called\n" +
        $"slotCount: {slotCount}\n" +
        $"sellScrollViewRect: " +
        $"{(sellScrollViewRect != null ? sellScrollViewRect.name : "NULL")}\n" +
        $"sellGridLayoutGroup: " +
        $"{(sellGridLayoutGroup != null ? sellGridLayoutGroup.name : "NULL")}",
        this);

    if (sellScrollViewRect == null ||
        sellGridLayoutGroup == null)
    {
        Debug.LogError(
            "[ShopMenuUI] Cannot resize sell Scroll View because " +
            "one or more references are missing.",
            this);

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
        sellGridLayoutGroup.cellSize.y;

    float verticalSpacing =
        sellGridLayoutGroup.spacing.y;

    float verticalPadding =
        sellGridLayoutGroup.padding.top +
        sellGridLayoutGroup.padding.bottom;

    float calculatedHeight =
        cellHeight * visibleRows +
        verticalSpacing *
        Mathf.Max(
            0,
            visibleRows - 1) +
        verticalPadding;

    Debug.Log(
        $"[ShopMenuUI] Sell Scroll View calculation\n" +
        $"columnCount: {columnCount}\n" +
        $"totalRows: {totalRows}\n" +
        $"visibleRows: {visibleRows}\n" +
        $"maximumVisibleSellRows: {maximumVisibleSellRows}\n" +
        $"cellSize: {sellGridLayoutGroup.cellSize}\n" +
        $"spacing: {sellGridLayoutGroup.spacing}\n" +
        $"verticalPadding: {verticalPadding}\n" +
        $"calculatedHeight: {calculatedHeight}\n" +
        $"height before resize: {sellScrollViewRect.rect.height}",
        this);

    sellScrollViewRect.SetSizeWithCurrentAnchors(
        RectTransform.Axis.Vertical,
        calculatedHeight);

    Debug.Log(
        $"[ShopMenuUI] Height immediately after resize: " +
        $"{sellScrollViewRect.rect.height}",
        this);

    StartCoroutine(
        LogSellScrollViewHeightNextFrame());
}

private IEnumerator LogSellScrollViewHeightNextFrame()
{
    yield return null;

    if (sellScrollViewRect == null)
        yield break;

    Debug.Log(
        $"[ShopMenuUI] Sell Scroll View height next frame: " +
        $"{sellScrollViewRect.rect.height}",
        this);
}

       private int GetSellColumnCount()
{
    if (sellGridLayoutGroup == null)
    {
        Debug.LogWarning(
            "[ShopMenuUI] Sell Grid Layout Group is null.",
            this);

        return 1;
    }

    Debug.Log(
        $"[ShopMenuUI] Grid constraint: " +
        $"{sellGridLayoutGroup.constraint}, " +
        $"constraint count: " +
        $"{sellGridLayoutGroup.constraintCount}",
        this);

    if (sellGridLayoutGroup.constraint ==
        GridLayoutGroup.Constraint.FixedColumnCount)
    {
        return Mathf.Max(
            1,
            sellGridLayoutGroup.constraintCount);
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
        sellGridLayoutGroup.cellSize.x;

    float horizontalSpacing =
        sellGridLayoutGroup.spacing.x;

    int calculatedColumns =
        Mathf.Max(
            1,
            Mathf.FloorToInt(
                (availableWidth +
                 horizontalSpacing) /
                (cellWidth +
                 horizontalSpacing)));

    Debug.Log(
        $"[ShopMenuUI] Dynamic column calculation\n" +
        $"availableWidth: {availableWidth}\n" +
        $"cellWidth: {cellWidth}\n" +
        $"horizontalSpacing: {horizontalSpacing}\n" +
        $"calculatedColumns: {calculatedColumns}",
        this);

    return calculatedColumns;
}

        #endregion

        private void TriggerDialogue(
            string knotName)
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
    }
}