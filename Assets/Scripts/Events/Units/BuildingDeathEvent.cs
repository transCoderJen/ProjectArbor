using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public readonly struct BuildingDeathEvent : IEvent
    {
        public readonly BaseBuilding Building;

        public BuildingDeathEvent(BaseBuilding building)
        {
            Building = building;
        }
    }
}