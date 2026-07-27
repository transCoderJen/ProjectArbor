using System.Collections;
using Ink.Runtime;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.Units;
using UnityEngine;

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
        private bool waitingForConstructionMenu;
        private bool inputSubscribed;

        private InkExternalFunctions inkExternalFunctions;
        private InkDialogueVariables inkDialogueVariables;

        protected override void Awake()
        {
            base.Awake();

            if (InkJson == null)
            {
                Debug.LogError(
                    $"{nameof(DialogueManager)} has no Ink JSON assigned.",
                    this);

                return;
            }

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
            Bus<EnterDialogueEvent>.OnEvent +=
                EnterDialogueHandler;

            Bus<DialogueAdvanceRequestedEvent>.OnEvent +=
                HandleDialogueAdvanceRequested;

            Bus<UpdateDialogueChoiceIndexEvent>.OnEvent +=
                UpdateChoiceIndex;

            Bus<UpdateInkDialogueVariableEvent>.OnEvent +=
                UpdateInkDialogueVariable;

            Bus<QuestStateChangedEvent>.OnEvent +=
                QuestStateChange;

            Bus<ConstructionMenuClosedEvent>.OnEvent +=
                HandleConstructionMenuClosed;

            FindInputReader();
            SubscribeToInput();
        }

        private void OnDisable()
        {
            Bus<EnterDialogueEvent>.OnEvent -=
                EnterDialogueHandler;

            Bus<DialogueAdvanceRequestedEvent>.OnEvent -=
                HandleDialogueAdvanceRequested;

            Bus<UpdateDialogueChoiceIndexEvent>.OnEvent -=
                UpdateChoiceIndex;

            Bus<UpdateInkDialogueVariableEvent>.OnEvent -=
                UpdateInkDialogueVariable;

            Bus<QuestStateChangedEvent>.OnEvent -=
                QuestStateChange;

            Bus<ConstructionMenuClosedEvent>.OnEvent -=
                HandleConstructionMenuClosed;

            UnsubscribeFromInput();
        }

        protected override void OnDestroy()
        {
            UnsubscribeFromInput();

            if (inkExternalFunctions != null &&
                story != null)
            {
                inkExternalFunctions.Unbind(story);
            }

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
            if (inputSubscribed ||
                inputReader == null)
            {
                return;
            }

            inputReader.InteractPressed +=
                HandleSubmitPressed;

            inputSubscribed = true;
        }

        private void UnsubscribeFromInput()
        {
            if (!inputSubscribed ||
                inputReader == null)
            {
                return;
            }

            inputReader.InteractPressed -=
                HandleSubmitPressed;

            inputSubscribed = false;
        }

        private void HandleSubmitPressed()
        {
            if (!dialoguePlaying ||
                waitingForConstructionMenu)
            {
                return;
            }

            Bus<DialogueSubmitEvent>.Raise(
                new DialogueSubmitEvent());
        }

        #endregion

        #region Event Handlers

        private void HandleConstructionMenuClosed(
            ConstructionMenuClosedEvent evt)
        {
            Debug.Log(
                "[DialogueManager] HandleConstructionMenuClosed\n" +
                $"  dialoguePlaying: {dialoguePlaying}\n" +
                $"  waitingForConstructionMenu: {waitingForConstructionMenu}\n" +
                $"  selectedBuilding: {(evt.SelectedBuilding != null ? evt.SelectedBuilding.ItemID : "<null>")}\n" +
                $"  currentKnot: {story.state.currentPathString}\n" +
                $"  frame: {Time.frameCount}");

            if (!dialoguePlaying ||
                !waitingForConstructionMenu)
            {
                Debug.Log(
                    "[DialogueManager] Ignoring ConstructionMenuClosedEvent " +
                    "(dialogue is not waiting for the construction menu).");

                return;
            }

            waitingForConstructionMenu = false;

            string selectedBuildingId =
                evt.SelectedBuilding != null
                    ? evt.SelectedBuilding.ItemID
                    : string.Empty;

            Debug.Log(
                $"[DialogueManager] Setting Ink variable " +
                $"selected_building = '{selectedBuildingId}'");

            story.variablesState["selected_building"] =
                selectedBuildingId;

            Debug.Log(
                "[DialogueManager] Showing dialogue UI and scheduling resume next frame.");

            Bus<SetDialogueVisibilityEvent>.Raise(
                new SetDialogueVisibilityEvent(true));

            StartCoroutine(
                ResumeDialogueNextFrame());
        }

        private void QuestStateChange(
            QuestStateChangedEvent evt)
        {
            Bus<UpdateInkDialogueVariableEvent>.Raise(
                new UpdateInkDialogueVariableEvent(
                    evt.Quest.Info.ID + "State",
                    new StringValue(
                        evt.Quest.State.ToString())));
        }

        private void UpdateInkDialogueVariable(
            UpdateInkDialogueVariableEvent evt)
        {
            inkDialogueVariables.UpdateVariableState(
                evt.Name,
                evt.Value);
        }

        private void UpdateChoiceIndex(
            UpdateDialogueChoiceIndexEvent evt)
        {
            currentChoiceIndex = evt.ChoiceIndex;
        }

        private void HandleDialogueAdvanceRequested(
            DialogueAdvanceRequestedEvent evt)
        {
            if (!dialoguePlaying ||
                waitingForConstructionMenu)
            {
                return;
            }

            ContinueOrExitStory();
        }

        #endregion

        #region Dialogue

        private void EnterDialogueHandler(
            EnterDialogueEvent evt)
        {
            if (dialoguePlaying ||
                story == null)
            {
                return;
            }

            dialoguePlaying = true;
            waitingForConstructionMenu = false;
            currentChoiceIndex = -1;
            currentSpeakerId = "default";

            Bus<DialogueStartedEvent>.Raise(
                new DialogueStartedEvent());

            Bus<EnablePlayerMovementEvent>.Raise(
                new EnablePlayerMovementEvent(false));

            if (!string.IsNullOrWhiteSpace(
                    evt.KnotName))
            {
                story.ChoosePathString(
                    evt.KnotName);
            }
            else
            {
                Debug.LogWarning(
                    "Knot name was empty when entering dialogue.",
                    this);
            }

            inkDialogueVariables
                .SyncVariablesAndStartListening(story);

            ContinueOrExitStory();
        }

        public void WaitForConstructionMenu()
        {
            waitingForConstructionMenu = true;

            Bus<SetDialogueVisibilityEvent>.Raise(
                new SetDialogueVisibilityEvent(false));

            Bus<EnablePlayerMovementEvent>.Raise(
                new EnablePlayerMovementEvent(true));

            Bus<OpenConstructionMenuEvent>.Raise(
                new OpenConstructionMenuEvent());
        }

        private void ContinueOrExitStory()
{
    Debug.Log(
        "[DialogueManager] ContinueOrExitStory ENTER\n" +
        $"  dialoguePlaying: {dialoguePlaying}\n" +
        $"  waitingForConstructionMenu: {waitingForConstructionMenu}\n" +
        $"  story null: {story == null}\n" +
        $"  currentChoiceIndex: {currentChoiceIndex}\n" +
        $"  currentChoices: {(story != null ? story.currentChoices.Count : -1)}\n" +
        $"  canContinue: {(story != null && story.canContinue)}\n" +
        $"  currentPath: {(story != null ? story.state.currentPathString : "<null>")}\n" +
        $"  selected_building: {(story != null ? story.variablesState["selected_building"] : "<null>")}\n" +
        $"  frame: {Time.frameCount}");

    if (!dialoguePlaying ||
        waitingForConstructionMenu ||
        story == null)
    {
        Debug.LogWarning(
            "[DialogueManager] ContinueOrExitStory stopped by guard.");

        return;
    }

    if (story.currentChoices.Count > 0 &&
        currentChoiceIndex != -1)
    {
        Debug.Log(
            $"[DialogueManager] Choosing choice index {currentChoiceIndex}.");

        story.ChooseChoiceIndex(
            currentChoiceIndex);

        currentChoiceIndex = -1;

        Debug.Log(
            "[DialogueManager] Choice selected\n" +
            $"  canContinue: {story.canContinue}\n" +
            $"  currentChoices: {story.currentChoices.Count}\n" +
            $"  currentPath: {story.state.currentPathString}\n" +
            $"  selected_building: {story.variablesState["selected_building"]}");
    }

    if (story.canContinue)
    {
        Debug.Log(
            "[DialogueManager] Calling story.Continue().");

        string dialogueLine =
            story.Continue();

        Debug.Log(
            "[DialogueManager] story.Continue() completed\n" +
            $"  dialogueLine: '{dialogueLine}'\n" +
            $"  waitingForConstructionMenu: {waitingForConstructionMenu}\n" +
            $"  canContinue: {story.canContinue}\n" +
            $"  currentChoices: {story.currentChoices.Count}\n" +
            $"  currentPath: {story.state.currentPathString}\n" +
            $"  selected_building: {story.variablesState["selected_building"]}");

        /*
         * An external Ink function can set
         * waitingForConstructionMenu during
         * story.Continue().
         */
        if (waitingForConstructionMenu)
        {
            Debug.Log(
                "[DialogueManager] Returning because Ink opened the construction menu.");

            return;
        }

        while (IsLineBlank(dialogueLine) &&
               story.canContinue)
        {
            Debug.Log(
                "[DialogueManager] Blank line found. Continuing Ink again.");

            dialogueLine =
                story.Continue();

            Debug.Log(
                "[DialogueManager] Blank-line Continue completed\n" +
                $"  dialogueLine: '{dialogueLine}'\n" +
                $"  waitingForConstructionMenu: {waitingForConstructionMenu}\n" +
                $"  canContinue: {story.canContinue}\n" +
                $"  currentChoices: {story.currentChoices.Count}\n" +
                $"  currentPath: {story.state.currentPathString}");

            if (waitingForConstructionMenu)
            {
                Debug.Log(
                    "[DialogueManager] Returning because Ink opened the construction menu during blank-line processing.");

                return;
            }
        }

        if (IsLineBlank(dialogueLine) &&
            !story.canContinue)
        {
            Debug.LogWarning(
                "[DialogueManager] Ink returned a blank line and cannot continue.");

            if (story.currentChoices.Count > 0)
            {
                Debug.Log(
                    "[DialogueManager] Displaying remaining choices.");

                DisplayCurrentChoicesOnly();
                return;
            }

            Debug.LogWarning(
                "[DialogueManager] No choices remain. Starting ExitDialogue.");

            StartCoroutine(
                ExitDialogue());

            return;
        }

        string speakerId =
            GetSpeakerId();

        Debug.Log(
            "[DialogueManager] Raising DisplayDialogueEvent\n" +
            $"  speakerId: '{speakerId}'\n" +
            $"  dialogueLine: '{dialogueLine}'\n" +
            $"  choices: {story.currentChoices.Count}");

        Bus<DisplayDialogueEvent>.Raise(
            new DisplayDialogueEvent(
                CheckIfNote(),
                speakerId,
                dialogueLine,
                story.currentChoices));

        return;
    }

    if (story.currentChoices.Count > 0)
    {
        Debug.Log(
            "[DialogueManager] Ink cannot continue, but choices exist. Displaying choices.");

        DisplayCurrentChoicesOnly();
        return;
    }

    Debug.LogWarning(
        "[DialogueManager] Ink cannot continue and has no choices. Starting ExitDialogue.");

    StartCoroutine(
        ExitDialogue());
}

        private void DisplayCurrentChoicesOnly()
        {
            Bus<DisplayDialogueEvent>.Raise(
                new DisplayDialogueEvent(
                    CheckIfNote(),
                    GetSpeakerId(),
                    string.Empty,
                    story.currentChoices));
        }

        private IEnumerator ResumeDialogueNextFrame()
        {
            Debug.Log(
                $"[DialogueManager] ResumeDialogueNextFrame() START\n" +
                $"frame: {Time.frameCount}\n" +
                $"dialoguePlaying: {dialoguePlaying}\n" +
                $"waitingForConstructionMenu: {waitingForConstructionMenu}\n" +
                $"selected_building: '{story.variablesState["selected_building"]}'");

            yield return null;

            Debug.Log(
                $"[DialogueManager] ResumeDialogueNextFrame() AFTER YIELD\n" +
                $"frame: {Time.frameCount}\n" +
                $"dialoguePlaying: {dialoguePlaying}\n" +
                $"waitingForConstructionMenu: {waitingForConstructionMenu}\n" +
                $"story.canContinue: {story.canContinue}\n" +
                $"currentPath: {story.state.currentPathString}\n" +
                $"selected_building: '{story.variablesState["selected_building"]}'");

            if (!dialoguePlaying ||
                waitingForConstructionMenu)
            {
                Debug.LogWarning(
                    "[DialogueManager] Resume aborted before ContinueOrExitStory.");

                yield break;
            }

            Debug.Log(
                "[DialogueManager] Calling ContinueOrExitStory.");

            ContinueOrExitStory();
        }

        private IEnumerator ExitDialogue()
        {
            yield return null;

            /*
             * Another flow may have paused the story
             * before this coroutine executes.
             */
            if (waitingForConstructionMenu)
                yield break;

            dialoguePlaying = false;
            waitingForConstructionMenu = false;
            currentChoiceIndex = -1;
            currentSpeakerId = "default";

            Bus<SetDialogueVisibilityEvent>.Raise(
                new SetDialogueVisibilityEvent(false));

            Bus<DialogueFinishedEvent>.Raise(
                new DialogueFinishedEvent());

            Bus<EnablePlayerMovementEvent>.Raise(
                new EnablePlayerMovementEvent(true));

            inkDialogueVariables.StopListening(
                story);

            inkExternalFunctions
                .SetCommandTargetToNull();

            story.ResetState();
        }

        #endregion

        #region Ink Helpers

        public void SetCommandTarget(
            Worker worker)
        {
            inkExternalFunctions.SetCommandTarget(
                worker);
        }

        private bool CheckIfNote()
        {
            foreach (string tag in
                     story.currentTags)
            {
                if (tag.Equals("note"))
                    return true;
            }

            return false;
        }

        private string GetSpeakerId()
        {
            foreach (string tag in
                     story.currentTags)
            {
                string[] splitTag =
                    tag.Split(':');

                if (splitTag.Length != 2)
                    continue;

                string tagName =
                    splitTag[0].Trim();

                string tagValue =
                    splitTag[1].Trim();

                if (!tagName.Equals("speaker"))
                    continue;

                currentSpeakerId =
                    tagValue;

                return currentSpeakerId;
            }

            return currentSpeakerId;
        }

        private bool IsLineBlank(
            string dialogueLine)
        {
            return
                string.IsNullOrWhiteSpace(
                    dialogueLine) ||
                dialogueLine.Trim()
                    .Equals("/n");
        }

        #endregion
    }
}