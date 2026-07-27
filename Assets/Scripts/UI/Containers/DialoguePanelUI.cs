using Febucci.TextAnimatorForUnity;
using ShiftedSignal.Garden.Dialogue;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;
using ShiftedSignal.Garden.UserInterface.Components;

namespace ShiftedSignal.Garden.UserInterface.Containers
{
    
    public class DialoguePanelUI : MonoBehaviour
    {
        [Header("Dialogue Components")]
        [SerializeField] private GameObject DialogueContentParent;
        [SerializeField] private TypewriterComponent DialogueTypeWriter;
        [SerializeField] private TMP_Text DialogueText;
        [SerializeField] private Image SpeakerPortraitImage;
        [SerializeField] private TMP_Text SpeakerNameText;
        [SerializeField] private DialogueChoiceButton[] ChoiceButtons;

        [Header("Note Components")]
        [SerializeField] private GameObject NoteContentParent;
        [SerializeField] private TypewriterComponent NoteTypeWriter;
        [SerializeField] private TMP_Text NoteText;

        [Header("Speaker Data")]
        [SerializeField] private DialogueSpeakerDatabase SpeakerDatabase;
        [SerializeField] private Sprite DefaultPortrait;

        [SerializeField] private float submitLockDuration = 0.15f;

        private bool canSubmitDialogue;

        private bool isTyping;
        private string currentDialogueLine = string.Empty;
        private DisplayDialogueEvent currentDisplayEvent;
        private bool hasPendingChoices;
        private TypewriterComponent currentTypeWriter;
        private string currentNoteText = string.Empty;

        private void Awake()
        {
            DialogueContentParent.SetActive(false);
            NoteContentParent.SetActive(false);
            ResetPanel();
        }

        private void OnEnable()
        {
            Bus<DialogueStartedEvent>.OnEvent += DialogueStarted;
            Bus<DialogueFinishedEvent>.OnEvent += DialogueFinished;
            Bus<DisplayDialogueEvent>.OnEvent += DisplayDialogue;
            Bus<DialogueSubmitEvent>.OnEvent += HandleSubmit;
            Bus<SetDialogueVisibilityEvent>.OnEvent += HandleDialogueVisibility;

            DialogueTypeWriter.onTextShowed.AddListener(HandleTextFinished);
            NoteTypeWriter.onTextShowed.AddListener(HandleTextFinished);
        }

        private void OnDisable()
        {
            Bus<DialogueStartedEvent>.OnEvent -= DialogueStarted;
            Bus<DialogueFinishedEvent>.OnEvent -= DialogueFinished;
            Bus<DisplayDialogueEvent>.OnEvent -= DisplayDialogue;
            Bus<DialogueSubmitEvent>.OnEvent -= HandleSubmit;
            Bus<SetDialogueVisibilityEvent>.OnEvent -= HandleDialogueVisibility;

            DialogueTypeWriter.onTextShowed.RemoveListener(HandleTextFinished);
            NoteTypeWriter.onTextShowed.RemoveListener(HandleTextFinished);
        }

        private void DialogueStarted(DialogueStartedEvent _)
        {
            DialogueContentParent.SetActive(true);
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

            DialogueContentParent.SetActive(false);
            ResetPanel();
        }

        private void DisplayDialogue(DisplayDialogueEvent evt)
        {
            currentDisplayEvent = evt;
            hasPendingChoices = evt.DialogueChoices.Count > 0;

            currentDialogueLine = evt.DialogueLine;
            isTyping = true;

            HideChoiceButtons();

            DialogueContentParent.SetActive(!evt.IsNote);
            NoteContentParent.SetActive(evt.IsNote);

            currentTypeWriter = evt.IsNote ? NoteTypeWriter : DialogueTypeWriter;

            if (evt.IsNote)
            {
                DialogueContentParent.SetActive(false);
                NoteContentParent.SetActive(true);

                if (!string.IsNullOrWhiteSpace(currentNoteText))
                    currentNoteText += "\n\n";

                currentNoteText += currentDialogueLine;

                NoteTypeWriter.ShowText(currentNoteText);
                currentTypeWriter = NoteTypeWriter;
            }
            else
            {
                DialogueContentParent.SetActive(true);
                NoteContentParent.SetActive(false);

                DialogueText.text = string.Empty;
                UpdateSpeakerVisuals(evt.SpeakerID);

                DialogueTypeWriter.ShowText(currentDialogueLine);
                currentTypeWriter = DialogueTypeWriter;
            }

            if (evt.DialogueChoices.Count > ChoiceButtons.Length)
                Debug.LogError("More dialogue choices than buttons available");
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
                currentTypeWriter.SkipTypewriter();
                isTyping = false;
                ShowPendingChoices();

                LockSubmitBriefly();
                return;
            }

            Bus<DialogueAdvanceRequestedEvent>.Raise(new DialogueAdvanceRequestedEvent());

            LockSubmitBriefly();
        }

        private void LockSubmitBriefly()
        {
            canSubmitDialogue = false;
            CancelInvoke(nameof(UnlockDialogueSubmit));
            Invoke(nameof(UnlockDialogueSubmit), submitLockDuration);
        }
        
        private void HandleTextFinished()
        {
            isTyping = false;
            ShowPendingChoices();
        }

        private void ResetPanel()
        {
            currentDialogueLine = string.Empty;
            currentNoteText = string.Empty;
            isTyping = false;

            DialogueTypeWriter.StopShowingText();
            NoteTypeWriter.StopShowingText();

            DialogueText.text = string.Empty;
            NoteText.text = string.Empty;

            DialogueContentParent.SetActive(false);
            NoteContentParent.SetActive(false);

            UpdatePortrait(DefaultPortrait);
            UpdateSpeakerName(string.Empty, Color.white);

            currentTypeWriter = DialogueTypeWriter;
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

        private void HandleDialogueVisibility(SetDialogueVisibilityEvent evt)
        {
            if (!evt.IsVisible)
            {
                DialogueContentParent.SetActive(false);
                NoteContentParent.SetActive(false);

                canSubmitDialogue = false;
                return;
            }

            if (currentDisplayEvent.IsNote)
            {
                DialogueContentParent.SetActive(false);
                NoteContentParent.SetActive(true);
            }
            else
            {
                DialogueContentParent.SetActive(true);
                NoteContentParent.SetActive(false);
            }

            LockSubmitBriefly();
        }
    }
}