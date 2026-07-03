using ShiftedSignal.Garden.Buildable;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Unity.VisualScripting;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PickRandomLocationWithinRendererBounds", story: "Set [TargetLocation] to a random point within [BuildTarget]", category: "Action", id: "2e272229890d57830578848a1c9785d5")]
    public partial class PickRandomLocationWithinRendererBoundsAction : Action
    {
        [SerializeReference] public BlackboardVariable<Vector3> TargetLocation;
        [SerializeReference] public BlackboardVariable<BaseBuilding> BuildTarget;

        protected override Status OnStart()
        {
            if (BuildTarget.Value == null)
                return Status.Failure;

            Renderer renderer = BuildTarget.Value.GetComponent<Renderer>();

            if (renderer == null)
                return Status.Failure;

            Bounds bounds = renderer.bounds;

            float minDepth = 4f;

            float zMin = bounds.center.z - Mathf.Max(bounds.size.z, minDepth) * 0.5f;
            float zMax = bounds.center.z + Mathf.Max(bounds.size.z, minDepth) * 0.5f;

            TargetLocation.Value = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                TargetLocation.Value.y,
                UnityEngine.Random.Range(zMin, zMax)
            );

            return Status.Success;
        }
    }
}

