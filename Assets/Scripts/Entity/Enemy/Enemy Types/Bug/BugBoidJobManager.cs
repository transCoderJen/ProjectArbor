using System.Collections.Generic;
using ShiftedSignal.Garden.Managers;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.BugSpace
{
    /// <summary>
    /// Calculates bug boid movement data using Unity's Job System.
    /// NavMeshAgent movement is still applied by each bug/state on the main thread.
    /// </summary>
    public class BugBoidJobManager : MonoBehaviour
    {
        public static BugBoidJobManager Instance { get; private set; }

        private readonly List<EnemyBug> bugs = new();

        private NativeArray<float3> positions;
        private NativeArray<float3> velocities;
        private NativeArray<float3> targets;
        private NativeArray<float3> outputDirections;
        private NativeArray<float3> outputDestinations;
        private NativeArray<int> modes;

        private int capacity;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            DisposeArrays();

            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            BugBoidManager boids = BugBoidManager.Instance;

            if (boids == null || bugs.Count == 0)
                return;

            EnsureCapacity(bugs.Count);
            CopyBugData();

            BugBoidJob job = new BugBoidJob
            {
                Positions = positions,
                Velocities = velocities,
                Targets = targets,
                Modes = modes,
                OutputDirections = outputDirections,
                OutputDestinations = outputDestinations,

                Count = bugs.Count,

                NeighborRadius = boids.NeighborRadius,
                SeparationWeight = boids.SeparationWeight,
                AlignmentWeight = boids.AlignmentWeight,
                CohesionWeight = boids.CohesionWeight,
                BuzzWeight = boids.BuzzWeight,
                ChaseWeight = boids.ChaseWeight,
                ChaseStepDistance = boids.ChaseStepDistance,
                PlayerPull = boids.PlayerPull,
                DirectChaseDistance = boids.DirectChaseDistance,

                Time = Time.time,
                BuzzSpeed = boids.BuzzSpeed
            };

            JobHandle handle = job.Schedule(bugs.Count, 32);
            handle.Complete();

            ApplyBugData();
        }

        public int RegisterBug(EnemyBug bug)
        {
            if (bug == null)
                return -1;

            if (bugs.Contains(bug))
                return bugs.IndexOf(bug);

            bugs.Add(bug);
            return bugs.Count - 1;
        }

        public void UnregisterBug(EnemyBug bug)
        {
            if (bug == null)
                return;

            int index = bugs.IndexOf(bug);

            if (index < 0)
                return;

            bugs.RemoveAt(index);

            for (int i = 0; i < bugs.Count; i++)
            {
                if (bugs[i] == null)
                    continue;

                bugs[i].BoidIndex = i;
            }
        }

        private void CopyBugData()
        {
            for (int i = 0; i < bugs.Count; i++)
            {
                EnemyBug bug = bugs[i];

                if (bug == null)
                    continue;

                bug.BoidIndex = i;

                positions[i] = bug.transform.position;
                velocities[i] = bug.Agent.velocity;
                targets[i] = bug.BoidTarget;
                modes[i] = (int)bug.BoidMode;
            }
        }

        private void ApplyBugData()
        {
            for (int i = 0; i < bugs.Count; i++)
            {
                EnemyBug bug = bugs[i];

                if (bug == null)
                    continue;

                bug.BoidDirection = outputDirections[i];
                bug.BoidDestination = outputDestinations[i];
            }
        }

        private void EnsureCapacity(int requiredCapacity)
        {
            if (capacity >= requiredCapacity)
                return;

            DisposeArrays();

            capacity = Mathf.NextPowerOfTwo(requiredCapacity);

            positions = new NativeArray<float3>(capacity, Allocator.Persistent);
            velocities = new NativeArray<float3>(capacity, Allocator.Persistent);
            targets = new NativeArray<float3>(capacity, Allocator.Persistent);
            outputDirections = new NativeArray<float3>(capacity, Allocator.Persistent);
            outputDestinations = new NativeArray<float3>(capacity, Allocator.Persistent);
            modes = new NativeArray<int>(capacity, Allocator.Persistent);
        }

        private void DisposeArrays()
        {
            if (positions.IsCreated)
                positions.Dispose();

            if (velocities.IsCreated)
                velocities.Dispose();

            if (targets.IsCreated)
                targets.Dispose();

            if (outputDirections.IsCreated)
                outputDirections.Dispose();

            if (outputDestinations.IsCreated)
                outputDestinations.Dispose();

            if (modes.IsCreated)
                modes.Dispose();
        }

        [BurstCompile]
        private struct BugBoidJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> Positions;
            [ReadOnly] public NativeArray<float3> Velocities;
            [ReadOnly] public NativeArray<float3> Targets;
            [ReadOnly] public NativeArray<int> Modes;

            [WriteOnly] public NativeArray<float3> OutputDirections;
            [WriteOnly] public NativeArray<float3> OutputDestinations;

            public int Count;

            public float NeighborRadius;
            public float SeparationWeight;
            public float AlignmentWeight;
            public float CohesionWeight;
            public float BuzzWeight;
            public float ChaseWeight;
            public float ChaseStepDistance;
            public float PlayerPull;
            public float DirectChaseDistance;

            public float Time;
            public float BuzzSpeed;

            public void Execute(int index)
            {
                float3 position = Positions[index];
                float3 target = Targets[index];

                float3 separation = float3.zero;
                float3 alignment = float3.zero;
                float3 cohesion = float3.zero;

                int neighborCount = 0;

                for (int i = 0; i < Count; i++)
                {
                    if (i == index)
                        continue;

                    float3 toSelf = position - Positions[i];
                    toSelf.y = 0f;

                    float distance = math.length(toSelf);

                    if (distance <= 0.01f || distance > NeighborRadius)
                        continue;

                    separation += math.normalize(toSelf) / distance;

                    float3 velocity = Velocities[i];
                    velocity.y = 0f;

                    if (math.lengthsq(velocity) > 0.01f)
                        alignment += math.normalize(velocity);

                    cohesion += Positions[i];
                    neighborCount++;
                }

                if (neighborCount > 0)
                {
                    alignment /= neighborCount;

                    float3 center = cohesion / neighborCount;
                    cohesion = center - position;
                    cohesion.y = 0f;

                    if (math.lengthsq(cohesion) > 0.01f)
                        cohesion = math.normalize(cohesion);
                }

                float noiseX = noise.cnoise(new float2(Time * BuzzSpeed, index * 17.13f));
                float noiseZ = noise.cnoise(new float2(index * 23.71f, Time * BuzzSpeed));

                float3 buzz = new float3(noiseX, 0f, noiseZ);

                if (math.lengthsq(buzz) > 0.01f)
                    buzz = math.normalize(buzz);

                float3 chase = target - position;
                chase.y = 0f;

                if (math.lengthsq(chase) > 0.01f)
                    chase = math.normalize(chase);

                float chaseWeight = Modes[index] == 0 ? 0f : ChaseWeight;

                float3 direction =
                    separation * SeparationWeight +
                    alignment * AlignmentWeight +
                    cohesion * CohesionWeight +
                    buzz * BuzzWeight +
                    chase * chaseWeight;

                direction.y = 0f;

                if (math.lengthsq(direction) <= 0.01f)
                    direction = chase;

                if (math.lengthsq(direction) > 0.01f)
                    direction = math.normalize(direction);

                float distanceToTarget = math.distance(position, target);

                float3 destination;

                if (Modes[index] == 0)
                {
                    destination = position + direction * ChaseStepDistance;
                }
                else if (distanceToTarget > DirectChaseDistance)
                {
                    destination = target;
                }
                else
                {
                    float3 boidDestination = position + direction * ChaseStepDistance;
                    destination = math.lerp(boidDestination, target, PlayerPull);
                }

                OutputDirections[index] = direction;
                OutputDestinations[index] = destination;
            }
        }
    }

    public enum BugBoidMode
    {
        Idle = 0,
        Chase = 1,
        Crop = 2
    }
}