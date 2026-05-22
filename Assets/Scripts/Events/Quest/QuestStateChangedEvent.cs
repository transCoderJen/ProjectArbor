
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.QuestSystem;

namespace ShiftedSignal.Garden.Events
{
    public struct QuestStateChangedEvent : IEvent
    {
        public Quest Quest { get; private set; }

        public QuestStateChangedEvent(Quest quest)
        {
            Quest = quest;
        }
    }
}