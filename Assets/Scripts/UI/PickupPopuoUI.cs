using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface
{
    public class PickupPopupUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup CanvasGroup;
        [SerializeField] private Image IconImage;
        [SerializeField] private TextMeshProUGUI AmountText;

        [Header("Feel")]
        [SerializeField] private MMF_Player PopupFeedback;
        [SerializeField] private bool DestroyAfterFeedback = true;

        public void Setup(Sprite icon, int amount)
        {
            if (amount > 1)
                Setup(icon, $"+{amount}");
            else
                Setup(icon, string.Empty);
        }

        public void Setup(Sprite icon, string text)
        {
            Debug.Log("Setting up popup");
            if (CanvasGroup != null)
                CanvasGroup.alpha = 1f;

            if (IconImage != null)
            {
                IconImage.sprite = icon;
                IconImage.enabled = icon != null;
            }

            if (AmountText != null)
            {
                bool showText = !string.IsNullOrWhiteSpace(text);

                AmountText.gameObject.SetActive(showText);
                AmountText.text = showText ? text : string.Empty;
            }

            PlayFeedback();
        }

        private void PlayFeedback()
        {
            if (PopupFeedback == null)
            {
                Debug.LogWarning($"{name} is missing a PopupFeedback MMF_Player.");
                Destroy(gameObject);
                return;
            }

            PopupFeedback.PlayFeedbacks();

            if (DestroyAfterFeedback)
                Destroy(gameObject, PopupFeedback.TotalDuration);
        }
    }
}