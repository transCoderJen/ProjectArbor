using Febucci.TextAnimatorForUnity;
using ShiftedSignal.Garden.Dialogue;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;

namespace ShiftedSignal.Garden.UserInterface
{
    
    public class DialoguePanelUI : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private GameObject ContentParent;
        [SerializeField] private TypewriterComponent DialogueTypeWriter;
        [SerializeField] private TMP_Text DialogueText;
        [SerializeField] private Image SpeakerPortraitImage;
        [SerializeField] private TMP_Text SpeakerNameText;
        [SerializeField] private DialogueChoiceButton[] ChoiceButtons;

        [Header("Speaker Data")]
        [SerializeField] private DialogueSpeakerDatabase SpeakerDatabase;
        [SerializeField] private Sprite DefaultPortrait;

        [SerializeField] private float submitLockDuration = 0.15f;

        private bool canSubmitDialogue;

        private bool isTyping;
        private string currentDialogueLine = string.Empty;
        private DisplayDialogueEvent currentDisplayEvent;
        private bool hasPendingChoices;

        private void Awake()
        {
            ContentParent.SetActive(false);
            ResetPanel();
        }

        private void OnEnable()
        {
            Bus<DialogueStartedEvent>.OnEvent += DialogueStarted;
            Bus<DialogueFinishedEvent>.OnEvent += DialogueFinished;
            Bus<DisplayDialogueEvent>.OnEvent += DisplayDialogue;
            Bus<DialogueSubmitEvent>.OnEvent += HandleSubmit;

            DialogueTypeWriter.onTextShowed.AddListener(HandleTextFinished);
        }

        private void OnDisable()
        {
            Bus<DialogueStartedEvent>.OnEvent -= DialogueStarted;
            Bus<DialogueFinishedEvent>.OnEvent -= DialogueFinished;
            Bus<DisplayDialogueEvent>.OnEvent -= DisplayDialogue;
            Bus<DialogueSubmitEvent>.OnEvent -= HandleSubmit;

            DialogueTypeWriter.onTextShowed.RemoveListener(HandleTextFinished);
        }

        private void DialogueStarted(DialogueStartedEvent _)
        {
            ContentParent.SetActive(true);
            canSubmitDialogue = false;
            Invoke(nameof(UnlockDialogueSubmit), submitLockDuration);
        }

        private void UnlockDialogueSubmit()
        {
            canSubmitDialogue = true;
        }

        private void DialogueFinished(DialogueFinishedEvent _)
        {
            CancelInvoke(nameof(UnlockDialogueSubmit));
            canSubmitDialogue = false;

            ContentParent.SetActive(false);
            ResetPanel();
        }

        private void DisplayDialogue(DisplayDialogueEvent evt)
        {
            currentDisplayEvent = evt;
            hasPendingChoices = evt.DialogueChoices.Count > 0;

            currentDialogueLine = evt.DialogueLine;
            isTyping = true;

            UpdateSpeakerVisuals(evt.SpeakerID);

            HideChoiceButtons();

            DialogueTypeWriter.ShowText(currentDialogueLine);

            if (evt.DialogueChoices.Count > ChoiceButtons.Length)
            {
                Debug.LogError("More dialogue choices than buttons available");
            }
        }

        private void UpdateSpeakerVisuals(string speakerId)
        {
            if (SpeakerDatabase != null &&
                SpeakerDatabase.TryGetSpeaker(speakerId, out DialogueSpeakerData speakerData))
            {
                UpdatePortrait(speakerData.Portrait);
                UpdateSpeakerName(speakerData.DisplayName, speakerData.NameColor);
                return;
            }

            UpdatePortrait(DefaultPortrait);
            UpdateSpeakerName(string.Empty, Color.white);
        }

        private void UpdatePortrait(Sprite portrait)
        {
            if (SpeakerPortraitImage == null)
                return;

            SpeakerPortraitImage.sprite = portrait;
            SpeakerPortraitImage.enabled = portrait != null;
        }

        private void UpdateSpeakerName(string displayName, Color nameColor)
        {
            if (SpeakerNameText == null)
                return;

            SpeakerNameText.text = displayName;
            SpeakerNameText.color = nameColor;
            SpeakerNameText.gameObject.SetActive(!string.IsNullOrWhiteSpace(displayName));
        }

        private void HandleSubmit(DialogueSubmitEvent _)
        {
            if (!canSubmitDialogue)
                return;

            if (isTyping)
            {
                DialogueTypeWriter.SkipTypewriter();
                isTyping = false;
                ShowPendingChoices();
                return;
            }

            Bus<DialogueAdvanceRequestedEvent>.Raise(new DialogueAdvanceRequestedEvent());
        }

        private void HandleTextFinished()
        {
            isTyping = false;
            ShowPendingChoices();
        }

        private void ResetPanel()
        {
            currentDialogueLine = string.Empty;
            isTyping = false;

            DialogueTypeWriter.StopShowingText();
            DialogueText.text = string.Empty;

            UpdatePortrait(DefaultPortrait);
            UpdateSpeakerName(string.Empty, Color.white);
        }

        private void HideChoiceButtons()
        {
            foreach (DialogueChoiceButton choiceButton in ChoiceButtons)
            {
                choiceButton.gameObject.SetActive(false);
            }
        }

        private void ShowPendingChoices()
        {
            if (!hasPendingChoices)
                return;

            hasPendingChoices = false;

            int choiceButtonIndex = currentDisplayEvent.DialogueChoices.Count - 1;

            for (int inkChoiceIndex = 0; inkChoiceIndex < currentDisplayEvent.DialogueChoices.Count; inkChoiceIndex++)
            {
                Choice dialogueChoice = currentDisplayEvent.DialogueChoices[inkChoiceIndex];
                DialogueChoiceButton choiceButton = ChoiceButtons[choiceButtonIndex];

                choiceButton.gameObject.SetActive(true);
                choiceButton.SetChoiceIndex(inkChoiceIndex);
                choiceButton.SetChoiceText(dialogueChoice.text);

                if (inkChoiceIndex == 0)
                {
                    choiceButton.SelectButton();
                    Bus<UpdateDialogueChoiceIndexEvent>.Raise(new UpdateDialogueChoiceIndexEvent(0));
                }

                choiceButtonIndex--;
            }
        }
    }
}