
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.QuestSystem;

namespace ShiftedSignal.Garden.Events
{
    public struct QuestStepStateChangedEvent : IEvent
    {
        public string ID { get; private set; }
        public int StepIndex { get; private set; }
        public QuestStepState QuestStepState { get; private set; }

        public QuestStepStateChangedEvent(string id, int stepIndex, QuestStepState questStepState)
        {
            ID = id;
            StepIndex = stepIndex;
            QuestStepState = questStepState;
        }
    }
}