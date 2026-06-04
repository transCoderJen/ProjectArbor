
using ShiftedSignal.Garden.EventBus;
using Ink.Runtime;

namespace ShiftedSignal.Garden.Events
{
    public struct UpdateInkDialogueVariableEvent : IEvent
    {
        public string Name { get; private set; }
        public Ink.Runtime.Object Value { get; private set; }

        public UpdateInkDialogueVariableEvent(string name, Ink.Runtime.Object value)
        {
            this.Name = name;
            this.Value = value;
        }
    }
}