using UnityEngine;
using Ink.Runtime;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

public class InkDialogueVariables
{
    private Dictionary<string, Ink.Runtime.Object> variables;

    public InkDialogueVariables(Story story)
    {
        variables = new Dictionary<string, Ink.Runtime.Object>();

        foreach (string name in story.variablesState)
        {
            Ink.Runtime.Object value = story.variablesState.GetVariableWithName(name);
            variables.Add(name, value);  
        }
    }

    public void SyncVariablesAndStartListening(Story story)
    {
        SyncVariablesToStory(story);
        story.variablesState.variableChangedEvent += UpdateVariableState;
    }

    public void StopListening(Story story)
    {
        story.variablesState.variableChangedEvent -= UpdateVariableState;
    }

    public void UpdateVariableState(string name, Ink.Runtime.Object value)
    {
        if (!variables.ContainsKey(name)) 
        { 
            return; 
        }

        variables[name] = value;

    }

    private void SyncVariablesToStory(Story story)
    {
        foreach(KeyValuePair<string, Ink.Runtime.Object> variable in variables)
        {
            story.variablesState.SetGlobal(variable.Key, variable.Value);
        }
    }
}
