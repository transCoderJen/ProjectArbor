using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Check for Retaliation",
        story: "Check if [RetaliationTarget] exists",
        category: "Action/Conditional",
        id: "4a26a0b0c5c2055fedff8a372f70151a")]
    public partial class CheckForRetaliationAction : Action
    {
        [SerializeReference]
        public BlackboardVariable<GameObject> RetaliationTarget;

        protected override Status OnStart()
        {
            if (RetaliationTarget == null ||
                RetaliationTarget.Value == null)
            {
                return Status.Failure;
            }

            return Status.Success;
        }
    }
}