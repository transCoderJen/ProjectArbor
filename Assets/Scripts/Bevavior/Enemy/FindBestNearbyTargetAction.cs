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

            Debug.Log(
                $"[TARGET SEARCH] {Self.Value.name} starting search | " +
                $"NearbyCount={NearbyEnemies.Value.Count} | " +
                $"PreviousTarget={(Target.Value != null ? Target.Value.name : "NULL")}");

            Target.Value = null;

            Debug.Log(
                $"[TARGET SET] {Self.Value.name} | " +
                $"Source=FindBestNearbyTarget Clear | " +
                $"Target=NULL");

            GameObject bestTarget = null;

            foreach (GameObject nearbyEnemy in NearbyEnemies.Value)
            {
                if (nearbyEnemy == null)
                    continue;

                IDamageable damageable =
                    nearbyEnemy.GetComponentInParent<IDamageable>();

                BaseBuilding building =
                    nearbyEnemy.GetComponentInParent<BaseBuilding>();

                bool valid =
                    IsValidTarget(nearbyEnemy);

                bool isFarm =
                    IsFarmTarget(nearbyEnemy);

                Debug.Log(
                    $"[TARGET CANDIDATE] {Self.Value.name} | " +
                    $"Candidate={nearbyEnemy.name} | " +
                    $"Owner={(damageable != null ? damageable.Owner.ToString() : "NO IDAMAGEABLE")} | " +
                    $"Building={(building != null ? building.name : "NULL")} | " +
                    $"Valid={valid} | " +
                    $"IsFarm={isFarm}");

                if (!valid)
                    continue;

                if (isFarm)
                {
                    Debug.Log(
                        $"[TARGET SET] {Self.Value.name} | " +
                        $"Source=FindBestNearbyTarget Farm Priority | " +
                        $"Target={nearbyEnemy.name} | " +
                        $"Owner={(damageable != null ? damageable.Owner.ToString() : "NO IDAMAGEABLE")}");

                    Target.Value = nearbyEnemy;

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
                    $"[TARGET SEARCH] {Self.Value.name} | " +
                    $"No valid nearby target found.");

                return Status.Failure;
            }

            IDamageable bestDamageable =
                bestTarget.GetComponentInParent<IDamageable>();

            BaseBuilding bestBuilding =
                bestTarget.GetComponentInParent<BaseBuilding>();

            Debug.Log(
                $"[TARGET SET] {Self.Value.name} | " +
                $"Source=FindBestNearbyTarget Fallback | " +
                $"Target={bestTarget.name} | " +
                $"Owner={(bestDamageable != null ? bestDamageable.Owner.ToString() : "NO IDAMAGEABLE")} | " +
                $"Building={(bestBuilding != null ? bestBuilding.name : "NULL")}");

            Target.Value = bestTarget;

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