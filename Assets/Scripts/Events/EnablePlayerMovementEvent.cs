using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct EnablePlayerMovementEvent : IEvent
    {
        public bool EnableMovement { get; private set; }

        public EnablePlayerMovementEvent(bool enable)
        {
            this.EnableMovement = enable;
        }
    }
}