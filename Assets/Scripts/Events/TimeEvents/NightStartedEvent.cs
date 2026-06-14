using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public readonly struct NightStartedEvent : IEvent
    {
        public readonly int NightNumber;

        public NightStartedEvent(int nightNumber)
        {
            NightNumber = nightNumber;
        }
    }
}