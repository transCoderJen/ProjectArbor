using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Interfaces;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Find Nearest Seed Source", story: "[Unit] finds nearest [SeedSource]", category: "Action/Units", id: "02724eb3a1a95ffdd1b8469f78ef6fe1")]
    public partial class FindNearestSeedSourceAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> SeedSource;
        [SerializeReference] public BlackboardVariable<float> SearchRadius = new(500f);

        protected override Status OnStart()
        {
            if (Unit.Value == null)
                return Status.Failure;

            Collider[] colliders = Physics.OverlapSphere(
                Unit.Value.transform.position,
                SearchRadius.Value,
                LayerMask.GetMask("Buildings"));

            ISeedSource bestSource = null;
            float bestDistanceSqr = float.MaxValue;

            foreach (Collider collider in colliders)
            {
                ISeedSource source =
                    collider.GetComponentInParent<ISeedSource>();

                if (source == null)
                    continue;

                if (!source.HasAnySeed)
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
            {
                SeedSource.Value = null;
                return Status.Failure;
            }

            SeedSource.Value = ((MonoBehaviour)bestSource).gameObject;

            return Status.Success;
        }
    }
}

