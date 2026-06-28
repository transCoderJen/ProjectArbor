using ShiftedSignal.Garden.Environment;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Fina Getherable Supply", story: "[Unit] finds nearest [Supply]", category: "Action/Units", id: "26eb8a2cca0efb0ef4d393e39217acb4")]
    public partial class FinaGetherableSupplyAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<GatherableSupply> Supply;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        [SerializeReference] public BlackboardVariable<float> SearchRadius = new(70f);

        protected override Status OnStart()
        {
            if (Unit.Value == null)
                return Status.Failure;

            Collider[] colliders = Physics.OverlapSphere(
                Unit.Value.transform.position,
                SearchRadius.Value,
                LayerMask.GetMask("Supplies"));

            GatherableSupply bestSupply = null;
            float bestDistanceSqr = float.MaxValue;

            foreach (Collider collider in colliders)
            {
                if (!collider.TryGetComponent(out GatherableSupply gatherableSupply))
                    continue;

                if (gatherableSupply.IsBusy)
                    continue;

                if (gatherableSupply.Amount <= 0)
                    continue;

                float distanceSqr =
                    (gatherableSupply.transform.position - Unit.Value.transform.position).sqrMagnitude;

                if (distanceSqr >= bestDistanceSqr)
                    continue;

                bestSupply = gatherableSupply;
                bestDistanceSqr = distanceSqr;
            }

            if (bestSupply == null)
            {
                Supply.Value = null;
                TargetGameObject.Value = null;
                return Status.Failure;
            }

            Supply.Value = bestSupply;
            TargetGameObject.Value = bestSupply.gameObject;

            return Status.Success;
        }
    }
}

