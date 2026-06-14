using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public readonly struct VillageReputationChangedEvent : IEvent
    {
        public readonly int OldValue;
        public readonly int NewValue;

        public VillageReputationChangedEvent(int oldValue, int newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}