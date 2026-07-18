using System;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Units;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Find Blocking Target Toward Farm", story: "[Agent] finds a structure blocking the path the [Farm] and sets [Target]", category: "Unit/Enemy", id: "eaea7269d51263ea15a9d3bf339b11ca")]
    public partial class FindBlockingTargetTowardFarmAction : Action
    {
        [SerializeReference]
        public BlackboardVariable<GameObject> Agent;

        [SerializeReference]
        public BlackboardVariable<GameObject> Farm;

        [SerializeReference]
        public BlackboardVariable<GameObject> Target;

        [SerializeReference]
        public BlackboardVariable<float> SearchRadius = new(6f);

        private NavMeshAgent navMeshAgent;
        private AbstractCommandable self;

        protected override Status OnStart()
        {
            Target.Value = null;

            if (!HasValidInputs())
                return Status.Failure;

            Vector3 farmPosition = GetFarmTargetPosition();

            NavMeshPath path = new NavMeshPath();
            bool calculated = navMeshAgent.CalculatePath(farmPosition, path);

            Debug.Log(
                $"{Agent.Value.name}: Blocking-target path calculation: " +
                $"Calculated={calculated}, " +
                $"Status={path.status}, " +
                $"Corners={path.corners.Length}");

            if (!calculated ||
                path.status != NavMeshPathStatus.PathPartial ||
                path.corners.Length == 0)
            {
                Debug.Log(
                    $"{Agent.Value.name}: No partial path available " +
                    $"for blocking-target search.");

                return Status.Failure;
            }

            Vector3 blockedPoint = path.corners[^1];

            Debug.Log(
                $"{Agent.Value.name}: Searching for blocking structures " +
                $"around {blockedPoint}");

            GameObject blockingTarget = FindBestBlockingTarget(blockedPoint);

            if (blockingTarget == null)
            {
                Debug.Log(
                    $"{Agent.Value.name}: No blocking structure found.");

                return Status.Failure;
            }

            Target.Value = blockingTarget;

            Debug.Log(
                $"{Agent.Value.name}: Selected blocking target: " +
                $"{blockingTarget.name}");

            return Status.Success;
        }

        private bool HasValidInputs()
        {
            if (Agent?.Value == null ||
                Farm?.Value == null ||
                Target == null)
            {
                return false;
            }

            if (!Agent.Value.TryGetComponent(out navMeshAgent))
                return false;

            if (!Agent.Value.TryGetComponent(out self))
                return false;

            return navMeshAgent.isOnNavMesh;
        }

        private GameObject FindBestBlockingTarget(Vector3 blockedPoint)
        {
            Collider[] hits = Physics.OverlapSphere(
                blockedPoint,
                SearchRadius.Value,
                ~0,
                QueryTriggerInteraction.Collide);

            GameObject bestTarget = null;
            float bestDistanceSqr = Mathf.Infinity;

            foreach (Collider hit in hits)
            {
                IDamageable damageable =
                    hit.GetComponentInParent<IDamageable>();

                if (!IsValidBlockingTarget(damageable))
                    continue;

                GameObject targetObject =
                    damageable.Transform.gameObject;

                float distanceSqr =
                    (damageable.TargetPoint - blockedPoint).sqrMagnitude;

                Debug.Log(
                    $"{Agent.Value.name}: Blocking candidate " +
                    $"{targetObject.name}, " +
                    $"Owner={damageable.Owner}, " +
                    $"Distance={Mathf.Sqrt(distanceSqr):F2}");

                if (distanceSqr >= bestDistanceSqr)
                    continue;

                bestDistanceSqr = distanceSqr;
                bestTarget = targetObject;
            }

            return bestTarget;
        }

        private bool IsValidBlockingTarget(IDamageable damageable)
        {
            if (damageable == null)
                return false;

            if (damageable is not Component component ||
                component == null)
            {
                return false;
            }

            if (damageable == self)
                return false;

            if (damageable.CurrentHealth <= 0)
                return false;

            if (!DamageRules.CanDamage(self.Owner, damageable.Owner))
                return false;

            // Includes fences if they inherit from BaseBuilding.
            return component.GetComponentInParent<BaseBuilding>() != null;
        }

        private Vector3 GetFarmTargetPosition()
        {
            if (Farm.Value.TryGetComponent(out Collider farmCollider))
            {
                return farmCollider.ClosestPoint(
                    navMeshAgent.transform.position);
            }

            return Farm.Value.transform.position;
        }
    }
}

