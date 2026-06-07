using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.QuestSystem;

namespace ShiftedSignal.Garden.Events
{
    public struct QuestStepAdvancedEvent : IEvent
    {
        public Quest Quest { get; private set; }

        public QuestStepAdvancedEvent(Quest quest)
        {
            Quest = quest;
        }
    }
}