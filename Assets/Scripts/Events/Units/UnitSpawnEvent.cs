
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Units;

namespace ShiftedSignal.Garden.Events
{
    public struct UnitSpawnEvent : IEvent
    {
        public AbstractUnit Unit { get; private set; }

        public UnitSpawnEvent(AbstractUnit unit)
        {
            Unit = unit;
        }
    }
}