using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct StartQuestEvent : IEvent
    {
        public string Id { get; private set; }

        public StartQuestEvent(string id)
        {
            Id = id;
        }
    } 
}