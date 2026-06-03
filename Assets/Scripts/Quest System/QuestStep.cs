using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using UnityEngine;

namespace ShiftedSignal.Garden.QuestSystem
{    
    public abstract class QuestStep : MonoBehaviour
    {
        private bool isFinished = false;
        private string questId;
        private int stepIndex;

        public void InitializeQuestStep(string questId, int stepIndex, string questStepState)
        {
            this.questId = questId;
            this.stepIndex = stepIndex;
            if (questStepState != null && questStepState != "")
            {
                SetQuestStepState(questStepState);
            }
        }

        protected void FinishQuestStep()
        {
            if (!isFinished)
            {
                isFinished = true;

                Bus<AdvanceQuestEvent>.Raise(new AdvanceQuestEvent(questId));

                Destroy(this.gameObject);
            }
        }

        protected void ChangeState(string newState, string newStatus)
        {
            Bus<QuestStepStateChangedEvent>.Raise(
                new QuestStepStateChangedEvent(
                    questId, 
                    stepIndex, 
                    new QuestStepState(newState, newStatus)));
        }

        protected abstract void SetQuestStepState(string state);

    }
}
