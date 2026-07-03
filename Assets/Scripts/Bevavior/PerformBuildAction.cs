using ShiftedSignal.Garden.Buildable;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Units;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Perform Build", story: "[Unit] build [BuildTarget]", category: "Action/Units", id: "6437b41983410b7c7d4ebd1e4fa4defc")]
    public partial class PerformBuildAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> BuildTarget;

        [SerializeReference] public BlackboardVariable<float> BuildPower = new(1f);

        private Worker worker;
        private BaseBuilding building;

        protected override Status OnStart()
        {
            if (Unit.Value == null || BuildTarget.Value == null)
                return Status.Failure;

            if (!Unit.Value.TryGetComponent(out worker))
                return Status.Failure;

            if (!BuildTarget.Value.TryGetComponent(out building))
                return Status.Failure;

            if (!building.IsUnderConstruction)
                return Status.Failure;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (worker == null || building == null)
                return Status.Failure;

            if (!building.IsUnderConstruction)
                return Status.Success;

            building.AddBuildProgress(BuildPower.Value * Time.deltaTime);

            return building.IsUnderConstruction
                ? Status.Running
                : Status.Success;
        }

        protected override void OnEnd()
        {
            if (building != null && worker != null)
                building.ReleaseBuilder(worker);
        }
    }
}

