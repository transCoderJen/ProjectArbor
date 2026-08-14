using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Interfaces;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;
using ShiftedSignal.Garden.Behavior;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Move to Farm Until Target Found", story: "[Agent] moves to [Farm] until a [target] appears in [NearbyEnemies]", category: "Unit/Enemy", id: "b7dba87ae2b26dd9160a92400986d974")]
public partial class MoveToFarmUntilTargetFoundAction : Action
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    [SerializeReference]
    public BlackboardVariable<GameObject> Farm;

    [SerializeReference]
    public BlackboardVariable<List<GameObject>> NearbyEnemies;

    private NavMeshAgent navMeshAgent;

    protected override Status OnStart()
    {
        if (!HasValidInputs())
            return Status.Failure;

        Debug.Log($"{Agent.Value.name}: MoveToFarm START");

        if (HasNearbyEnemy())
            return Status.Failure;

        Vector3 targetPosition = GetTargetPosition();

        NavMeshPath path = new NavMeshPath();
        bool found = navMeshAgent.CalculatePath(targetPosition, path);

        Debug.Log(
            $"{Agent.Value.name}: " +
            $"CalculatePath={found}, " +
            $"Status={path.status}, " +
            $"Corners={path.corners.Length}");

        if (!found ||
            path.status == NavMeshPathStatus.PathInvalid ||
            path.status == NavMeshPathStatus.PathPartial)
        {
            Debug.Log(
                $"{Agent.Value.name}: Farm path unavailable. " +
                $"Returning Failure for obstacle targeting.");

            return Status.Failure;
        }

        if (HasReached(targetPosition))
            return Status.Success;

        navMeshAgent.isStopped = false;

        // Use the path that was already calculated.
        if (!navMeshAgent.SetPath(path))
        {
            Debug.Log($"{Agent.Value.name}: SetPath failed.");
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null ||
            Farm?.Value == null ||
            navMeshAgent == null ||
            !navMeshAgent.isOnNavMesh)
        {
            Debug.Log($"{Agent?.Value?.name ?? "Unknown Agent"}: Invalid agent");
            return Status.Failure;
        }

        if (HasNearbyEnemy())
        {
            Debug.Log($"{Agent.Value.name}: Enemy detected");
            return Status.Failure;
        }

        if (navMeshAgent.pathPending)
        {
            Debug.Log($"{Agent.Value.name}: Path pending");
            return Status.Running;
        }

        if (HasReachedDestination())
        {
            Debug.Log($"{Agent.Value.name}: Reached farm");
            return Status.Success;
        }

        if (!navMeshAgent.hasPath)
        {
            Debug.Log($"{Agent.Value.name}: No path");
            return Status.Failure;
        }

        if (navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.Log($"{Agent.Value.name}: Path INVALID");
            return Status.Failure;
        }

        if (navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            Debug.Log($"{Agent.Value.name}: Path PARTIAL");
            return Status.Failure;
        }

        Debug.Log($"{Agent.Value.name}: Following complete path");

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            navMeshAgent.ResetPath();
    }

    private bool HasValidInputs()
    {
        if (Agent?.Value == null || Farm?.Value == null)
            return false;

        if (!Agent.Value.TryGetComponent(out navMeshAgent))
            return false;

        return navMeshAgent.isOnNavMesh;
    }

    private bool HasNearbyEnemy()
    {
        if (NearbyEnemies?.Value == null)
            return false;

        foreach (GameObject nearbyTarget in NearbyEnemies.Value)
        {
            if (!EnemyTargetUtility.IsValidNearbyTarget(
                    Agent.Value,
                    nearbyTarget))
            {
                continue;
            }

            Debug.Log(
                $"{Agent.Value.name}: movement interrupted by " +
                $"{nearbyTarget.name}");

            return true;
        }

        return false;
    }

    private bool IsValidNearbyTarget(GameObject target)
    {
        if (target == null || target == Agent?.Value)
            return false;

        // Fences should not interrupt movement toward the Farm.
        // They are only handled by the path-clearing branch.
        if (target.GetComponentInParent<FencePost2D>() != null)
            return false;

        IDamageable damageable =
            target.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return false;

        return damageable.CurrentHealth > 0;
    }

    private bool HasReachedDestination()
    {
        if (float.IsInfinity(navMeshAgent.remainingDistance))
            return false;

        return navMeshAgent.remainingDistance <=
                navMeshAgent.stoppingDistance;
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

