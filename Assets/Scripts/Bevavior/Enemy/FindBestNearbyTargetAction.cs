using System;
using System.Collections.Generic;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Interfaces;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Find Best Nearby Target",
        story: "[Self] finds the best nearby [Target] and prioritizes [FarmBuilding]",
        category: "Unit/Enemy",
        id: "e6a3ba28301791394ca9c50c2e7c9cc6")]
    public partial class FindBestNearbyTargetAction : Action
    {
        [SerializeReference]
        public BlackboardVariable<GameObject> Self;

        [SerializeReference]
        public BlackboardVariable<List<GameObject>> NearbyEnemies;

        [SerializeReference]
        public BlackboardVariable<GameObject> Target;

        [SerializeReference]
        public BlackboardVariable<BuildingSO> FarmBuilding;

        protected override Status OnStart()
        {
            if (Self?.Value == null ||
                NearbyEnemies?.Value == null ||
                Target == null ||
                FarmBuilding?.Value == null)
            {
                return Status.Failure;
            }

            Target.Value = null;

            GameObject bestTarget = null;

            foreach (GameObject nearbyEnemy in NearbyEnemies.Value)
            {
                if (!IsValidTarget(nearbyEnemy))
                    continue;

                if (IsFarmTarget(nearbyEnemy))
                {
                    Target.Value = nearbyEnemy;

                    Debug.Log(
                        $"{Self.Value.name}: prioritizing Farm target " +
                        $"{nearbyEnemy.name}");

                    return Status.Success;
                }

                // NearbyEnemies is already sorted by your existing
                // target-priority comparer, so the first valid
                // non-Farm target remains the best fallback.
                if (bestTarget == null)
                    bestTarget = nearbyEnemy;
            }

            if (bestTarget == null)
            {
                Debug.Log(
                    $"{Self.Value.name}: no valid nearby target found.");

                return Status.Failure;
            }

            Target.Value = bestTarget;

            Debug.Log(
                $"{Self.Value.name}: selected nearby target " +
                $"{bestTarget.name}");

            return Status.Success;
        }

        private bool IsValidTarget(GameObject target)
        {
            return EnemyTargetUtility.IsValidNearbyTarget(
                Self.Value,
                target);
        }

        private bool IsFarmTarget(GameObject target)
        {
            BaseBuilding building =
                target.GetComponentInParent<BaseBuilding>();

            if (building == null)
                return false;

            return building.UnitSO == FarmBuilding.Value;
        }
    }
}