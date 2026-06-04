using ShiftedSignal.Garden.QuestSystem;

[System.Serializable]
public class QuestData
{
    public QuestState State;
    public int QuestStepIndex;
    public QuestStepState[] QuestStepStates;
    public bool IsReceived;

    public QuestData(
        QuestState state,
        int questStepIndex,
        QuestStepState[] questStepStates,
        bool isReceived)
    {
        State = state;
        QuestStepIndex = questStepIndex;
        QuestStepStates = questStepStates;
        IsReceived = isReceived;
    }
}