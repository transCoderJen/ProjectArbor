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
    [NodeDescription(
        name: "Find Farm Task",
        story: "[Unit] finds [CropTarget] and [FarmTask]",
        category: "Action/Units",
        id: "0e6ab5a8ab42d7298626639b4f7377a5")]
    public partial class FindFarmTaskAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> CropTarget;
        [SerializeReference] public BlackboardVariable<FarmTaskType> FarmTask;
        [SerializeReference] public BlackboardVariable<float> SearchRadius = new(500f);

        private FarmTaskType currentTask = FarmTaskType.Harvest;

        private const float NoTaskRetryDelay = 0.5f;
        private float nextSearchTime;

        protected override Status OnStart()
        {
            return TryFindTaskOrWait();
        }

        protected override Status OnUpdate()
        {
            return TryFindTaskOrWait();
        }

        private Status TryFindTaskOrWait()
        {
            if (Time.time < nextSearchTime)
                return Status.Running;

            if (Unit.Value == null)
                return Status.Failure;

            if (!Unit.Value.TryGetComponent(out Worker worker))
                return Status.Failure;

            if (GridManager.Instance == null)
                return Status.Failure;

            FarmTaskType taskToFind = currentTask;

            GrowBlock bestBlock = FindBestBlockForTask(worker, taskToFind);

            AdvanceTask();

            if (bestBlock == null)
            {
                CropTarget.Value = null;
                FarmTask.Value = FarmTaskType.None;

                nextSearchTime = Time.time + NoTaskRetryDelay;

                return Status.Running;
            }

            if (!bestBlock.TryReserveFarmTask(worker))
            {
                CropTarget.Value = null;
                FarmTask.Value = FarmTaskType.None;

                nextSearchTime = Time.time + NoTaskRetryDelay;

                return Status.Running;
            }

            CropTarget.Value = bestBlock.gameObject;
            FarmTask.Value = taskToFind;

            return Status.Success;
        }

        private GrowBlock FindBestBlockForTask(Worker worker, FarmTaskType taskToFind)
        {
            GrowBlock bestBlock = null;
            float bestDistanceSqr = float.MaxValue;

            foreach (GrowBlock block in GridManager.Instance.GetBlocksInRadius(
                         Unit.Value.transform.position,
                         SearchRadius.Value))
            {
                if (block == null)
                    continue;

                if (block.IsReservedByAnotherWorker(worker))
                    continue;

                if (!BlockNeedsTask(block, taskToFind))
                    continue;

                float distanceSqr =
                    (block.transform.position - Unit.Value.transform.position).sqrMagnitude;

                if (distanceSqr >= bestDistanceSqr)
                    continue;

                bestBlock = block;
                bestDistanceSqr = distanceSqr;
            }

            return bestBlock;
        }

        private bool BlockNeedsTask(GrowBlock block, FarmTaskType task)
        {
            return task switch
            {
                FarmTaskType.Harvest => block.NeedsHarvest,
                FarmTaskType.Water => block.NeedsWater,
                FarmTaskType.Fertilize => block.NeedsFertilizer,
                FarmTaskType.Plant => block.NeedsPlanting,
                _ => false
            };
        }

        private void AdvanceTask()
        {
            currentTask = currentTask switch
            {
                FarmTaskType.Harvest => FarmTaskType.Water,
                FarmTaskType.Water => FarmTaskType.Fertilize,
                FarmTaskType.Fertilize => FarmTaskType.Plant,
                FarmTaskType.Plant => FarmTaskType.Harvest,
                _ => FarmTaskType.Harvest
            };
        }
    }
}