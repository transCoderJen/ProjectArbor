using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public readonly struct CorruptionChangedEvent : IEvent
    {
        public readonly int OldValue;
        public readonly int NewValue;

        public CorruptionChangedEvent(int oldValue, int newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}