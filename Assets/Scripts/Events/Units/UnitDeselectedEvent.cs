using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Interfaces;

namespace ShiftedSignal.Garden.Events
{
    public struct UnitDeselectedEvent : IEvent
    {
        public ISelectable Unit { get; private set; }

        public UnitDeselectedEvent(ISelectable unit)
        {
            Unit = unit;
        }
    }
}