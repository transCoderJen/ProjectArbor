using ShiftedSignal.Garden.Units;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Misc;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Collect Farm Supply", story: "[Unit] Collects [FarmTask] from [FarmSource]", category: "Action/Units", id: "373777da8b5371c3a2f7dd2bd6c06b06")]
    public partial class CollectFarmSupplyAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<FarmTaskType> FarmTask;
        [SerializeReference] public BlackboardVariable<GameObject> FarmSource;

        [SerializeReference] public BlackboardVariable<int> WaterAmountHeld;
        [SerializeReference] public BlackboardVariable<int> WaterCapacity;

        [SerializeReference] public BlackboardVariable<int> FertilizerAmountHeld;
        [SerializeReference] public BlackboardVariable<int> FertilizerCapacity;

        [SerializeReference] public BlackboardVariable<float> CollectTime = new(2f);

        private float enterTime;
        private Animator animator;
        private IFarmSupplySource source;

        protected override Status OnStart()
        {
            if (FarmSource.Value == null)
                return Status.Failure;

            source = FarmSource.Value.GetComponent<IFarmSupplySource>();

            if (source == null)
                return Status.Failure;

            if (!Unit.Value.TryGetComponent(out Worker worker))
                return Status.Failure;

            if (!source.TryBeginCollect(worker))
                return Status.Failure;

            enterTime = Time.time;

            if (Unit.Value.TryGetComponent(out animator))
            {
                animator.SetBool(AnimationConstants.IS_GATHERING, true);
            }

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Time.time >= enterTime + CollectTime.Value)
            {
                return Status.Success;
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (animator != null)
            {
                animator.SetBool(AnimationConstants.IS_GATHERING, false);
            }

            if (source == null)
                return;

            if (!Unit.Value.TryGetComponent(out Worker worker))
                return;

            if (CurrentStatus == Status.Success)
            {
                switch (FarmTask.Value)
                {
                    case FarmTaskType.Water:

                        int waterCollected =
                            source.CompleteCollect(
                                worker,
                                WaterCapacity.Value - WaterAmountHeld.Value);

                        WaterAmountHeld.Value += waterCollected;
                        break;

                    case FarmTaskType.Fertilize:

                        int fertilizerCollected =
                            source.CompleteCollect(
                                worker,
                                FertilizerCapacity.Value - FertilizerAmountHeld.Value);

                        FertilizerAmountHeld.Value += fertilizerCollected;
                        break;
                }

                FarmSource.Value = null;
            }
            else
            {
                source.AbortCollect(worker);
            }
        }
    }
}