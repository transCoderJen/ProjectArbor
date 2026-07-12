using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public readonly struct BuildingSpawnEvent : IEvent
    {
        public readonly BaseBuilding Building;

        public BuildingSpawnEvent(BaseBuilding building)
        {
            Building = building;
        }
    }
}