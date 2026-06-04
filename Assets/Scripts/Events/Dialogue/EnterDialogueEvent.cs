
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct EnterDialogueEvent : IEvent
    {
        public string KnotName { get; private set; }

        public EnterDialogueEvent(string knotName)
        {
            this.KnotName = knotName;
        }
    }
}