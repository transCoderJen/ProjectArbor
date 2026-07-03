using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public readonly struct BuildingPlacedForConstructionEvent : IEvent
    {
        public readonly BaseBuilding Building;

        public BuildingPlacedForConstructionEvent(BaseBuilding building)
        {
            Building = building;
        }
    }
}