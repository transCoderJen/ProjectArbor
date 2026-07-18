using System;
using System.Collections.Generic;
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
        story: "[Self] finds the best nearby [Target]",
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

        protected override Status OnStart()
        {
            if (Self?.Value == null ||
                NearbyEnemies?.Value == null ||
                Target == null)
            {
                return Status.Failure;
            }

            Target.Value = null;

            foreach (GameObject nearbyEnemy in NearbyEnemies.Value)
            {
                if (!IsValidTarget(nearbyEnemy))
                    continue;

                Target.Value = nearbyEnemy;
                return Status.Success;
            }

            return Status.Failure;
        }

        private bool IsValidTarget(GameObject target)
        {
            if (target == null || target == Self.Value)
                return false;

            if (!target.TryGetComponent(out IDamageable damageable))
                damageable = target.GetComponentInParent<IDamageable>();

            if (damageable == null || damageable.CurrentHealth <= 0)
                return false;

            return true;
        }
    }
}