using ShiftedSignal.Garden.Commands;
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct ActionSelectedEvent : IEvent
    {
        public BaseCommand Action { get; private set; }
        
        public ActionSelectedEvent(BaseCommand action)
        {
            Action = action;
        }
    }
}