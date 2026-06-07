using UnityEngine;

namespace ShiftedSignal.Garden.QuestSystem
{    
    public class Quest 
    {
        public QuestInfoSO Info;
        public QuestState State;
        private int currentQuestStepIndex;
        private QuestStepState[] questStepStates;
        public bool IsReceived { get; private set; }

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

        public void ReceiveQuest()
        {
            Debug.Log("Recieveing quest in the quest");
            IsReceived = true;
        }

        public void MoveToNextStep()
        {
            currentQuestStepIndex++;
        }

        public bool CurrentStepExists()
        {
            return currentQuestStepIndex >= 0 &&
                currentQuestStepIndex < Info.QuestStepPrefabs.Length &&
                currentQuestStepIndex < questStepStates.Length;
        }

        public string GetCurrentStepDescription()
        {
            if (State == QuestState.FINISHED)
                return "Quest complete";

            if (!CurrentStepExists())
                return "Return to quest giver";

            QuestStep questStep =
                Info.QuestStepPrefabs[currentQuestStepIndex].GetComponent<QuestStep>();

            if (questStep == null)
                return "";

            return questStep.GetStepDescription();
        }

        
        ////// OLD CODE THAT CREATED DUPLICATE QUEST STEPS
        
        // public void InstantiateCurrentQuestStep(Transform parentTransform)
        // {
        //     GameObject questStepPrefab = GetCurrentQuestStepPrefab();
        //     if (questStepPrefab != null)
        //     {
        //         QuestStep questStep = Object.Instantiate<GameObject>(questStepPrefab, parentTransform)
        //             .GetComponent<QuestStep>();
        //         questStep.InitializeQuestStep(Info.ID, currentQuestStepIndex, questStepStates[currentQuestStepIndex].State);
        //     }
        // }

        public void InstantiateCurrentQuestStep(Transform parentTransform)
        {
            if (!CurrentStepExists())
                return;

            GameObject questStepPrefab = Info.QuestStepPrefabs[currentQuestStepIndex];

            string stepObjectName = $"{Info.ID}_Step_{currentQuestStepIndex}";

            Transform existingStep = parentTransform.Find(stepObjectName);

            if (existingStep != null)
            {
                Debug.Log($"Quest step already active: {stepObjectName}");
                return;
            }

            GameObject questStepInstance = Object.Instantiate(
                questStepPrefab,
                parentTransform
            );

            questStepInstance.name = stepObjectName;

            QuestStep questStep = questStepInstance.GetComponent<QuestStep>();

            if (questStep != null)
            {
                questStep.InitializeQuestStep(Info.ID, currentQuestStepIndex, questStepStates[currentQuestStepIndex].State);
            }
        }

        public string GetCurrentStepStatusText()
        {
            if (questStepStates == null || questStepStates.Length == 0)
                return "";

            if (currentQuestStepIndex < 0 || currentQuestStepIndex >= questStepStates.Length)
                return "";

            return questStepStates[currentQuestStepIndex].Status;
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
                questStepStates[stepIndex].Status = questStepState.Status;
            }
            else
            {
                Debug.LogWarning("Tried to access quest step data out of range: "
                    + "Quest Id = " + Info.ID + " Step Index = " + stepIndex);
            }
        }

        public QuestData GetQuestData()
        {
            return new QuestData(
                State,
                currentQuestStepIndex,
                questStepStates,
                IsReceived
            );
        }

        public void LoadQuestData(QuestData questData)
        {
            State = questData.State;
            currentQuestStepIndex = questData.QuestStepIndex;
            questStepStates = questData.QuestStepStates;
            IsReceived = questData.IsReceived;

            if (questStepStates.Length != Info.QuestStepPrefabs.Length)
            {
                Debug.LogWarning("Quest Step prefab and Quest Step States are "
                    + "of different lengths. The Quest Info has changed and is now "
                    + "out of sync. Resetting the data is recommended. "
                    + "QuestID: " + Info.ID);
            }
        }

        public string GetFullStatusText()
        {
            string fullStatus = "";

            if (State == QuestState.REQUIREMENTS_NOT_MET)
            {
                return "Requirements are not yet met to start this quest";
            }

            if (State == QuestState.CAN_START)
            {
                return "This quest can be started!";
            }

            int completedStepCount = Mathf.Min(currentQuestStepIndex, questStepStates.Length);

            for (int i = 0; i < completedStepCount; i++)
            {
                fullStatus += "<s>" + questStepStates[i].Status + "</s>\n";
            }

            if (CurrentStepExists() && currentQuestStepIndex < questStepStates.Length)
            {
                fullStatus += questStepStates[currentQuestStepIndex].Status + "\n";
            }

            if (State == QuestState.CAN_FINISH)
            {
                fullStatus += "The quest is ready to be turned in.";
            }
            else if (State == QuestState.FINISHED)
            {
                fullStatus += "The quest has been completed!";
            }

            return fullStatus;
        }
    }
}
