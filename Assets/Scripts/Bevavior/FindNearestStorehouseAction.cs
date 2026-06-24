using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Buildable;
using System.Collections.Generic;
using ShiftedSignal.Garden.Units;

namespace ShiftedSignal.Garden.Behavior
{  
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Find Nearest Storehouse", story: "[Unit] finds nearest [Storehouse]", category: "Action/Units", id: "176d5ff90d18328439b9d56fff340a95")]
    public partial class FindNearestStorehouseAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> Storehouse;
        [SerializeReference] public BlackboardVariable<float> SearchRadius = new(10);
        [SerializeReference] public BlackboardVariable<UnitSO> StorehouseBuilding;

        protected override Status OnStart()
        {
            Collider[] colliders = Physics.OverlapSphere(
                Unit.Value.transform.position, 
                SearchRadius.Value, 
                LayerMask.GetMask("Buildings"));

            List<BaseBuilding> nearbyStorehouses = new();


            foreach (Collider collider in colliders)
            {
                Debug.Log($"Checking collider: {collider.name}");

                if (!collider.TryGetComponent(out BaseBuilding building))
                {
                    Debug.Log($"{collider.name} has no BaseBuilding");
                    continue;
                }

                if (building.UnitSO == null)
                {
                    Debug.LogWarning($"{building.name} has no UnitSO assigned");
                    continue;
                }

                if (StorehouseBuilding.Value == null)
                {
                    Debug.LogError("CommandPostBuilding blackboard variable is null");
                    return Status.Failure;
                }

                Debug.Log($"Building UnitSO: {building.UnitSO.name}");
                Debug.Log($"Looking for UnitSO: {StorehouseBuilding.Value.name}");

                if (building.UnitSO == StorehouseBuilding.Value)
                {
                    nearbyStorehouses.Add(building);
                    Debug.Log($"Found matching storehouse: {building.name}");
                }
            }

            if (nearbyStorehouses.Count == 0)
            {
                Debug.Log("Couldn't find a storehouse");
                return Status.Failure;
            }

            Storehouse.Value = nearbyStorehouses[0].gameObject;

            return Status.Success;
        }
    }
}