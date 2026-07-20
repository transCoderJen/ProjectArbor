using Ink.Runtime;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Dialogue
{
    /// <summary>
    /// Provides external Ink functions for interacting with the quest system.
    ///
    /// Quest Flow:
    /// ReceiveQuest
    ///     - Makes the player aware of a quest.
    ///     - Raises QuestReceivedEvent.
    ///     - Typically used to show a "New Quest" notification.
    ///
    /// StartQuest
    ///     - Starts an already received quest.
    ///     - Raises StartQuestEvent.
    ///     - Typically used to move the quest into IN_PROGRESS state.
    ///
    /// ReceiveQuestAndStart
    ///     - Convenience function that performs both actions.
    ///     - This is the most common function used by NPC dialogue.
    ///
    /// AdvanceQuest
    ///     - Advances the quest to the next step.
    ///
    /// FinishQuest
    ///     - Marks the quest as completed.
    /// </summary>
    public class InkExternalFunctions
    {
        private Worker commandTarget;
        
        /// <summary>
        /// Registers all external functions used by Ink.
        /// </summary>
        public void Bind(Story story)
        {
            // Receive a quest without starting it.
            story.BindExternalFunction(
                "ReceiveQuest",
                (string questId) => ReceiveQuest(questId));

            // Start a previously received quest.
            story.BindExternalFunction(
                "StartQuest",
                (string questId) => StartQuest(questId));

            // Receive and immediately start a quest.
            story.BindExternalFunction(
                "ReceiveQuestAndStart",
                (string questId) => ReceiveQuestAndStart(questId));

            // Legacy alias for older Ink files.
            // Can be removed once all stories use ReceiveQuestAndStart.
            story.BindExternalFunction(
                "AcceptQuest",
                (string questId) => ReceiveQuestAndStart(questId));

            // Advance a quest to its next step.
            story.BindExternalFunction(
                "AdvanceQuest",
                (string questId) => AdvanceQuest(questId));

            // Finish a quest.
            story.BindExternalFunction(
                "FinishQuest",
                (string questId) => FinishQuest(questId));
            
            story.BindExternalFunction(
                "command_farm",
                CommandFarm);

            story.BindExternalFunction(
                "command_gather",
                CommandGather);

            
        }

        /// <summary>
        /// Unregisters all external functions.
        /// Should be called when the story is being cleaned up.
        /// </summary>
        public void Unbind(Story story)
        {
            story.UnbindExternalFunction("ReceiveQuest");
            story.UnbindExternalFunction("StartQuest");
            story.UnbindExternalFunction("ReceiveQuestAndStart");
            story.UnbindExternalFunction("AcceptQuest");
            story.UnbindExternalFunction("AdvanceQuest");
            story.UnbindExternalFunction("FinishQuest");
            story.UnbindExternalFunction("command_farm");
            story.UnbindExternalFunction("command_gather");
        }

        /// <summary>
        /// Makes the player aware of a quest.
        /// This does not start the quest.
        /// </summary>
        private void ReceiveQuest(string questId)
        {
            Bus<QuestReceivedEvent>.Raise(
                new QuestReceivedEvent(questId));
        }

        /// <summary>
        /// Convenience function used by most NPC dialogue.
        /// Receives the quest and immediately starts it.
        /// </summary>
        private void ReceiveQuestAndStart(string questId)
        {
            ReceiveQuest(questId);
            StartQuest(questId);
        }

        /// <summary>
        /// Starts a quest that has already been received.
        /// </summary>
        private void StartQuest(string questId)
        {
            Bus<StartQuestEvent>.Raise(
                new StartQuestEvent(questId));
        }

        /// <summary>
        /// Advances the quest to its next step.
        /// </summary>
        private void AdvanceQuest(string questId)
        {
            Bus<AdvanceQuestEvent>.Raise(
                new AdvanceQuestEvent(questId));
        }

        /// <summary>
        /// Completes the quest.
        /// </summary>
        private void FinishQuest(string questId)
        {
            Bus<FinishQuestEvent>.Raise(
                new FinishQuestEvent(questId));
        }

        public void SetCommandTarget(Worker worker)
        {
            commandTarget = worker;
        }

        private void CommandFarm()
        {
            if (commandTarget == null)
            {
                Debug.LogWarning(
                    "[DialogueManager] Cannot issue Farm command: no Worker target.");

                return;
            }

            Debug.Log(
                $"[DialogueManager] Issuing Farm command to {commandTarget.name}.",
                commandTarget);

            commandTarget.Farm();
        }

        private void CommandGather()
        {
            if (commandTarget == null)
            {
                Debug.LogWarning(
                    "[DialogueManager] Cannot issue Gather command: no Worker target.");

                return;
            }

            Debug.Log(
                $"[DialogueManager] Issuing Gather command to {commandTarget.name}.",
                commandTarget);

            commandTarget.Gather();
        }

        public void SetCommandTargetToNull()
        {
            commandTarget = null;
        }
    }
}