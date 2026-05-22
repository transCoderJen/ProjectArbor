using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct DayChangedEvent : IEvent
    {        
        public int Day { get; private set; }

        public DayChangedEvent(int day)
        {
            this.Day = day;
        }
    }
}