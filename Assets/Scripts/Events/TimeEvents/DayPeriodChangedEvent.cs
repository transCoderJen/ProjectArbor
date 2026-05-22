using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Managers;

namespace ShiftedSignal.Garden.Events
{
    public struct DayPeriodChangedEvent : IEvent
    {        
        public DayPeriod DayPeriod { get; private set; }

        public DayPeriodChangedEvent(DayPeriod dayPeriod)
        {
            this.DayPeriod = dayPeriod;
        }
    }
}