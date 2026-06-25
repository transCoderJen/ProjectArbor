using ShiftedSignal.Garden.Environment;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.Misc;

namespace ShiftedSignal.Garden.Behavior
{  
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Gather Supplies", story: "[Unit] gathers [Amount] supplies from [GatherableSupplies] as [SupplySO]", category: "Action/Units", id: "5d1197f966a660aa6aef68977e1e8d78")]
    public partial class GatherSuppliesAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<int> Amount;
        [SerializeReference] public BlackboardVariable<GatherableSupply> GatherableSupplies;
        [SerializeReference] public BlackboardVariable<SupplySO> SupplySO;

        private float enterTime;
        private Animator animator;
        protected override Status OnStart()
        {
            if (GatherableSupplies.Value == null)
            {
                return Status.Failure;
            }

            enterTime = Time.time;

            if (Unit.Value.TryGetComponent(out animator))
            {
                animator.SetBool(AnimationConstants.IS_GATHERING, true);
            }

            GatherableSupplies.Value.BeginGather();
            SupplySO.Value = GatherableSupplies.Value.Supply;
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (GatherableSupplies.Value.Supply.BaseGatherTime + enterTime <= Time.time)
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
            
            if (GatherableSupplies.Value == null)
            {
                return;
            }

            if (CurrentStatus == Status.Success)
            {
                // Only complete the gather on success
                Amount.Value = GatherableSupplies.Value.EndGather();
            }
            else
            {
                // For any non-success end (Failure / Aborted), abort the gather.
                GatherableSupplies.Value.AbortGather();
            }
        }
    }
}
