using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections.Generic;
using UnityEngine.AI;
using ShiftedSignal.Garden.Interfaces;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Respond To Defense Assignment", story: "[Agent] responds to [DefenseTarget] near [DefenseBuilding]", category: "Action/Units", id: "9ffbde7ae724de9d0db4110ac0ab58b9")]
    public partial class RespondToDefenseAssignmentAction : Action
    {
        [SerializeReference]
        public BlackboardVariable<GameObject> Agent;

        [SerializeReference]
        public BlackboardVariable<GameObject> DefenseBuilding;

        [SerializeReference]
        public BlackboardVariable<GameObject> DefenseTarget;

        [SerializeReference]
        public BlackboardVariable<List<GameObject>> NearbyEnemies;

        private NavMeshAgent navMeshAgent;

        protected override Status OnStart()
        {
            if (Agent == null ||
                Agent.Value == null)
            {
                return Status.Failure;
            }

            if (!Agent.Value.TryGetComponent(
                    out navMeshAgent))
            {
                return Status.Failure;
            }

            if (!navMeshAgent.isOnNavMesh)
                return Status.Failure;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (navMeshAgent == null ||
                !navMeshAgent.isOnNavMesh)
            {
                return Status.Failure;
            }

            /*
                * A local enemy appeared.
                *
                * Yield so the unit's normal combat logic
                * can acquire and fight it.
                */
            if (HasValidNearbyEnemy())
            {
                return Status.Failure;
            }

            GameObject destination =
                GetDefenseDestination();

            if (destination == null)
            {
                return Status.Failure;
            }

            Vector3 targetPosition =
                GetTargetPosition(destination);

            if (Vector3.Distance(
                    navMeshAgent.transform.position,
                    targetPosition) <=
                navMeshAgent.stoppingDistance)
            {
                return Status.Success;
            }

            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(
                targetPosition);

            return Status.Running;
        }

        private GameObject GetDefenseDestination()
        {
            /*
                * Prefer the attacker while it is
                * still alive.
                */
            if (DefenseTarget != null &&
                DefenseTarget.Value != null)
            {
                IDamageable damageable =
                    DefenseTarget.Value
                        .GetComponentInParent<IDamageable>();

                if (damageable != null &&
                    damageable.CurrentHealth > 0)
                {
                    return DefenseTarget.Value;
                }
            }

            /*
                * Otherwise continue toward the
                * assigned building.
                */
            if (DefenseBuilding != null &&
                DefenseBuilding.Value != null)
            {
                return DefenseBuilding.Value;
            }

            return null;
        }

        private bool HasValidNearbyEnemy()
        {
            if (NearbyEnemies == null ||
                NearbyEnemies.Value == null)
            {
                return false;
            }

            foreach (GameObject enemy
                        in NearbyEnemies.Value)
            {
                if (enemy == null)
                    continue;

                IDamageable damageable =
                    enemy.GetComponentInParent<IDamageable>();

                if (damageable == null ||
                    damageable.CurrentHealth <= 0)
                {
                    continue;
                }

                /*
                    * Don't interrupt ourselves merely
                    * because the actual DefenseTarget
                    * is also in NearbyEnemies.
                    */
                if (DefenseTarget != null &&
                    DefenseTarget.Value == enemy)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private Vector3 GetTargetPosition(
            GameObject target)
        {
            Collider collider =
                target.GetComponentInChildren<Collider>();

            if (collider != null)
            {
                return collider.ClosestPoint(
                    navMeshAgent.transform.position);
            }

            return target.transform.position;
        }

        protected override void OnEnd()
        {
            if (navMeshAgent != null &&
                navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = false;
            }
        }
    }
}