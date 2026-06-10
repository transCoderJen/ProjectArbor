using System.Collections.Generic;
using ShiftedSignal.Garden.Misc;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface
{
    public class PickupPopupManager : Singleton<PickupPopupManager>
    {
        [SerializeField] private PickupPopupUI PickupPopupPrefab;
        [SerializeField] private RectTransform PopupParent;
        
        private readonly Dictionary<string, PickupPopupUI> activePopups = new();

        public void Show(Sprite icon, int amount, string itemName)
        {
            if (activePopups.TryGetValue(itemName, out PickupPopupUI popup) && popup != null)
            {
                popup.AddAmount(amount);
                return;
            }
            
            popup = Instantiate(PickupPopupPrefab, PopupParent);
            popup.transform.localScale = Vector3.one;
            popup.Setup(icon, amount, itemName);

            activePopups[itemName] = popup;
        }

        public void RemovePopup(string itemName)
        {
            activePopups.Remove(itemName);
        }
    }
}