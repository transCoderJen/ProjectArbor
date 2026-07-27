using Ink.Runtime;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.UserInterface.Containers;
using ShiftedSignal.Garden.UserInterface.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Dialogue
{
    /// <summary>
    /// Provides external Ink functions used by dialogue.
    /// </summary>
    public class InkExternalFunctions
    {
        private Worker commandTarget;

        /// <summary>
        /// Registers all external functions used by Ink.
        /// </summary>
        public void Bind(Story story)
        {
            story.BindExternalFunction(
                "ReceiveQuest",
                (string questId) => ReceiveQuest(questId));

            story.BindExternalFunction(
                "StartQuest",
                (string questId) => StartQuest(questId));

            story.BindExternalFunction(
                "ReceiveQuestAndStart",
                (string questId) => ReceiveQuestAndStart(questId));

            story.BindExternalFunction(
                "AcceptQuest",
                (string questId) => ReceiveQuestAndStart(questId));

            story.BindExternalFunction(
                "AdvanceQuest",
                (string questId) => AdvanceQuest(questId));

            story.BindExternalFunction(
                "FinishQuest",
                (string questId) => FinishQuest(questId));

            story.BindExternalFunction(
                "command_farm",
                CommandFarm);

            story.BindExternalFunction(
                "command_gather",
                CommandGather);

            story.BindExternalFunction(
                "command_open_construction",
                CommandOpenConstruction);
            
            story.BindExternalFunction(
                "command_begin_selected_building",
                CommandBeginSelectedBuilding);
        }

        /// <summary>
        /// Unregisters all external functions.
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
            story.UnbindExternalFunction("command_open_construction");
            story.UnbindExternalFunction("command_begin_selected_building");
        }

        private void ReceiveQuest(string questId)
        {
            Bus<QuestReceivedEvent>.Raise(
                new QuestReceivedEvent(questId));
        }

        private void ReceiveQuestAndStart(string questId)
        {
            ReceiveQuest(questId);
            StartQuest(questId);
        }

        private void StartQuest(string questId)
        {
            Bus<StartQuestEvent>.Raise(
                new StartQuestEvent(questId));
        }

        private void AdvanceQuest(string questId)
        {
            Bus<AdvanceQuestEvent>.Raise(
                new AdvanceQuestEvent(questId));
        }

        private void FinishQuest(string questId)
        {
            Bus<FinishQuestEvent>.Raise(
                new FinishQuestEvent(questId));
        }

        private void CommandOpenConstruction()
        {
            DialogueManager.Instance.WaitForConstructionMenu();
        }

        public void SetCommandTarget(Worker worker)
        {
            commandTarget = worker;
        }

        public void SetCommandTargetToNull()
        {
            commandTarget = null;
        }

        private void CommandFarm()
        {
            if (commandTarget == null)
            {
                Debug.LogWarning(
                    "[InkExternalFunctions] Cannot issue Farm command: " +
                    "no Worker target.");

                return;
            }

            commandTarget.Farm();
        }

        private void CommandGather()
        {
            if (commandTarget == null)
            {
                Debug.LogWarning(
                    "[InkExternalFunctions] Cannot issue Gather command: " +
                    "no Worker target.");

                return;
            }

            commandTarget.Gather();
        }

        private void CommandBeginSelectedBuilding()
        {
            ConstructionMenuUI menu =
                UI.Instance.constructionMenu;

            Debug.Log(
                "[InkExternalFunctions] command_begin_selected_building called\n" +
                $"  menu: {(menu != null ? menu.name : "<null>")}\n" +
                $"  menu instance ID: {(menu != null ? menu.GetInstanceID() : 0)}\n" +
                $"  frame: {Time.frameCount}");

            if (menu == null)
            {
                Debug.LogError(
                    "[InkExternalFunctions] Cannot begin placement: " +
                    "UI construction menu is null.");

                return;
            }

            menu.BeginSelectedBuildingPlacement();
        }
    }
}