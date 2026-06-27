using ShiftedSignal.Garden.Units;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Interfaces;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Find Nearest Farm Supply Source", story: "[Unit] finds nearest [FarmSource] for [FarmTask]", category: "Action/Units", id: "d0570c68238e1f9d6420ecc9417c8768")]
    public partial class FindNearestFarmSupplySourceAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> FarmSource;
        [SerializeReference] public BlackboardVariable<FarmTaskType> FarmTask;
        [SerializeReference] public BlackboardVariable<float> SearchRadius = new(50f);

        protected override Status OnStart()
        {
            if (Unit.Value == null)
                return Status.Failure;

            FarmSupplyType requiredSupply = FarmTask.Value switch
            {
                FarmTaskType.Water => FarmSupplyType.Water,
                FarmTaskType.Fertilize => FarmSupplyType.Fertilizer,
                _ => FarmSupplyType.None
            };

            if (requiredSupply == FarmSupplyType.None)
                return Status.Failure;

            Collider[] colliders = Physics.OverlapSphere(
                Unit.Value.transform.position,
                SearchRadius.Value,
                LayerMask.GetMask("Buildings"));

            IFarmSupplySource bestSource = null;
            float bestDistanceSqr = float.MaxValue;

            foreach (Collider collider in colliders)
            {
                IFarmSupplySource source =
                    collider.GetComponentInParent<IFarmSupplySource>();

                if (source == null)
                    continue;

                if (source.SupplyType != requiredSupply)
                    continue;

                if (!source.CanProvide(1))
                    continue;

                MonoBehaviour behaviour = source as MonoBehaviour;

                if (behaviour == null)
                    continue;

                float distanceSqr =
                    (behaviour.transform.position - Unit.Value.transform.position).sqrMagnitude;

                if (distanceSqr >= bestDistanceSqr)
                    continue;

                bestSource = source;
                bestDistanceSqr = distanceSqr;
            }

            if (bestSource == null)
                return Status.Failure;

            FarmSource.Value = (bestSource as MonoBehaviour).gameObject;

            return Status.Success;
        }
    }
}

