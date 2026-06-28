using ShiftedSignal.Garden.Units;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Managers;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Find Farm Task", story: "[Unit] finds [CropTarget] and [FarmTask]", category: "Action/Units", id: "0e6ab5a8ab42d7298626639b4f7377a5")]
    public partial class FindFarmTaskAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> CropTarget;
        [SerializeReference] public BlackboardVariable<FarmTaskType> FarmTask;
        [SerializeReference] public BlackboardVariable<float> SearchRadius = new(500f);

        protected override Status OnStart()
        {
            if (Unit.Value == null)
                return Status.Failure;

            if (!Unit.Value.TryGetComponent(out Worker worker))
                return Status.Failure;

            if (GridManager.Instance == null)
                return Status.Failure;

            GrowBlock bestBlock = null;
            FarmTaskType bestTask = FarmTaskType.None;
            float bestDistanceSqr = float.MaxValue;

            foreach (GrowBlock block in GridManager.Instance.GetBlocksInRadius(
                        Unit.Value.transform.position,
                        SearchRadius.Value))
            {
                if (block == null)
                    continue;

                if (block.IsReservedByAnotherWorker(worker))
                    continue;

                if (!block.HasActionableFarmTask)
                    continue;

                FarmTaskType task = FarmTaskType.None;

                if (block.NeedsWater)
                    task = FarmTaskType.Water;
                else if (block.NeedsFertilizer)
                    task = FarmTaskType.Fertilize;

                if (task == FarmTaskType.None)
                    continue;

                float distanceSqr =
                    (block.transform.position - Unit.Value.transform.position).sqrMagnitude;

                if (distanceSqr >= bestDistanceSqr)
                    continue;

                bestBlock = block;
                bestTask = task;
                bestDistanceSqr = distanceSqr;
            }

            if (bestBlock == null)
            {
                CropTarget.Value = null;
                FarmTask.Value = FarmTaskType.None;
                return Status.Failure;
            }

            if (!bestBlock.TryReserveFarmTask(worker))
            {
                CropTarget.Value = null;
                FarmTask.Value = FarmTaskType.None;
                return Status.Failure;
            }

            CropTarget.Value = bestBlock.gameObject;
            FarmTask.Value = bestTask;

            return Status.Success;
        }
    }
}
