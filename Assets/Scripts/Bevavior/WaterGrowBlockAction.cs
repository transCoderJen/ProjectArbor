using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.GridSystem;

// NOT USED.  I'm just scared to delete it and mess up my behaviour tree.  
namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Water Grow Block", story: "[Unit] waters [FarmTarget]", category: "Action/Units", id: "3f7c7b2af49c1a370033a4bc185bfe7f")]
    public partial class WaterGrowBlockAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> FarmTarget;
        [SerializeReference] public BlackboardVariable<FarmTaskType> FarmTask;

        protected override Status OnStart()
        {
            if (FarmTarget.Value == null)
            {
                Debug.Log("WaterGrowBlock failed: FarmTarget is null");
                return Status.Failure;
            }

            if (!FarmTarget.Value.TryGetComponent(out GrowBlock growBlock))
            {
                Debug.Log($"WaterGrowBlock failed: {FarmTarget.Value.name} has no GrowBlock");
                return Status.Failure;
            }

            Debug.Log(
                $"WaterGrowBlock check | " +
                $"Task={FarmTask.Value} | " +
                $"Stage={growBlock.CurrentStage} | " +
                $"HasCrop={growBlock.HasCrop} | " +
                $"IsGrowing={growBlock.IsGrowing} | " +
                $"IsWatered={growBlock.IsWatered} | " +
                $"RequiresWater={growBlock.Seed?.RequiresWater} | " +
                $"NeedsWater={growBlock.NeedsWater}");

            if (FarmTask.Value != FarmTaskType.Water)
            {
                Debug.Log("WaterGrowBlock failed: FarmTask is not Water");
                return Status.Failure;
            }

            bool watered = growBlock.TryWater();

            Debug.Log($"WaterGrowBlock TryWater result: {watered}");

            FarmTarget.Value = null;
            FarmTask.Value = FarmTaskType.None;

            return watered ? Status.Success : Status.Failure;
        }
    }    
}


