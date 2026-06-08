using ShiftedSignal.Garden.Misc;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface
{
    public class PickupPopupManager : Singleton<PickupPopupManager>
    {
        [Header("Popup")]
        [SerializeField] private PickupPopupUI PopupPrefab;

        [Header("Defaults")]
        [SerializeField] private Vector3 DefaultOffset = new Vector3(0f, 0.75f, 0f);

        public void Show(Vector3 worldPosition, Sprite icon, int amount)
        {
            if (PopupPrefab == null)
            {
                Debug.LogWarning("PickupPopupManager is missing a PopupPrefab.");
                return;
            }

            PickupPopupUI popup = Instantiate(
                PopupPrefab,
                worldPosition + DefaultOffset,
                Quaternion.identity);

            popup.Setup(icon, amount);
        }

        public void Show(Vector3 worldPosition, Sprite icon, string text)
        {
            if (PopupPrefab == null)
            {
                Debug.LogWarning("PickupPopupManager is missing a PopupPrefab.");
                return;
            }

            PickupPopupUI popup = Instantiate(
                PopupPrefab,
                worldPosition + DefaultOffset,
                Quaternion.identity);

            popup.Setup(icon, text);
        }
    }
}