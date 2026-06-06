using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.SaveAndLoad;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ShiftedSignalGames.GOF.UISpace
{
    public class WeaponSelectorUI : MonoBehaviour, ISaveManager
    {
        [Header("Buttons")]
        [SerializeField] private Button[] weaponButtons = new Button[5];

        [Header("Input")]
        [SerializeField] private InputActionReference rightThumbstick;
        [SerializeField] private float controllerDeadZone = 0.5f;
        [SerializeField] private float mouseMinDistanceFromCenter = 40f;

        [Header("Weapons")]
        [SerializeField] private ItemData_Equipment[] wheelAssignedWeapons = new ItemData_Equipment[5];

        [Header("Layout")]
        [SerializeField] private float radius = 150f;
        
        [Tooltip("Moves the entire selector wheel up or down.")]
        [SerializeField] private float verticalOffset = 0f;

        private int lastButtonIndex = -1;
        private RectTransform rectTransform;
        private Vector2 wheelCenterScreenPosition;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            for (int i = 0; i < wheelAssignedWeapons.Length; i++)
            {
                if (wheelAssignedWeapons[i] != null && wheelAssignedWeapons[i].EquipmentType != EquipmentType.Weapon)
                {
                    Debug.LogWarning($"Weapon at index {i} is not of type Weapon: {wheelAssignedWeapons[i].ItemName}");
                }
            }
        }

        private void Start()
        {
            PositionButtons();
            UpdateButtonIcons();
        }

        private void OnEnable()
        {
            rightThumbstick?.action.Enable();
            Time.timeScale = 0f;
        }

        private void OnDisable()
        {
            rightThumbstick?.action.Disable();
            ClearSelection();
            Time.timeScale = 1f;
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

        private void PositionButtons()
        {
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                if (weaponButtons[i] == null)
                    continue;

                float angle = i * 72f * Mathf.Deg2Rad;

                float x = radius * Mathf.Sin(angle);
                float y = radius * Mathf.Cos(angle) + verticalOffset;

                weaponButtons[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            }
        }

        private void UpdateButtonIcons()
        {
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                if (weaponButtons[i] == null)
                    continue;

                if (wheelAssignedWeapons[i] != null)
                {
                    weaponButtons[i].image.sprite = wheelAssignedWeapons[i].Icon;
                }
            }
        }

        private void SelectFromMouse()
        {
            wheelCenterScreenPosition = RectTransformUtility.WorldToScreenPoint(
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

            int buttonIndex = Mathf.RoundToInt(angle / 72f) % weaponButtons.Length;

            SelectButton(buttonIndex);
        }

        private void SelectButton(int buttonIndex)
        {
            if (buttonIndex == lastButtonIndex)
                return;

            if (lastButtonIndex != -1 && weaponButtons[lastButtonIndex] != null)
            {
                weaponButtons[lastButtonIndex].OnDeselect(null);
            }

            if (weaponButtons[buttonIndex] != null)
            {
                weaponButtons[buttonIndex].OnSelect(null);
            }

            if (wheelAssignedWeapons[buttonIndex] != null)
            {
                Bus<WeaponQuickSelectEvent>.Raise(
                    new WeaponQuickSelectEvent(wheelAssignedWeapons[buttonIndex])
                );
            }

            lastButtonIndex = buttonIndex;
        }

        private void ClearSelection()
        {
            if (lastButtonIndex != -1 && weaponButtons[lastButtonIndex] != null)
            {
                weaponButtons[lastButtonIndex].OnDeselect(null);
            }

            lastButtonIndex = -1;
        }

        public void SaveData(ref GameData data)
        {
            data.weaponWheelIds.Clear();
            for (int i = 0; i < wheelAssignedWeapons.Length; i++)
            {
                if (wheelAssignedWeapons[i] != null)
                    data.weaponWheelIds.Add(wheelAssignedWeapons[i].ItemID);
                else
                    data.weaponWheelIds.Add(""); // Save an empty string for blank slots
            }
        }

        public void LoadData(GameData data)
        {
            if (data.weaponWheelIds == null || data.weaponWheelIds.Count == 0) return;

            for (int i = 0; i < data.weaponWheelIds.Count && i < wheelAssignedWeapons.Length; i++)
            {
                string id = data.weaponWheelIds[i];
                if (string.IsNullOrEmpty(id))
                {
                    wheelAssignedWeapons[i] = null;
                    continue;
                }

                // Look up weapon from the central database
                foreach (var item in Inventory.Instance.itemDataBase)
                {
                    if (item.ItemID == id)
                    {
                        wheelAssignedWeapons[i] = item as ShiftedSignal.Garden.ItemsAndInventory.ItemData_Equipment;
                        break;
                    }
                }
            }
            
            UpdateButtonIcons(); // Refresh visuals
        }
    }
}