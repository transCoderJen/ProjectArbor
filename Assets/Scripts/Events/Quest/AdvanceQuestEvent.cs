
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct AdvanceQuestEvent : IEvent
    {
        public string Id { get; private set; }

        public AdvanceQuestEvent(string id)
        {
            Id = id;
        }
    } 
}