using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct HourChangedEvent : IEvent
    {
        public int Hour { get; private set; }

        public HourChangedEvent(int hour)
        {
            this.Hour = hour;
        }
        
    }
}