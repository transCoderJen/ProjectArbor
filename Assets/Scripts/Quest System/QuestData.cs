using ShiftedSignal.Garden.QuestSystem;
using UnityEngine;

[System.Serializable]
public class QuestData
{
    public QuestState State;
    public int QuestStepIndex;
    public QuestStepState[] QuestStepStates;

    public QuestData(QuestState state, int questStepIndex, QuestStepState[] queueStepStates)
    {
        State = state;
        QuestStepIndex = questStepIndex;
        QuestStepStates = queueStepStates;
    }

}
