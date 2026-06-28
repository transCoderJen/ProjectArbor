using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Units;

namespace ShiftedSignal.Garden.Events
{
    public readonly struct UnitDestroyedEvent : IEvent
    {
        public readonly AbstractUnit Unit;

        public UnitDestroyedEvent(AbstractUnit unit)
        {
            Unit = unit;
        }
    }
}