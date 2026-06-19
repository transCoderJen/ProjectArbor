
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Interfaces;

namespace ShiftedSignal.Garden.Events
{
    public struct UnitSelectedEvent : IEvent
    {
        public ISelectable Unit { get; private set; }

        public UnitSelectedEvent(ISelectable unit)
        {
            Unit = unit;
        }
    }
}