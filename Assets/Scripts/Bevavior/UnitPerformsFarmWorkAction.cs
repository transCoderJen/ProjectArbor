using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Units;
using System.Collections;
using ShiftedSignal.Garden.Misc;

namespace ShiftedSignal.Garden.Behavior
{   
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Unit performs Farm Work", story: "[Unit] perfroms work for [FarmTask]", category: "Action/Units", id: "b26afc789b7cf74c07deb18fc7853cab")]
    public partial class UnitPerformsFarmWorkAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<FarmTaskType> FarmTask;

        [SerializeReference] public BlackboardVariable<float> WaterTime = new(5f);
        [SerializeReference] public BlackboardVariable<float> FertilizeTime = new (8f);
        [SerializeReference] public BlackboardVariable<float> HarvestTime = new(5f);
        [SerializeReference] public BlackboardVariable<float> PlantTime = new(8f);

        private float enterTime;
        private float workTime;
        private Animator animator;

        protected override Status OnStart()
        {
            if (Unit.Value == null)
                return Status.Failure;
            
            if (!Unit.Value.TryGetComponent(out animator))
                return Status.Failure;

            switch (FarmTask.Value)
            {
                case FarmTaskType.Water:
                workTime = WaterTime.Value;
                animator.SetBool(AnimationConstants.IS_WATERING, true);
                    break;
                case FarmTaskType.Fertilize:
                workTime = FertilizeTime.Value;
                animator.SetBool(AnimationConstants.IS_FERTILIZING, true);
                    break;
                case FarmTaskType.Harvest:
                workTime = HarvestTime.Value;
                animator.SetBool(AnimationConstants.IS_HARVESTING, true);
                    break;
                case FarmTaskType.Plant:
                    workTime = PlantTime.Value;
                    animator.SetBool(AnimationConstants.IS_PLANTING, true);
                    break;
                
                default:
                    return Status.Failure;
            }
            if (workTime <= 0f)
                return Status.Failure;
            
            enterTime = Time.time;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Time.time >= enterTime + workTime)
                return Status.Success;
            
            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (animator == null)
                return;
            
            animator.SetBool(AnimationConstants.IS_HARVESTING, false);
            animator.SetBool(AnimationConstants.IS_FERTILIZING, false);
            animator.SetBool(AnimationConstants.IS_WATERING, false);
            animator.SetBool(AnimationConstants.IS_PLANTING, false);
        }
    }
}

