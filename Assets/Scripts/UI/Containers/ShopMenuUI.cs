using System.Collections.Generic;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Shops;
using ShiftedSignal.Garden.UserInterface.Components;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Linq;

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
        [SerializeField] private ScrollRect buyScrollRect;
        [SerializeField] private RectTransform buyScrollViewRect;
        [SerializeField] private VerticalLayoutGroup buyLayoutGroup;
        [SerializeField] private RectTransform buyContentRect;
        [SerializeField] private Transform buySlotContainer;
        [SerializeField] private UI_ShopSlot shopSlotPrefab;
        [SerializeField] private float buySlotHeight = 80f;
        [SerializeField] private int maximumVisibleBuySlots = 5;

        [SerializeField] private float mouseScrollSpeed = 0.15f;

        private readonly List<Button> buySlotButtons = new();

        private int firstVisibleBuyIndex = 0;
        private GameObject lastSelectedBuyObject;

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
                RefreshBuySlots(true);
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

            if (buyScrollRect != null)
            {
                buyScrollRect.StopMovement();
                buyScrollRect.verticalNormalizedPosition = 1f;
            }

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

        private void RefreshBuySlots(
            bool resetPosition = false)
        {
            int previousSelectedIndex =
                resetPosition
                    ? 0
                    : GetCurrentSelectedBuyIndex();

            int previousFirstVisibleIndex =
                resetPosition
                    ? 0
                    : firstVisibleBuyIndex;

            ClearBuySlots();


            if (currentShop == null ||
                currentShop.Items == null ||
                buySlotContainer == null ||
                shopSlotPrefab == null)
            {
                return;
            }

            foreach (ItemData item in currentShop.Items
                        .Where(item => item != null)
                        .OrderBy(item => item.BaseValue))
            {
                UI_ShopSlot slot =
                    Instantiate(
                        shopSlotPrefab,
                        buySlotContainer);

                int ownedAmount =
                    Inventory.Instance != null
                        ? Inventory.Instance.GetItemAmount(item)
                        : 0;

                slot.Setup(
                    item,
                    ownedAmount,
                    HandlePurchaseRequested,
                    tooltip);

                buySlots.Add(slot);

                Button button =
                    slot.GetComponent<Button>();

                if (button == null)
                {
                    button =
                        slot.GetComponentInChildren<Button>();
                }

                if (button != null)
                {
                    buySlotButtons.Add(button);
                }
            }

            UpdateBuyScrollView(
                buySlots.Count);

            ConfigureBuyNavigation();

            Canvas.ForceUpdateCanvases();

            RestoreBuySelection(
                previousSelectedIndex,
                previousFirstVisibleIndex);
        }

        private int GetCurrentSelectedBuyIndex()
        {
            if (EventSystem.current == null)
                return 0;

            GameObject selected =
                EventSystem.current.currentSelectedGameObject;

            if (selected == null)
                return 0;

            int selectedIndex =
                GetSelectedBuyIndex(selected);

            return selectedIndex >= 0
                ? selectedIndex
                : 0;
        }

        private void RestoreBuySelection(
            int selectedIndex,
            int firstVisibleIndex)
        {
            if (buySlotButtons.Count == 0)
                return;

            selectedIndex =
                Mathf.Clamp(
                    selectedIndex,
                    0,
                    buySlotButtons.Count - 1);

            int maximumStartIndex =
                Mathf.Max(
                    0,
                    buySlotButtons.Count -
                    maximumVisibleBuySlots);

            firstVisibleBuyIndex =
                Mathf.Clamp(
                    firstVisibleIndex,
                    0,
                    maximumStartIndex);

            Canvas.ForceUpdateCanvases();

            Button button =
                buySlotButtons[selectedIndex];

            if (button != null &&
                EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(
                    button.gameObject);

                lastSelectedBuyObject =
                    button.gameObject;
            }

            SnapBuyScrollToIndex(
                firstVisibleBuyIndex);

            Canvas.ForceUpdateCanvases();
        }

        public void SnapBuyScrollToNearestItem()
        {
            if (buyScrollRect == null ||
                buySlotButtons.Count <= maximumVisibleBuySlots)
            {
                return;
            }

            int maximumStartIndex =
                Mathf.Max(
                    0,
                    buySlotButtons.Count -
                    maximumVisibleBuySlots);

            float normalizedFromTop =
                1f -
                buyScrollRect.verticalNormalizedPosition;

            int nearestIndex =
                Mathf.RoundToInt(
                    normalizedFromTop *
                    maximumStartIndex);

            firstVisibleBuyIndex =
                Mathf.Clamp(
                    nearestIndex,
                    0,
                    maximumStartIndex);

            SnapBuyScrollToIndex(
                firstVisibleBuyIndex);
        }

        private void ConfigureBuyNavigation()
        {
            for (int i = 0; i < buySlotButtons.Count; i++)
            {
                Button button = buySlotButtons[i];

                if (button == null)
                    continue;

                Navigation navigation = button.navigation;

                navigation.mode =
                    Navigation.Mode.Explicit;

                navigation.selectOnUp =
                    i > 0
                        ? buySlotButtons[i - 1]
                        : null;

                navigation.selectOnDown =
                    i < buySlotButtons.Count - 1
                        ? buySlotButtons[i + 1]
                        : null;

                button.navigation = navigation;
            }
        }

        private void SelectFirstBuySlot()
        {
            if (buySlotButtons.Count == 0 ||
                EventSystem.current == null)
            {
                return;
            }

            Button firstButton =
                buySlotButtons[0];

            if (firstButton == null)
                return;

            EventSystem.current.SetSelectedGameObject(
                firstButton.gameObject);

            lastSelectedBuyObject =
                firstButton.gameObject;
        }

        private void ClearBuySlots()
        {
            foreach (UI_ShopSlot slot in buySlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }

            buySlots.Clear();
            buySlotButtons.Clear();

            firstVisibleBuyIndex = 0;
            lastSelectedBuyObject = null;
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

            Inventory.Instance.AddItem(item, item.AmountPerPurchase);

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

        private void Update()
        {
            if (menuRoot == null ||
                !menuRoot.activeInHierarchy)
            {
                return;
            }

            if (currentMode != ShopMode.Buy ||
                buyScrollRect == null)
            {
                return;
            }

            HandleBuyMouseScroll();
            HandleBuySelectionScroll();
        }

        private void HandleBuySelectionScroll()
        {
            if (EventSystem.current == null ||
                buySlotButtons.Count == 0)
            {
                return;
            }

            GameObject selected =
                EventSystem.current.currentSelectedGameObject;

            if (selected == null ||
                selected == lastSelectedBuyObject)
            {
                return;
            }

            int selectedIndex =
                GetSelectedBuyIndex(selected);

            if (selectedIndex < 0)
                return;

            lastSelectedBuyObject = selected;

            ScrollToBuyIndex(selectedIndex);
        }

        private void HandleBuyMouseScroll()
        {
            if (Mouse.current == null)
                return;

            if (buySlotButtons.Count <= maximumVisibleBuySlots)
                return;

            float scroll =
                Mouse.current.scroll.ReadValue().y;

            if (Mathf.Approximately(scroll, 0f))
                return;

            int direction =
                scroll > 0f
                    ? -1
                    : 1;

            int maximumStartIndex =
                Mathf.Max(
                    0,
                    buySlotButtons.Count -
                    maximumVisibleBuySlots);

            firstVisibleBuyIndex =
                Mathf.Clamp(
                    firstVisibleBuyIndex + direction,
                    0,
                    maximumStartIndex);

            SnapBuyScrollToIndex(
                firstVisibleBuyIndex);
        }

        private void SnapBuyScrollToIndex(
            int firstIndex)
        {
            if (buyScrollRect == null)
                return;

            int maximumStartIndex =
                Mathf.Max(
                    0,
                    buySlotButtons.Count -
                    maximumVisibleBuySlots);

            if (maximumStartIndex == 0)
            {
                buyScrollRect.verticalNormalizedPosition = 1f;
                return;
            }

            firstIndex =
                Mathf.Clamp(
                    firstIndex,
                    0,
                    maximumStartIndex);

            float normalized =
                firstIndex /
                (float)maximumStartIndex;

            buyScrollRect.StopMovement();

            buyScrollRect.verticalNormalizedPosition =
                1f - normalized;
        }

        private int GetSelectedBuyIndex(
            GameObject selected)
        {
            for (int i = 0; i < buySlotButtons.Count; i++)
            {
                Button button =
                    buySlotButtons[i];

                if (button == null)
                    continue;

                if (button.gameObject == selected ||
                    selected.transform.IsChildOf(
                        button.transform))
                {
                    return i;
                }
            }

            return -1;
        }

        private void ScrollToBuyIndex(
            int selectedIndex)
        {
            if (buyScrollRect == null)
                return;

            int visibleCount =
                Mathf.Min(
                    maximumVisibleBuySlots,
                    buySlotButtons.Count);

            if (buySlotButtons.Count <= visibleCount)
            {
                firstVisibleBuyIndex = 0;

                SnapBuyScrollToIndex(0);

                return;
            }

            if (selectedIndex < firstVisibleBuyIndex)
            {
                firstVisibleBuyIndex =
                    selectedIndex;
            }
            else if (
                selectedIndex >=
                firstVisibleBuyIndex + visibleCount)
            {
                firstVisibleBuyIndex =
                    selectedIndex -
                    visibleCount + 1;
            }
            else
            {
                return;
            }

            SnapBuyScrollToIndex(
                firstVisibleBuyIndex);
        }

        private void UpdateBuyScrollView(int slotCount)
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

            // Size of the visible Scroll View.
            float visibleHeight =
                buySlotHeight * visibleSlotCount +
                spacing * Mathf.Max(
                    0,
                    visibleSlotCount - 1) +
                verticalPadding;

            buyScrollViewRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                visibleHeight);

            // Size of the actual scrolling Content.
            if (buyContentRect != null)
            {
                float contentHeight =
                    buySlotHeight * slotCount +
                    spacing * Mathf.Max(
                        0,
                        slotCount - 1) +
                    verticalPadding;

                buyContentRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    contentHeight);
            }

            if (buyScrollRect != null)
            {
                bool shouldScroll =
                    slotCount >
                    maximumVisibleBuySlots;

                buyScrollRect.vertical =
                    shouldScroll;

                if (buyScrollRect.verticalScrollbar != null)
                {
                    buyScrollRect
                        .verticalScrollbar
                        .gameObject
                        .SetActive(shouldScroll);
                }

                if (!shouldScroll)
                {
                    buyScrollRect
                        .verticalNormalizedPosition = 1f;
                }
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

            UpdateSellScrollView(
                sellSlots.Count);
        }

        private void PopulateSellSlots(
            List<InventoryItem> inventoryItems)
        {
            if (inventoryItems == null)
                return;

            foreach (InventoryItem inventoryItem in inventoryItems)
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
            foreach (UI_SellSlot slot in sellSlots)
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
            Inventory.Instance.RemoveItem(item);

            /*
            * Give the player the sale value.
            */
            Bus<CurrencyUpdatedEvent>.Raise(
                new CurrencyUpdatedEvent(sellPrice));

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

        private void UpdateSellScrollView(int slotCount)
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