
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface
{
    public class ToolSelectorUI : MonoBehaviour
    {
        [SerializeField] private Button[] toolButtons = new Button[4];
        public InputActionReference rightThumbstick;

        void Start()
        {
            // Position buttons in a circle around the center of the screen
            float radius = 150f;
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;

            for (int i = 0; i < toolButtons.Length; i++)
            {
                float angle = (i * 90f) * Mathf.Deg2Rad;
                float x = centerX + radius * Mathf.Sin(angle);
                float y = centerY + radius * Mathf.Cos(angle);
                
                toolButtons[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(x - centerX, y - centerY);
            }      
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
                
                int buttonIndex = Mathf.RoundToInt(angle / 72f) % 4;
                
                if (buttonIndex != lastButtonIndex)
                {

                    if (lastButtonIndex != -1)
                    {
                        toolButtons[lastButtonIndex].OnDeselect(null);
                    }
                    
                    toolButtons[buttonIndex].OnSelect(null);

                    Bus<ToolEquipEvent>.Raise(new ToolEquipEvent(buttonIndex));
                    lastButtonIndex = buttonIndex;
                }
            }
        }
    }
}
