
using System;
using ShiftedSignal.Garden.Commands;
using ShiftedSignal.Garden.UserInterface.Managers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface.Components
{
    [RequireComponent(typeof(Button))]
    public class UIActionButton : MonoBehaviour, IUIElement<BaseCommand, UnityAction>, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image icon;
        private Tooltip tooltip;
        private RectTransform rectTransform;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            rectTransform = GetComponent<RectTransform>();
            tooltip = GetComponentInChildren<Tooltip>(true);

            Disable();
        }

        public void EnableFor(BaseCommand action, UnityAction onClick)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
            SetIcon(action.Icon);
            button.interactable = !action.IsLocked(new CommandContext());

            if (tooltip != null)
            {
                tooltip.SetText(action);
            }
        }

        public void Disable()
        {
            SetIcon(null);
            button.interactable = false;
            button.onClick.RemoveAllListeners();
            if (tooltip != null)
            {
                tooltip.Hide();
            }
            CancelInvoke();
        }

        private void SetIcon(Sprite icon)
        {
            if (icon == null)
            {
                this.icon.enabled = false;
            }
            else
            {
                this.icon.sprite = icon;
                this.icon.enabled = true;
            }
        }

        public void OnPointerEnter(PointerEventData _)
        {
            Invoke(nameof(ShowToolTip), tooltip.HoverDelay);
        }

        private void ShowToolTip()
        {
            if (tooltip != null && icon.enabled)
            {
                tooltip.Show();

                tooltip.RectTransform.position = new Vector2(
                    rectTransform.position.x + rectTransform.rect.width / 2f ,
                    rectTransform.position.y + rectTransform.rect.height / 2f - 4
                );
            }
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (tooltip != null)
            {
                tooltip.Hide();
            }
            CancelInvoke();
        }

    }
}
