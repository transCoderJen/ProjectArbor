using Ink.Runtime;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;

namespace ShiftedSignal.Garden.Dialogue
{
    public class InkExternalFunctions
    {
        public void Bind(Story story)
        {
            story.BindExternalFunction("StartQuest",
                (string questId) => StartQuest(questId));

            story.BindExternalFunction("AdvanceQuest",
                (string questId) => AdvanceQuest(questId));

            story.BindExternalFunction("FinishQuest",
                (string questId) => FinishQuest(questId));

            story.BindExternalFunction("AcceptQuest",
                (string questId) => AcceptQuest(questId));
        }

        public void Unbind(Story story)
        {
            story.UnbindExternalFunction("StartQuest");
            story.UnbindExternalFunction("AdvanceQuest");
            story.UnbindExternalFunction("FinishQuest");
            story.UnbindExternalFunction("AcceptQuest");
        }

        
        private void AcceptQuest(string questId)
        {
            Bus<QuestReceivedEvent>.Raise(
                new QuestReceivedEvent(questId));

            Bus<StartQuestEvent>.Raise(
                new StartQuestEvent(questId));
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
    }
}