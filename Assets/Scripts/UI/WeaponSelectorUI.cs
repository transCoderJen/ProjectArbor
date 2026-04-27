
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ShiftedSignalGames.GOF.UISpace
{
    public class WeaponSelectorUI : MonoBehaviour
    {
        [SerializeField] private Button[] weaponButtons = new Button[5];
        public InputActionReference rightThumbstick;

        public ItemData_Equipment[] wheelAssignedWeapons = new ItemData_Equipment[5];
        
        private void Awake()
        {
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                if (wheelAssignedWeapons[i] != null && wheelAssignedWeapons[i].EquipmentType != EquipmentType.Weapon)
                {
                    Debug.LogWarning($"Weapon at index {i} is not of type Weapon: {wheelAssignedWeapons[i].ItemName}");
                }
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Position buttons in a circle around the center of the screen
            float radius = 150f;
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;

            for (int i = 0; i < weaponButtons.Length; i++)
            {
                float angle = (i * 72f) * Mathf.Deg2Rad;
                float x = centerX + radius * Mathf.Sin(angle);
                float y = centerY + radius * Mathf.Cos(angle);
                
                weaponButtons[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(x - centerX, y - centerY);
            }

            for (int i = 0; i < weaponButtons.Length; i++)
            {
                if (wheelAssignedWeapons[i] != null)
                    weaponButtons[i].image.sprite = wheelAssignedWeapons[i].Icon;
            }
        }

        void OnDisable()
        {
            
        }

        // Update is called once per frame
        private int lastButtonIndex = -1;

        void Update()
        {
            Vector2 input = rightThumbstick.action.ReadValue<Vector2>();
            if (input.magnitude > 0.5f)
            {
                float angle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;
                
                int buttonIndex = Mathf.RoundToInt(angle / 72f) % 5;
                
                if (buttonIndex != lastButtonIndex)
                {               
                    if (lastButtonIndex != -1)
                    {
                        weaponButtons[lastButtonIndex].OnDeselect(null);
                    }
                    
                    weaponButtons[buttonIndex].OnSelect(null);

                    if (wheelAssignedWeapons[buttonIndex] != null)
                    {
                        Debug.Log("Weapon Equip event being raised");
                        Bus<WeaponQuickSelectEvent>.Raise(new WeaponQuickSelectEvent(wheelAssignedWeapons[buttonIndex]));
                    }
                    lastButtonIndex = buttonIndex;
                }
            }
        }
    }
}
