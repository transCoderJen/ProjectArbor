using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.UserInterface.Managers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface.Components
{
    [RequireComponent(typeof(Button))]
    public class UIBuildQueueButton : MonoBehaviour, IUIElement<AbstractUnitSO, UnityAction>
    {
        [SerializeField] private Image icon;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            Disable();
        }

        public void EnableFor(AbstractUnitSO unit, UnityAction callback)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = true;
            button.onClick.AddListener(callback);
            icon.gameObject.SetActive(true);
            icon.sprite = unit.Icon;
        }

        public void Disable()
        {
            button.interactable = false;
            button.onClick.RemoveAllListeners();
            icon.gameObject.SetActive(false);
        }  
    }
}