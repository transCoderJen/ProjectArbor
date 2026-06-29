using ShiftedSignal.Garden.Units;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.ItemsAndInventory;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Perform Farm Task", story: "[Unit] performs [FarmTask] on [CropTarget]", category: "Action/Units", id: "e3ff61f1d93910047b5543d7899c1b8f")]
    public partial class PerformFarmTaskAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> CropTarget;
        [SerializeReference] public BlackboardVariable<FarmTaskType> FarmTask;
        [SerializeReference] public BlackboardVariable<int> WaterAmountHeld;
        [SerializeReference] public BlackboardVariable<int> FertilizerAmountHeld;
        [SerializeReference] public BlackboardVariable<ItemData_Seed> SeedHeld;
        [SerializeReference] public BlackboardVariable<int> SeedAmountHeld;

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
                FarmTaskType.Harvest => growBlock.TryHarvest(),
                FarmTaskType.Plant => growBlock.TryPlant(SeedHeld.Value),
                _ => false
            };

            // Always release the reservation once we've attempted the task.
            growBlock.ReleaseFarmTask(worker);

            if (!success)
                return Status.Failure;

            // Consume the carried farm supply.
            switch (FarmTask.Value)
            {
                case FarmTaskType.Water:
                    WaterAmountHeld.Value = Mathf.Max(
                        0,
                        WaterAmountHeld.Value - 1);
                    break;

                case FarmTaskType.Fertilize:
                    FertilizerAmountHeld.Value = Mathf.Max(
                        0,
                        FertilizerAmountHeld.Value - 1);
                    break;

                case FarmTaskType.Plant:
                    SeedAmountHeld.Value = Mathf.Max(
                        0,
                        SeedAmountHeld.Value - 1);

                    if (SeedAmountHeld.Value == 0)
                    {
                        SeedHeld.Value = null;
                    }
                    break;
            }

            CropTarget.Value = null;
            FarmTask.Value = FarmTaskType.None;

            return Status.Success;
        }

        
    }
}