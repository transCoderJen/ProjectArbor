using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public readonly struct PlayOrReplaceDialogueEvent : IEvent
    {
        public string KnotName { get; }

        public PlayOrReplaceDialogueEvent(
            string knotName)
        {
            KnotName = knotName;
        }
    }
}