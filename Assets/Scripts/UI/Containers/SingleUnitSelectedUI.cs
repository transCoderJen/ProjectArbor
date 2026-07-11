using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.UserInterface.Managers;
using TMPro;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface.Containers
{
    public class SingleUnitSelectedUI : MonoBehaviour, IUIElement<AbstractCommandable>
    {
        [SerializeField] private TextMeshProUGUI unitName;

        public void EnableFor(AbstractCommandable commandable)
        {
            gameObject.SetActive(true);
            unitName.SetText(commandable.Config.Name);
        }

        public void Disable()
        {
            gameObject.SetActive(false);
        }

    }
}