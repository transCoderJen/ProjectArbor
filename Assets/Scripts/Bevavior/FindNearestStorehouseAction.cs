using System;
using System.Collections.Generic;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Units;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Find Nearest Storehouse", story: "[Unit] finds nearest [Storehouse]", category: "Action/Units", id: "176d5ff90d18328439b9d56fff340a95")]
    public partial class FindNearestStorehouseAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> Storehouse;
        [SerializeReference] public BlackboardVariable<float> SearchRadius = new(10);
        [SerializeReference] public BlackboardVariable<BuildingSO> StorehouseBuilding;

        protected override Status OnStart()
        {
            Collider[] colliders = Physics.OverlapSphere(
                Unit.Value.transform.position,
                SearchRadius.Value,
                LayerMask.GetMask("Buildings"));

            List<BaseBuilding> nearbyStorehouses = new();

            foreach (Collider collider in colliders)
            {
                if (!collider.TryGetComponent(out BaseBuilding building))
                    continue;

                if (building.UnitSO == null)
                    continue;

                if (StorehouseBuilding.Value == null)
                    return Status.Failure;

                if (building.UnitSO == StorehouseBuilding.Value)
                {
                    nearbyStorehouses.Add(building);
                }
            }

            if (nearbyStorehouses.Count == 0)
                return Status.Failure;

            Storehouse.Value = nearbyStorehouses[0].gameObject;

            return Status.Success;
        }
    }
}