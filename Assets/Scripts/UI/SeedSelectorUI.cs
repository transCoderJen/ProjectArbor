using System;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface
{
    public class SeedSelectorUI : MonoBehaviour
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

        private int lastButtonIndex = -1;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            PositionButtons();
        }

        private void OnEnable()
        {
            rightThumbstick?.action.Enable();
            Bus<AssignSeedToQuickSelectEvent>.OnEvent -= AddSeedToFirstOpenSlot;
            Bus<AssignSeedToQuickSelectEvent>.OnEvent += AddSeedToFirstOpenSlot;
        }

        private void AddSeedToFirstOpenSlot(AssignSeedToQuickSelectEvent evt)
        {
            for (int i = 0; i < seedSlots.Length; i++)
            {
                if (seedSlots[i].item.data == evt.Seed)
                {
                    return;
                }
            }
            
            for (int i = 0; i < seedSlots.Length; i++)
            {
                if (seedSlots[i].item.data == null)
                {
                    seedSlots[i].item.data = evt.Seed;
                    seedButtons[i].image.sprite = evt.Seed.Icon;
                    break;
                }
            }
        }

        private void OnDisable()
        {
            rightThumbstick?.action.Disable();
            // Bus<AssignSeedToQuickSelectEvent>.OnEvent -= AddSeedToFirstOpenSlot;
            ClearSelection();
        }

        private void Update()
        {
            Vector2 controllerInput = rightThumbstick.action.ReadValue<Vector2>();

            if (controllerInput.magnitude > controllerDeadZone)
            {
                SelectFromDirection(controllerInput);
                return;
            }

            SelectFromMouse();
        }

        public void AddSeedToFirstOpenSlot(ItemData_Seed seed)
        {
            for (int i = 0; i < seedSlots.Length; i++)
            {
                if (seedSlots[i].item.data == null)
                {
                    seedSlots[i].item.data = seed;
                    seedButtons[i].image.sprite = seed.Icon;
                    break;
                }
            }
        }

        private void PositionButtons()
        {
            for (int i = 0; i < seedButtons.Length; i++)
            {
                if (seedButtons[i] == null)
                    continue;

                float angle = i * 36f * Mathf.Deg2Rad;

                float x = radius * Mathf.Sin(angle);
                float y = radius * Mathf.Cos(angle) + verticalOffset;

                seedButtons[i].GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(x, y);
            }
        }

        private void SelectFromMouse()
        {
            Vector2 wheelCenterScreenPosition = RectTransformUtility.WorldToScreenPoint(
                null,
                rectTransform.position + new Vector3(0f, verticalOffset, 0f)
            );

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector2 direction = mousePosition - wheelCenterScreenPosition;

            if (direction.magnitude < mouseMinDistanceFromCenter)
                return;

            SelectFromDirection(direction.normalized);
        }

        private void SelectFromDirection(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

            if (angle < 0f)
                angle += 360f;

            int buttonIndex = Mathf.RoundToInt(angle / 36f) % seedButtons.Length;

            SelectButton(buttonIndex);
        }

        private void SelectButton(int buttonIndex)
        {
            if (buttonIndex == lastButtonIndex)
                return;

            if (lastButtonIndex != -1 && seedButtons[lastButtonIndex] != null)
            {
                seedButtons[lastButtonIndex].OnDeselect(null);
            }

            if (seedButtons[buttonIndex] != null)
            {
                seedButtons[buttonIndex].OnSelect(null);
            }

            Bus<SeedEquipEvent>.Raise(new SeedEquipEvent(seedSlots[buttonIndex].item.data));

            lastButtonIndex = buttonIndex;
        }

        private void ClearSelection()
        {
            if (lastButtonIndex != -1 && seedButtons[lastButtonIndex] != null)
            {
                seedButtons[lastButtonIndex].OnDeselect(null);
            }

            lastButtonIndex = -1;
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