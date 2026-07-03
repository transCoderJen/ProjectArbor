using System;
using ShiftedSignal.Garden.Buildable;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Is Building Complete", story: "Check [BuildTarget] is complete", category: "Conditions", id: "87fdda124e1a10075a12fd0c941e3eed")]
public partial class IsBuildingCompleteCondition : Condition
{
    [SerializeReference] public BlackboardVariable<BaseBuilding> BuildTarget;

    public override bool IsTrue()
    {
        if (BuildTarget == null || BuildTarget.Value == null)
            return false;

        return BuildTarget.Value.CurrentBuildingState == BuildingState.Complete;
    }
}
