using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct UpdateInGameTimerEvent : IEvent
    {
        public bool RunTimer { get; private set; }
        
        public UpdateInGameTimerEvent(bool runTimer)
        {
            RunTimer = runTimer;
        }
    }
}