using System.Collections;
using MoreMountains.Feedbacks;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.UserInterface.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface.Containers
{
    public class PickupPopupUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup CanvasGroup;
        [SerializeField] private Image IconImage;
        [SerializeField] private TextMeshProUGUI AmountText;
        [SerializeField] private TextMeshProUGUI ItemNameText;

        [Header("Feedbacks")]
        [SerializeField] private float StartingDelay = 0.3f;
        [SerializeField] private float MinimumDelay = 0.1f;
        [SerializeField] private MMF_Player CountUpFeedback;
        [SerializeField] private MMF_Player FadeOutFeedback;

        private int currentAmount;
        private int targetAmount;
        private Coroutine countRoutine;
        private Coroutine destroyRoutine;

        public string ItemName { get; private set; }

        public void Setup(Sprite icon, int amount, string itemName)
        {
            ItemName = itemName;
            currentAmount = 0;

            if (CanvasGroup != null)
                CanvasGroup.alpha = 1f;

            if (IconImage != null)
            {
                IconImage.sprite = icon;
                IconImage.enabled = icon != null;
            }

            if (ItemNameText != null)
                ItemNameText.text = itemName;

            AddAmount(amount);
        }

        public void AddAmount(int amount)
        {
            ResetFade();

            targetAmount += amount;

            if (countRoutine == null)
                countRoutine = StartCoroutine(CountUpRoutine());

            RestartDestroyTimer();
        }

        private void ResetFade()
        {
            if (destroyRoutine != null)
            {
                StopCoroutine(destroyRoutine);
                destroyRoutine = null;
            }

            if (FadeOutFeedback != null)
                FadeOutFeedback.StopFeedbacks();

            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 1f;
                CanvasGroup.interactable = true;
                CanvasGroup.blocksRaycasts = true;
            }
        }

        private IEnumerator CountUpRoutine()
        {
            while (currentAmount < targetAmount)
            {
                currentAmount++;

                if (AmountText != null)
                    AmountText.text = currentAmount.ToString();

                CountUpFeedback?.PlayFeedbacks();

                int remaining = targetAmount - currentAmount;

                float t = 1f;

                if (targetAmount > 1)
                    t = Mathf.Clamp01((float)currentAmount / targetAmount);

                float delay = Mathf.Lerp(
                    StartingDelay,
                    MinimumDelay,
                    t);

                yield return Helpers.GetWait(delay);
            }

            countRoutine = null;
        }

        private void RestartDestroyTimer()
        {
            if (destroyRoutine != null)
                StopCoroutine(destroyRoutine);

            destroyRoutine = StartCoroutine(DestroyAfterFade());
        }

        private IEnumerator DestroyAfterFade()
        {
            yield return new WaitForSeconds(1.5f);

            if (FadeOutFeedback != null)
            {
                FadeOutFeedback.PlayFeedbacks();
                yield return new WaitForSeconds(FadeOutFeedback.TotalDuration);
            }

            PickupPopupManager.Instance.RemovePopup(ItemName);

            Destroy(gameObject);
        }
    }
}