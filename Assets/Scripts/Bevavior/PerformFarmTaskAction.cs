using ShiftedSignal.Garden.Units;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.GridSystem;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Perform Farm Task", story: "[Unit] performs [FarmTask] on [CropTarget]", category: "Action/Units", id: "e3ff61f1d93910047b5543d7899c1b8f")]
    public partial class PerformFarmTaskAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> CropTarget;
        [SerializeReference] public BlackboardVariable<FarmTaskType> FarmTask;

        protected override Status OnStart()
        {
            if (CropTarget.Value == null)
                return Status.Failure;
            
            if (!Unit.Value.TryGetComponent(out Worker worker))
                return Status.Failure;

            if (!CropTarget.Value.TryGetComponent(out GrowBlock growBlock))
                return Status.Failure;

            bool success = FarmTask.Value switch
            {
                FarmTaskType.Water => growBlock.TryWater(),
                FarmTaskType.Fertilize => growBlock.TryFertilize(),
                _ => false
            };

            growBlock.ReleaseFarmTask(worker);

            if (!success)
                return Status.Failure;

            CropTarget.Value = null;
            FarmTask.Value = FarmTaskType.None;

            return Status.Success;
        }
    }
}