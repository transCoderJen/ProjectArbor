using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.QuestSystem;

namespace ShiftedSignal.Garden.Events
{
    public struct TrackedQuestChangedEvent : IEvent
    {
        public Quest Quest { get; private set; }

        public TrackedQuestChangedEvent(Quest quest)
        {
            Quest = quest;
        }
    }
}