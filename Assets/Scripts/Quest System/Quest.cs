using UnityEngine;

namespace ShiftedSignal.Garden.QuestSystem
{    
    public class Quest 
    {
        public QuestInfoSO Info;
        public QuestState State;
        private int currentQuestStepIndex;
        private QuestStepState[] questStepStates;

        public Quest(QuestInfoSO questInfo)
        {
            this.Info = questInfo;
            this.State = QuestState.REQUIREMENTS_NOT_MET;
            this.currentQuestStepIndex = 0;
            this.questStepStates = new QuestStepState[Info.QuestStepPrefabs.Length];
            for (int i = 0; i < questStepStates.Length; i++)
            {
                questStepStates[i] = new QuestStepState();
            }
        }

        public Quest(QuestInfoSO questInfo, QuestState questState, int currentQuestStepIndex, QuestStepState[] questStepStates)
        {
            this.Info = questInfo;
            this.State = questState;
            this.currentQuestStepIndex = currentQuestStepIndex;
            this.questStepStates = questStepStates;

            if (this.questStepStates.Length != this.Info.QuestStepPrefabs.Length)
            {
                Debug.LogWarning("Quest Step prefab and Quest Step States are " 
                    + "of differernt lenghts.  The Quest Info has changed and is now "
                    + "out of synce.  Ressetting the data is recommmended. "
                    + "QuestID: " + this.Info.ID);
            }
        }

        public void MoveToNextStep()
        {
            currentQuestStepIndex++;
        }

        public bool CurrentStepExists()
        {
            return currentQuestStepIndex < Info.QuestStepPrefabs.Length;
        }

        public void InstantiateCurrentQuestStep(Transform parentTransform)
        {
            GameObject questStepPrefab = GetCurrentQuestStepPrefab();
            if (questStepPrefab != null)
            {
                QuestStep questStep = Object.Instantiate<GameObject>(questStepPrefab, parentTransform)
                    .GetComponent<QuestStep>();
                questStep.InitializeQuestStep(Info.ID, currentQuestStepIndex, questStepStates[currentQuestStepIndex].State);
            }
        }

        private GameObject GetCurrentQuestStepPrefab()
        {
            GameObject questStepPrefab = null;
            if (CurrentStepExists())
            {
                questStepPrefab = Info.QuestStepPrefabs[currentQuestStepIndex];
            }
            else
            {
                Debug.LogWarning("Tried to get quest step prefab, but stepIndex was out of range inicating that "
                + "ther's no current step: QuestId= " + Info.ID + ", stepIndex= " + currentQuestStepIndex);
            }

            return questStepPrefab;
        }

        public void StoreQuestStepState(QuestStepState questStepState, int stepIndex)
        {
            if (stepIndex < questStepStates.Length)
            {
                questStepStates[stepIndex].State = questStepState.State;
            }
            else
            {
                Debug.LogWarning("Tried to access quest step data out of range: "
                    + "Quest Id = " + Info.ID + " Step Index = " + stepIndex);
            }
        }

        public QuestData GetQuestData()
        {
            return new QuestData(State, currentQuestStepIndex, questStepStates);
        }

        public void LoadQuestData(QuestData questData)
        {
            State = questData.State;
            currentQuestStepIndex = questData.QuestStepIndex;
            questStepStates = questData.QuestStepStates;

            if (questStepStates.Length != Info.QuestStepPrefabs.Length)
            {
                Debug.LogWarning("Quest Step prefab and Quest Step States are "
                    + "of different lengths. The Quest Info has changed and is now "
                    + "out of sync. Resetting the data is recommended. "
                    + "QuestID: " + Info.ID);
            }
        }
    }
}
