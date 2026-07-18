using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Move to Farm Until Target Found", story: "[Agent] moves to [Farm] until a [target] appears in [NearbyTargets]", category: "Unit/Enemy", id: "b7dba87ae2b26dd9160a92400986d974")]
public partial class MoveToFarmUntilTargetFoundAction : Action
    {
        [SerializeReference]
        public BlackboardVariable<GameObject> Agent;

        [SerializeReference]
        public BlackboardVariable<GameObject> Farm;

        [SerializeReference]
        public BlackboardVariable<List<GameObject>> NearbyTargets;

        private NavMeshAgent navMeshAgent;

        protected override Status OnStart()
        {
            if (!HasValidInputs())
                return Status.Failure;

            if (HasNearbyTarget())
                return Status.Failure;

            Vector3 targetPosition = GetTargetPosition();

            if (HasReached(targetPosition))
                return Status.Success;

            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(targetPosition);

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Agent?.Value == null ||
                Farm?.Value == null ||
                navMeshAgent == null ||
                !navMeshAgent.isOnNavMesh)
            {
                return Status.Failure;
            }

            // Interrupt Farm navigation so the parent selector
            // reevaluates the combat branch.
            if (HasNearbyTarget())
            {
                navMeshAgent.ResetPath();
                return Status.Failure;
            }

            if (navMeshAgent.pathPending)
                return Status.Running;

            if (navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                return Status.Failure;

            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                return Status.Success;

            return Status.Running;
        }

        protected override void OnEnd()
        {
        }

        private bool HasValidInputs()
        {
            if (Agent?.Value == null || Farm?.Value == null)
                return false;

            if (!Agent.Value.TryGetComponent(out navMeshAgent))
                return false;

            return navMeshAgent.isOnNavMesh;
        }

        private bool HasNearbyTarget()
        {
            if (NearbyTargets?.Value == null)
                return false;

            foreach (GameObject target in NearbyTargets.Value)
            {
                if (target != null)
                    return true;
            }

            return false;
        }

        private bool HasReached(Vector3 targetPosition)
        {
            float stoppingDistance = navMeshAgent.stoppingDistance;

            return (navMeshAgent.transform.position - targetPosition).sqrMagnitude
                   <= stoppingDistance * stoppingDistance;
        }

        private Vector3 GetTargetPosition()
        {
            if (Farm.Value.TryGetComponent(out Collider targetCollider))
            {
                return targetCollider.ClosestPoint(
                    navMeshAgent.transform.position);
            }

            return Farm.Value.transform.position;
        }
    }


