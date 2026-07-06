using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Units;

namespace ShiftedSignal.Garden.Events
{
    public readonly struct UnitDeathEvent : IEvent
    {
        public readonly AbstractUnit Unit;

        public UnitDeathEvent(AbstractUnit unit)
        {
            Unit = unit;
        }
    }
}