using System;
using UnityEngine;

[System.Serializable]
public class QuestStepState
{
    public String State;
    public string Status;

    public QuestStepState(string state, string status)
    {
        this.State = state;
        this.Status = status;

    }

    public QuestStepState()
    {
        this.State = "";
        this.Status = "";

    }
}
