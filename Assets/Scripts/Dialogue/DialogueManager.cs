using System.Collections;
using Ink.Runtime;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Misc;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.Dialogue
{
    public class DialogueManager : Singleton<DialogueManager>
    {
        [Header("Ink Story")]
        [SerializeField] private TextAsset InkJson;

        private Story story;
        private int currentChoiceIndex = -1;
        private string currentSpeakerId = "default";
        private bool dialoguePlaying = false;
        private InkExternalFunctions inkExternalFunctions;
        private InkDialogueVariables inkDialogueVariables;

        override protected void Awake()
        {
            base.Awake();
            story = new Story(InkJson.text);
            inkExternalFunctions = new InkExternalFunctions();
            inkExternalFunctions.Bind(story);
            inkDialogueVariables = new InkDialogueVariables(story);
        }

        override protected void OnDestroy()
        {
            if (inkExternalFunctions != null && story != null)
                inkExternalFunctions.Unbind(story);       
        }

        private void OnEnable()
        {
            Bus<EnterDialogueEvent>.OnEvent += EnterDialogueHandler;
            Bus<DialogueAdvanceRequestedEvent>.OnEvent += HandleDialogueAdvanceRequested;
            Bus<UpdateDialogueChoiceIndexEvent>.OnEvent += UpdateChoiceIndex;
            Bus<UpdateInkDialogueVariableEvent>.OnEvent += UpdateInkDialogueVariable;
            Bus<QuestStateChangedEvent>.OnEvent += QuestStateChange;
        }  

        private void OnDisable()
        {
            Bus<EnterDialogueEvent>.OnEvent -= EnterDialogueHandler;
            Bus<DialogueAdvanceRequestedEvent>.OnEvent -= HandleDialogueAdvanceRequested;
            Bus<UpdateDialogueChoiceIndexEvent>.OnEvent -= UpdateChoiceIndex;
            Bus<UpdateInkDialogueVariableEvent>.OnEvent -= UpdateInkDialogueVariable;
            Bus<QuestStateChangedEvent>.OnEvent -= QuestStateChange;
        }

        private void QuestStateChange(QuestStateChangedEvent evt)
        {
            Bus<UpdateInkDialogueVariableEvent>.Raise(
                new UpdateInkDialogueVariableEvent(evt.Quest.Info.ID + "State", new StringValue(evt.Quest.State.ToString()))
            );
        }

        private void UpdateInkDialogueVariable(UpdateInkDialogueVariableEvent evt)
        {
            inkDialogueVariables.UpdateVariableState(evt.Name, evt.Value);
        }

        private void UpdateChoiceIndex(UpdateDialogueChoiceIndexEvent evt)
        {
            this.currentChoiceIndex = evt.ChoiceIndex;
        }

        private void Update()
        {
            if (!dialoguePlaying)
                return;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                SubmitPressed();
        }

        public void OnInteract(InputValue value)
        {
            if (!dialoguePlaying)
                return;

            SubmitPressed();
        }

        private void SubmitPressed()
        {
            Bus<DialogueSubmitEvent>.Raise(new DialogueSubmitEvent());
        }

        private void HandleDialogueAdvanceRequested(DialogueAdvanceRequestedEvent evt)
        {
            if (!dialoguePlaying)
                return;

            ContinueOrExitStory();
        }

        private void EnterDialogueHandler(EnterDialogueEvent evt)
        {
            if (dialoguePlaying)
                return;

            dialoguePlaying = true;

            Bus<DialogueStartedEvent>.Raise(new DialogueStartedEvent());
            Bus<EnablePlayerMovementEvent>.Raise(new EnablePlayerMovementEvent(false));

            if (!string.IsNullOrWhiteSpace(evt.KnotName))
            {
                story.ChoosePathString(evt.KnotName);
            }
            else
            {
                Debug.LogWarning("Knot name was empty when entering dialogue.");
            }

            inkDialogueVariables.SyncVariablesAndStartListening(story);

            ContinueOrExitStory();
        }

        private void ContinueOrExitStory()
        {
            if (story.currentChoices.Count > 0 && currentChoiceIndex != -1)
            {
                story.ChooseChoiceIndex(currentChoiceIndex);
                currentChoiceIndex = -1;
            }

            if (story.canContinue)
            {
                string dialogueLine = story.Continue();
                string speakerId = GetSpeakerId();

                while (IsLineBlank(dialogueLine) && story.canContinue)
                {
                    dialogueLine = story.Continue();
                }
                if (IsLineBlank(dialogueLine) && !story.canContinue)
                {
                    StartCoroutine(ExitDialogue());
                }
                else
                {
                    Bus<DisplayDialogueEvent>.Raise(
                        new DisplayDialogueEvent(CheckIfNote(), speakerId, dialogueLine, story.currentChoices)
                    );
                }

                
            }
            else if (story.currentChoices.Count == 0)
            {
                StartCoroutine(ExitDialogue());
            }
        }

        private bool CheckIfNote()
        {
            foreach (string tag in story.currentTags)
            {
                if (tag.Equals("note"))
                    return true;
            }

            return false;
        }

        private string GetSpeakerId()
        {
            foreach (string tag in story.currentTags)
            {
                string[] splitTag = tag.Split(':');

                if (splitTag.Length != 2)
                    continue;

                string tagName = splitTag[0].Trim();
                string tagValue = splitTag[1].Trim();

                if (tagName.Equals("speaker"))
                {
                    currentSpeakerId = tagValue;
                    return currentSpeakerId;
                }
            }

            return currentSpeakerId;
        }

        private IEnumerator ExitDialogue()
        {
            yield return null;

            dialoguePlaying = false;

            Bus<DialogueFinishedEvent>.Raise(new DialogueFinishedEvent());
            Bus<EnablePlayerMovementEvent>.Raise(new EnablePlayerMovementEvent(true));
            inkDialogueVariables.StopListening(story);
            story.ResetState();
        }

        private bool IsLineBlank(string dialogueLine)
        {
            return dialogueLine.Trim().Equals("") || dialogueLine.Trim().Equals("/n");
        }
    }
}