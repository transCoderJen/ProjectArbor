using System.Collections;
using Ink.Runtime;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Misc;
using UnityEngine;
using ShiftedSignal.Garden.Units;

namespace ShiftedSignal.Garden.Dialogue
{
    public class DialogueManager : Singleton<DialogueManager>
    {
        [Header("Ink Story")]
        [SerializeField] private TextAsset InkJson;

        [Header("Input")]
        [SerializeField] private PlayerInputReader inputReader;

        private Story story;
        private int currentChoiceIndex = -1;
        private string currentSpeakerId = "default";
        private bool dialoguePlaying;

        private InkExternalFunctions inkExternalFunctions;
        private InkDialogueVariables inkDialogueVariables;

        private bool inputSubscribed;

        


        protected override void Awake()
        {
            base.Awake();

            story = new Story(InkJson.text);

            inkExternalFunctions = new InkExternalFunctions();
            inkExternalFunctions.Bind(story);

            inkDialogueVariables = new InkDialogueVariables(story);
        }

        private void Start()
        {
            FindInputReader();
            SubscribeToInput();
        }

        private void OnEnable()
        {
            Bus<EnterDialogueEvent>.OnEvent += EnterDialogueHandler;
            Bus<DialogueAdvanceRequestedEvent>.OnEvent += HandleDialogueAdvanceRequested;
            Bus<UpdateDialogueChoiceIndexEvent>.OnEvent += UpdateChoiceIndex;
            Bus<UpdateInkDialogueVariableEvent>.OnEvent += UpdateInkDialogueVariable;
            Bus<QuestStateChangedEvent>.OnEvent += QuestStateChange;

            FindInputReader();
            SubscribeToInput();
        }

        private void OnDisable()
        {
            Bus<EnterDialogueEvent>.OnEvent -= EnterDialogueHandler;
            Bus<DialogueAdvanceRequestedEvent>.OnEvent -= HandleDialogueAdvanceRequested;
            Bus<UpdateDialogueChoiceIndexEvent>.OnEvent -= UpdateChoiceIndex;
            Bus<UpdateInkDialogueVariableEvent>.OnEvent -= UpdateInkDialogueVariable;
            Bus<QuestStateChangedEvent>.OnEvent -= QuestStateChange;

            UnsubscribeFromInput();
        }

        protected override void OnDestroy()
        {
            UnsubscribeFromInput();

            if (inkExternalFunctions != null && story != null)
                inkExternalFunctions.Unbind(story);

            base.OnDestroy();
        }

    
        #region Input

        private void FindInputReader()
        {
            if (inputReader != null)
                return;

            if (Player.Instance != null)
                inputReader = Player.Instance.InputReader;
        }

        private void SubscribeToInput()
        {
            if (inputSubscribed || inputReader == null)
                return;

            inputReader.InteractPressed += HandleSubmitPressed;
            inputSubscribed = true;
        }

        private void UnsubscribeFromInput()
        {
            if (!inputSubscribed || inputReader == null)
                return;

            inputReader.InteractPressed-= HandleSubmitPressed;
            inputSubscribed = false;
        }

        private void HandleSubmitPressed()
        {
            if (!dialoguePlaying)
                return;

            Bus<DialogueSubmitEvent>.Raise(new DialogueSubmitEvent());
        }

        #endregion

        #region Event Handlers

        private void QuestStateChange(QuestStateChangedEvent evt)
        {
            Bus<UpdateInkDialogueVariableEvent>.Raise(
                new UpdateInkDialogueVariableEvent(
                    evt.Quest.Info.ID + "State",
                    new StringValue(evt.Quest.State.ToString())));
        }

        private void UpdateInkDialogueVariable(UpdateInkDialogueVariableEvent evt)
        {
            inkDialogueVariables.UpdateVariableState(evt.Name, evt.Value);
        }

        private void UpdateChoiceIndex(UpdateDialogueChoiceIndexEvent evt)
        {
            currentChoiceIndex = evt.ChoiceIndex;
        }

        private void HandleDialogueAdvanceRequested(
            DialogueAdvanceRequestedEvent evt)
        {
            if (!dialoguePlaying)
                return;

            ContinueOrExitStory();
        }

        #endregion

        #region Dialogue

        private void EnterDialogueHandler(EnterDialogueEvent evt)
        {
            if (dialoguePlaying)
                return;

            dialoguePlaying = true;

            Bus<DialogueStartedEvent>.Raise(new DialogueStartedEvent());
            Bus<EnablePlayerMovementEvent>.Raise(
                new EnablePlayerMovementEvent(false));

            if (!string.IsNullOrWhiteSpace(evt.KnotName))
            {
                story.ChoosePathString(evt.KnotName);
            }
            else
            {
                Debug.LogWarning(
                    "Knot name was empty when entering dialogue.");
            }

            inkDialogueVariables.SyncVariablesAndStartListening(story);

            ContinueOrExitStory();
        }

        private void ContinueOrExitStory()
        {
            if (story.currentChoices.Count > 0 &&
                currentChoiceIndex != -1)
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
                    return;
                }

                Bus<DisplayDialogueEvent>.Raise(
                    new DisplayDialogueEvent(
                        CheckIfNote(),
                        speakerId,
                        dialogueLine,
                        story.currentChoices));

                return;
            }

            if (story.currentChoices.Count == 0)
                StartCoroutine(ExitDialogue());
        }

        private IEnumerator ExitDialogue()
        {
            yield return null;

            dialoguePlaying = false;

            Bus<DialogueFinishedEvent>.Raise(
                new DialogueFinishedEvent());

            Bus<EnablePlayerMovementEvent>.Raise(
                new EnablePlayerMovementEvent(true));

            inkDialogueVariables.StopListening(story);

            inkExternalFunctions.SetCommandTargetToNull();
            
            story.ResetState();
        }

        #endregion

        #region Ink Helpers

        public void SetCommandTarget(Worker worker)
        {
            inkExternalFunctions.SetCommandTarget(worker);
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

        private bool IsLineBlank(string dialogueLine)
        {
            return string.IsNullOrWhiteSpace(dialogueLine) ||
                   dialogueLine.Trim().Equals("/n");
        }

        #endregion
    }
}