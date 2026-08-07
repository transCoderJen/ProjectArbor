using TMPro;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.SaveAndLoad;
using ShiftedSignal.Garden.UserInterface.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface.Containers
{
    public class SeedSelectorUI : MonoBehaviour, ISaveManager
    {
        [Header("Buttons")]
        [SerializeField] private Button[] seedButtons;
        [SerializeField] private UI_ItemSlot[] seedSlots;

        [Header("Input")]
        [SerializeField] private InputActionReference rightThumbstick;
        [SerializeField] private float controllerDeadZone = 0.5f;
        [SerializeField] private float mouseMinDistanceFromCenter = 40f;

        [Header("Layout")]
        [SerializeField] private float radius = 150f;

        [Tooltip("Moves the entire selector wheel up or down.")]
        [SerializeField] private float verticalOffset = 0f;

        private TextMeshProUGUI[] amountTexts;

        private int lastButtonIndex = -1;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            amountTexts = new TextMeshProUGUI[seedButtons.Length];

            for (int i = 0; i < seedButtons.Length; i++)
            {
                if (seedButtons[i] != null)
                {
                    amountTexts[i] =
                        seedButtons[i]
                            .GetComponentInChildren<TextMeshProUGUI>();
                }
            }
        }

        private void Start()
        {
            PositionButtons();
            RefreshSeedAmounts();
        }

        private void OnEnable()
        {
            rightThumbstick?.action.Enable();

            Bus<AssignSeedToQuickSelectEvent>.OnEvent -=
                AddSeedToFirstOpenSlot;

            Bus<AssignSeedToQuickSelectEvent>.OnEvent +=
                AddSeedToFirstOpenSlot;

            RefreshSeedAmounts();
        }

        private void OnDisable()
        {
            rightThumbstick?.action.Disable();

            Bus<AssignSeedToQuickSelectEvent>.OnEvent -=
                AddSeedToFirstOpenSlot;

            ClearSelection();
        }

        private void Update()
        {
            Vector2 controllerInput =
                rightThumbstick.action.ReadValue<Vector2>();

            if (controllerInput.magnitude > controllerDeadZone)
            {
                SelectFromDirection(controllerInput);
                return;
            }

            SelectFromMouse();
        }

        private void AddSeedToFirstOpenSlot(
            AssignSeedToQuickSelectEvent evt)
        {
            if (evt.Seed == null)
                return;

            TryAddSeedToFirstOpenSlot(evt.Seed);
        }

        public bool TryAddSeedToFirstOpenSlot(
            ItemData_Seed seed)
        {
            if (seed == null)
                return false;

            // Already assigned?
            for (int i = 0; i < seedSlots.Length; i++)
            {
                if (seedSlots[i] == null ||
                    seedSlots[i].item == null)
                {
                    continue;
                }

                if (seedSlots[i].item.data == seed)
                {
                    RefreshSeedAmount(i);
                    return false;
                }
            }

            // First empty slot.
            for (int i = 0; i < seedSlots.Length; i++)
            {
                if (seedSlots[i] == null)
                    continue;

                bool isEmpty =
                    seedSlots[i].item == null ||
                    seedSlots[i].item.data == null;

                if (!isEmpty)
                    continue;

                InventoryItem inventoryItem =
                    new InventoryItem(seed);

                inventoryItem.stackSize =
                    GetSeedAmount(seed);

                seedSlots[i].UpdateSlot(inventoryItem);

                if (seedButtons[i] != null)
                    seedButtons[i].image.sprite = seed.Icon;

                RefreshSeedAmount(i);

                return true;
            }

            return false;
        }

        public void RefreshSeedAmounts()
        {
            for (int i = 0; i < seedSlots.Length; i++)
            {
                RefreshSeedAmount(i);
            }
        }

        private void RefreshSeedAmount(
            int index)
        {
            if (index < 0 ||
                index >= seedSlots.Length)
            {
                return;
            }

            UI_ItemSlot slot =
                seedSlots[index];

            if (slot == null ||
                slot.item == null ||
                slot.item.data is not ItemData_Seed seed)
            {
                SetAmountText(index, string.Empty);

                if (seedButtons[index] != null)
                    seedButtons[index].interactable = false;

                return;
            }

            int amount =
                GetSeedAmount(seed);

            slot.item.stackSize = amount;

            SetAmountText(index, amount.ToString());

            if (seedButtons[index] != null)
                seedButtons[index].interactable =
                    amount > 0;
        }

        private int GetSeedAmount(
            ItemData_Seed seed)
        {
            if (seed == null ||
                Inventory.Instance == null)
            {
                return 0;
            }

            return Inventory.Instance.GetItemAmount(seed);
        }

        private void SetAmountText(
            int index,
            string amount)
        {
            if (index < 0 ||
                index >= amountTexts.Length ||
                amountTexts[index] == null)
            {
                return;
            }

            amountTexts[index].text = amount;
        }

        private void PositionButtons()
        {
            for (int i = 0; i < seedButtons.Length; i++)
            {
                if (seedButtons[i] == null)
                    continue;

                float angle =
                    i * 36f * Mathf.Deg2Rad;

                float x =
                    radius * Mathf.Sin(angle);

                float y =
                    radius * Mathf.Cos(angle) +
                    verticalOffset;

                seedButtons[i]
                    .GetComponent<RectTransform>()
                    .anchoredPosition =
                    new Vector2(x, y);
            }
        }

        private void SelectFromMouse()
        {
            Vector2 wheelCenter =
                RectTransformUtility.WorldToScreenPoint(
                    null,
                    rectTransform.position +
                    new Vector3(
                        0f,
                        verticalOffset,
                        0f));

            Vector2 mouse =
                Mouse.current.position.ReadValue();

            Vector2 direction =
                mouse - wheelCenter;

            if (direction.magnitude <
                mouseMinDistanceFromCenter)
            {
                return;
            }

            SelectFromDirection(
                direction.normalized);
        }

        private void SelectFromDirection(
            Vector2 direction)
        {
            float angle =
                Mathf.Atan2(
                    direction.x,
                    direction.y) *
                Mathf.Rad2Deg;

            if (angle < 0f)
                angle += 360f;

            int index =
                Mathf.RoundToInt(angle / 36f) %
                seedButtons.Length;

            SelectButton(index);
        }

        private void SelectButton(
            int index)
        {
            if (index == lastButtonIndex)
                return;

            if (index < 0 ||
                index >= seedButtons.Length)
            {
                return;
            }

            ItemData_Seed seed = null;

            if (seedSlots[index] != null &&
                seedSlots[index].item != null)
            {
                seed =
                    seedSlots[index]
                        .item.data as ItemData_Seed;
            }

            if (seed == null)
                return;

            if (GetSeedAmount(seed) <= 0)
                return;

            if (lastButtonIndex != -1 &&
                seedButtons[lastButtonIndex] != null)
            {
                seedButtons[lastButtonIndex]
                    .OnDeselect(null);
            }

            seedButtons[index].OnSelect(null);

            Bus<SeedEquipEvent>.Raise(
                new SeedEquipEvent(seed));

            lastButtonIndex = index;
        }

        private void ClearSelection()
        {
            if (lastButtonIndex != -1 &&
                seedButtons[lastButtonIndex] != null)
            {
                seedButtons[lastButtonIndex]
                    .OnDeselect(null);
            }

            lastButtonIndex = -1;
        }

        public void SaveData(
            ref GameData data)
        {
            data.seedWheelIds.Clear();

            for (int i = 0; i < seedSlots.Length; i++)
            {
                if (seedSlots[i] != null &&
                    seedSlots[i].item != null &&
                    seedSlots[i].item.data != null)
                {
                    data.seedWheelIds.Add(
                        seedSlots[i]
                            .item.data.ItemID);
                }
                else
                {
                    data.seedWheelIds.Add("");
                }
            }
        }

        public void LoadData(
            GameData data)
        {
            if (data.seedWheelIds == null ||
                data.seedWheelIds.Count == 0)
            {
                return;
            }

            for (int i = 0;
                 i < data.seedWheelIds.Count &&
                 i < seedSlots.Length;
                 i++)
            {
                string id =
                    data.seedWheelIds[i];

                if (string.IsNullOrEmpty(id))
                {
                    seedSlots[i].CleanUpSlot();

                    if (seedButtons[i] != null)
                        seedButtons[i].image.sprite = null;

                    SetAmountText(i, string.Empty);

                    continue;
                }

                foreach (ItemData item
                         in Inventory.Instance.itemDataBase)
                {
                    if (item.ItemID != id)
                        continue;

                    if (item is ItemData_Seed seed)
                    {
                        InventoryItem inventoryItem =
                            new InventoryItem(seed);

                        inventoryItem.stackSize =
                            GetSeedAmount(seed);

                        seedSlots[i]
                            .UpdateSlot(inventoryItem);

                        if (seedButtons[i] != null)
                            seedButtons[i].image.sprite =
                                seed.Icon;
                    }

                    break;
                }
            }

            RefreshSeedAmounts();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                PositionButtons();
        }
#endif
    }
}