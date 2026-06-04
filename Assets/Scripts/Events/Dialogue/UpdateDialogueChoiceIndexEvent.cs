
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct UpdateDialogueChoiceIndexEvent : IEvent
    {
        public int ChoiceIndex  { get; private set; }

        public UpdateDialogueChoiceIndexEvent(int ChoiceIndex)
        {
            this.ChoiceIndex = ChoiceIndex;
        }
    }
}