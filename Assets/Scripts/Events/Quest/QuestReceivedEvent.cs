
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct QuestReceivedEvent : IEvent
    {
        public string Id { get; private set; }

        public QuestReceivedEvent(string id)
        {
            Id = id;
        }
    } 
}