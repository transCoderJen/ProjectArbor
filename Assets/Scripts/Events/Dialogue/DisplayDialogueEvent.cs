
using System.Transactions;
using ShiftedSignal.Garden.EventBus;
using Ink.Runtime;
using System.Collections.Generic;

namespace ShiftedSignal.Garden.Events
{
    public struct DisplayDialogueEvent : IEvent
    {
        public bool IsNote { get; private set; }
        public string SpeakerID { get; private set; }
        public string DialogueLine { get; private set; }
        public List<Choice> DialogueChoices { get; private set; }
        
        public DisplayDialogueEvent(bool isNote, string speakerId, string dialogueLine, List<Choice> dialogueChoices)
        {
            this.IsNote = isNote;
            this.SpeakerID = speakerId;
            this.DialogueLine = dialogueLine;
            this.DialogueChoices = dialogueChoices;
        }
    }
}