using System.Collections;
using Febucci.TextAnimatorForUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.QuestSystem;

namespace ShiftedSignal.Garden.UserInterface
{
    public class QuestToastUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup CanvasGroup;
        [SerializeField] private RectTransform ToastRoot;
        [SerializeField] private TextMeshProUGUI HeaderText;
        [SerializeField] private TextMeshProUGUI BodyText;
        [SerializeField] private Image IconImage;

        [Header("Text Animator")]
        [SerializeField] private TypewriterComponent HeaderTypewriter;
        [SerializeField] private TypewriterComponent BodyTypewriter;

        [Header("Icons")]
        [SerializeField] private Sprite NewQuestIcon;
        [SerializeField] private Sprite ActiveQuestIcon;
        [SerializeField] private Sprite ObjectiveCompleteIcon;
        [SerializeField] private Sprite QuestCompleteIcon;

        [Header("Audio")]
        [SerializeField] private AudioSource AudioSource;
        [SerializeField] private AudioClip NewQuestClip;
        [SerializeField] private AudioClip ActiveQuestClip;
        [SerializeField] private AudioClip ObjectiveCompleteClip;
        [SerializeField] private AudioClip QuestCompleteClip;

        [Header("Animation")]
        [SerializeField] private float FadeInTime = 0.2f;
        [SerializeField] private float StayTime = 2f;
        [SerializeField] private float FadeOutTime = 0.35f;
        [SerializeField] private float PopScale = 1.12f;

        private Coroutine toastRoutine;
        private bool hasShownFirstQuestToast;

        private void Awake()
        {
            HideImmediate();
        }

        private void OnEnable()
        {
            Bus<QuestReceivedEvent>.OnEvent += HandleQuestReceived;
            Bus<TrackedQuestChangedEvent>.OnEvent += HandleTrackedQuestChanged;
            Bus<QuestStepAdvancedEvent>.OnEvent += HandleQuestStepAdvanced;
            Bus<QuestStateChangedEvent>.OnEvent += HandleQuestStateChanged;
        }

        private void OnDisable()
        {
            Bus<QuestReceivedEvent>.OnEvent -= HandleQuestReceived;
            Bus<TrackedQuestChangedEvent>.OnEvent -= HandleTrackedQuestChanged;
            Bus<QuestStepAdvancedEvent>.OnEvent -= HandleQuestStepAdvanced;
            Bus<QuestStateChangedEvent>.OnEvent -= HandleQuestStateChanged;
        }

        private void HandleQuestReceived(QuestReceivedEvent evt)
        {
            if (QuestManager.Instance == null)
                return;

            Quest quest = QuestManager.Instance.GetQuestById(evt.Id);

            if (quest == null)
                return;

            hasShownFirstQuestToast = true;

            ShowToast(
                "<bounce>New Quest</bounce>",
                quest.Info.DisplayName,
                GetQuestIcon(quest, NewQuestIcon),
                NewQuestClip);
        }

        private Sprite GetQuestIcon(Quest quest, Sprite fallbackIcon)
        {
            if (quest != null &&
                quest.Info != null &&
                quest.Info.QuestIcon != null)
            {
                return quest.Info.QuestIcon;
            }

            return fallbackIcon;
        }
        
        private void HandleTrackedQuestChanged(TrackedQuestChangedEvent evt)
        {
            if (evt.Quest == null)
                return;

            if (hasShownFirstQuestToast)
            {
                hasShownFirstQuestToast = false;
                return;
            }

            ShowToast(
                "<wave>Active Quest</wave>",
                evt.Quest.Info.DisplayName,
                GetQuestIcon(evt.Quest, ActiveQuestIcon),
                ActiveQuestClip);
        }

        private void HandleQuestStepAdvanced(QuestStepAdvancedEvent evt)
        {
            if (evt.Quest == null)
                return;

            if (evt.Quest.State == QuestState.FINISHED)
                return;

            ShowToast(
                "<bounce>Objective Complete</bounce>",
                evt.Quest.Info.DisplayName,
                GetQuestIcon(evt.Quest, ObjectiveCompleteIcon),
                ObjectiveCompleteClip);
        }

        private void HandleQuestStateChanged(QuestStateChangedEvent evt)
        {
            if (evt.Quest == null)
                return;

            if (evt.Quest.State != QuestState.FINISHED)
                return;

            ShowToast(
                "<bounce>Quest Complete</bounce>",
                evt.Quest.Info.DisplayName,
                GetQuestIcon(evt.Quest, QuestCompleteIcon),
                QuestCompleteClip);
        }

        private void ShowToast(
            string header,
            string body,
            Sprite icon,
            AudioClip clip)
        {
            if (toastRoutine != null)
                StopCoroutine(toastRoutine);

            toastRoutine = StartCoroutine(
                ToastRoutine(header, body, icon, clip));
        }

        private IEnumerator ToastRoutine(
            string header,
            string body,
            Sprite icon,
            AudioClip clip)
        {
            CanvasGroup.alpha = 0f;
            CanvasGroup.gameObject.SetActive(true);

            if (ToastRoot != null)
                ToastRoot.localScale = Vector3.one * PopScale;

            if (IconImage != null)
            {
                IconImage.sprite = icon;
                IconImage.enabled = icon != null;
            }

            if (HeaderText != null)
                HeaderText.text = header;

            if (BodyText != null)
                BodyText.text = body;

            if (HeaderTypewriter != null)
                HeaderTypewriter.ShowText(header);

            if (BodyTypewriter != null)
                BodyTypewriter.ShowText(body);

            PlaySound(clip);

            float timer = 0f;

            while (timer < FadeInTime)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / FadeInTime;

                CanvasGroup.alpha = t;

                if (ToastRoot != null)
                    ToastRoot.localScale = Vector3.Lerp(
                        Vector3.one * PopScale,
                        Vector3.one,
                        t);

                yield return null;
            }

            CanvasGroup.alpha = 1f;

            if (ToastRoot != null)
                ToastRoot.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(StayTime);

            timer = 0f;

            while (timer < FadeOutTime)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / FadeOutTime;

                CanvasGroup.alpha = 1f - t;

                yield return null;
            }

            HideImmediate();
            toastRoutine = null;
        }

        private void PlaySound(AudioClip clip)
        {
            if (AudioSource == null || clip == null)
                return;

            AudioSource.PlayOneShot(clip);
        }

        private void HideImmediate()
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 0f;
                CanvasGroup.gameObject.SetActive(false);
            }

            if (ToastRoot != null)
                ToastRoot.localScale = Vector3.one;
        }
    }
}