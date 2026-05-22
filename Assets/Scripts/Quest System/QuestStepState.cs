using System;
using UnityEngine;

[System.Serializable]
public class QuestStepState
{
    public String State;

    public QuestStepState(string state)
    {
        State = state;
    }

    public QuestStepState()
    {
        State = "";
    }
}
