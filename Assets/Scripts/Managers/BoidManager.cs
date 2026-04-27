using UnityEngine;
using UnityEngine.AI;

namespace ShiftedSignal.Garden.Managers
{
    public class BugBoidManager : MonoBehaviour
    {
        public static BugBoidManager Instance { get; private set; }

        [Header("Neighbor Detection")]
        public float NeighborRadius = 4f;
        public LayerMask BugLayer;

        [Header("Boid Weights")]
        public float SeparationWeight = 2.5f;
        public float AlignmentWeight = 0.25f;
        public float CohesionWeight = 0.15f;
        public float ChaseWeight = 8f;
        public float BuzzWeight = 0.75f;

        [Header("Chase Control")]
        [Range(0f, 1f)] public float PlayerPull = 0.65f;
        public float DirectChaseDistance = 8f;

        [Header("Movement")]
        public float ChaseStepDistance = 5f;
        public float RepathRate = 0.1f;
        public float NavMeshSampleDistance = 3f;

        [Header("Buzz")]
        public float BuzzSpeed = 3f;

        [Header("NavMesh Avoidance")]
        public ObstacleAvoidanceType AvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        [Range(0, 99)] public int MinAvoidancePriority = 20;
        [Range(0, 99)] public int MaxAvoidancePriority = 80;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnValidate()
        {
            NeighborRadius = Mathf.Max(0.1f, NeighborRadius);
            DirectChaseDistance = Mathf.Max(0.1f, DirectChaseDistance);
            ChaseStepDistance = Mathf.Max(0.1f, ChaseStepDistance);
            RepathRate = Mathf.Max(0.02f, RepathRate);
            NavMeshSampleDistance = Mathf.Max(0.1f, NavMeshSampleDistance);

            if (MaxAvoidancePriority < MinAvoidancePriority)
                MaxAvoidancePriority = MinAvoidancePriority;
        }
    }
}