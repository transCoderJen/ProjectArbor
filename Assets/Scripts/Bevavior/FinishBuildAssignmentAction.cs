using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.Buildable;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FinishBuildAssignment", story: "[Unit] finished build assignment", category: "Action/Units", id: "6349cd046b51b8f7e876a1586b54e87d")]
public partial class FinishBuildAssignmentAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Unit;
    
    [SerializeReference] public BlackboardVariable<BaseBuilding> Building;
    private Worker worker;

    protected override Status OnStart()
    {
        if (!Unit.Value.TryGetComponent(out worker))
                return Status.Failure;

         if (!Building.Value.IsUnderConstruction)
                worker.FinishBuildAssignment();
            else
                worker.ClearBuildAssignment();
        return Status.Success;
    }
}

