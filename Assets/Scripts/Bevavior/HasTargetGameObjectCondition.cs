using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Has Target GameObject", story: "[TargetGameObject] is assigned", category: "Conditions", id: "0956e486433b3758acc57cc37978a908")]
public partial class HasTargetGameObjectCondition : Condition
{
    [SerializeReference]
    public BlackboardVariable<GameObject> TargetGameObject;

    public override bool IsTrue()
    {
        return TargetGameObject.Value != null;
    }
}
