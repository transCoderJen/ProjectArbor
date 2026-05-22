
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct FinishQuestEvent : IEvent
    {
        public string Id { get; private set; }

        public FinishQuestEvent(string id)
        {
            Id = id;
        }
    } 
}