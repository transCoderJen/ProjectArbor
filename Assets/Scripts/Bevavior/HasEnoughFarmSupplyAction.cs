using ShiftedSignal.Garden.Units;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.ItemsAndInventory;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Has Enough Farm Supply", story: "[Unit] has enough supply for [FarmTask]", category: "Action/Units", id: "615f8fdded2e867c68f7ee568bf143f8")]
    public partial class HasEnoughFarmSupplyAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<FarmTaskType> FarmTask;

        [SerializeReference] public BlackboardVariable<int> WaterAmountHeld;
        [SerializeReference] public BlackboardVariable<int> FertilizerAmountHeld;
        [SerializeReference] public BlackboardVariable<ItemData_Seed> SeedHeld;
        [SerializeReference] public BlackboardVariable<int> SeedAmountHeld;

        protected override Status OnStart()
        {
            switch (FarmTask.Value)
            {
                case FarmTaskType.Water:
                    return WaterAmountHeld.Value > 0
                        ? Status.Success
                        : Status.Failure;

                case FarmTaskType.Fertilize:
                    return FertilizerAmountHeld.Value > 0
                        ? Status.Success
                        : Status.Failure;
                case FarmTaskType.Plant:
                    return SeedHeld.Value != null && SeedAmountHeld.Value > 0
                        ? Status.Success
                        : Status.Failure;
                case FarmTaskType.Harvest:
                    return Status.Success;
                    
                default:
                    return Status.Failure;
            }
        }
    }
}