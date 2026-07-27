using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public readonly struct ConstructionMenuClosedEvent : IEvent
    {
        public BuildingSO SelectedBuilding { get; }

        public bool BuildingSelected =>
            SelectedBuilding != null;

        public ConstructionMenuClosedEvent(
            BuildingSO selectedBuilding)
        {
            SelectedBuilding = selectedBuilding;
        }
    }
}