using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct ToolEquipEvent : IEvent
    {
        public ToolType Tool { get; private set; }

        public ToolEquipEvent(int tool)
        {
            Tool = (ToolType) tool;
        }
    }
    
}