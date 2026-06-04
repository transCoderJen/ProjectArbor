
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct QuestRecievedEvent : IEvent
    {
        public string Id { get; private set; }

        public QuestRecievedEvent(string id)
        {
            Id = id;
        }
    } 
}