using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public readonly struct SetDialogueVisibilityEvent : IEvent
    {
        public bool IsVisible { get; }

        public SetDialogueVisibilityEvent(bool isVisible)
        {
            IsVisible = isVisible;
        }
    }
}