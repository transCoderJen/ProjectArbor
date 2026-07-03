using ShiftedSignal.Garden.Buildable;
using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Building Under Construction", story: "Is [BuildTarget] under construction", category: "Conditions", id: "fc7f32237bd1d56ade6cd2e784d093fe")]
public partial class BuildingUnderConstructionCondition : Condition
{
    [SerializeReference] public BlackboardVariable<BaseBuilding> BuildTarget;

    public override bool IsTrue()
    {
        if (BuildTarget == null || BuildTarget.Value == null)
            return false;

        return BuildTarget.Value.CurrentBuildingState == BuildingState.UnderConstruction;
    }

}
