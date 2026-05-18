using ShiftedSignal.Garden.Managers;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.BugSpace
{
    public class EnemyBug : Enemy
    {
        #region States

        public BugIdleState IdleState { get; private set; }
        public BugChaseState ChaseState { get; private set; }
        public BugCropState CropState { get; private set; }

        #endregion

        [Header("Hover")]
        [SerializeField] private Vector2 AmplitudeRange = new Vector2(1f, 8f);
        [SerializeField] private float Frequency = 1f;
        [SerializeField] private float Offset = 6f;
        [SerializeField] private float MinimumGroundDistance = 2f;

        [Header("Hover Speed Variation")]
        [SerializeField] private Vector2 VerticalSpeedRange = new Vector2(0.8f, 2f);
        [SerializeField] private Vector2 SpeedChangeIntervalRange = new Vector2(2f, 5f);
        [SerializeField] private float SpeedSmoothTime = 1.5f;
        [SerializeField] private float AmplitudeSmoothTime = 1.5f;

        public int BoidIndex { get; set; } = -1;
        public BugBoidMode BoidMode { get; private set; } = BugBoidMode.Idle;
        public Vector3 BoidTarget { get; private set; }
        public Vector3 BoidDirection { get; set; }
        public Vector3 BoidDestination { get; set; }

        private float randomHeightOffset;
        private float hoverTime;

        private float amplitude;
        private float targetAmplitude;

        private float verticalSpeed;
        private float targetVerticalSpeed;

        private float speedTimer;

        protected override void Awake()
        {
            base.Awake();

            randomHeightOffset = Random.Range(0f, 2f * math.PI);

            amplitude = Random.Range(AmplitudeRange.x, AmplitudeRange.y);
            targetAmplitude = amplitude;

            verticalSpeed = Random.Range(VerticalSpeedRange.x, VerticalSpeedRange.y);
            targetVerticalSpeed = verticalSpeed;

            SetNewSpeedTimer();

            Agent.updateUpAxis = false;

            IdleState = new BugIdleState(this, StateMachine, "Idle", this);
            ChaseState = new BugChaseState(this, StateMachine, "Move", this);
            CropState = new BugCropState(this, StateMachine, "Move", this);
        }

        protected override void Start()
        {
            base.Start();

            Agent.updateUpAxis = false;
            Agent.updateRotation = false;

            StateMachine.Initialize(IdleState);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (BugBoidJobManager.Instance != null)
                BoidIndex = BugBoidJobManager.Instance.RegisterBug(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (BugBoidJobManager.Instance != null)
                BugBoidJobManager.Instance.UnregisterBug(this);

            BoidIndex = -1;
        }

        public override bool CanBeStunned()
        {
            if (base.CanBeStunned())
            {
                // StateMachine.ChangeState(stunnedState); TODO: Add stunned state.
                return true;
            }

            return false;
        }

        public override void Die()
        {
            base.Die();
        }

        public void SetBoidData(BugBoidMode mode, Vector3 target)
        {
            BoidMode = mode;
            BoidTarget = target;
        }

        public void ApplyBoidAgentSettings()
        {
            BugBoidManager boids = BugBoidManager.Instance;

            if (boids == null)
                return;

            Agent.obstacleAvoidanceType = boids.AvoidanceType;
            Agent.avoidancePriority = Random.Range(
                boids.MinAvoidancePriority,
                boids.MaxAvoidancePriority + 1
            );
        }

        public void Hover()
        {
            UpdateHoverSpeed();

            hoverTime += Time.deltaTime * verticalSpeed * Frequency;

            float targetY = Mathf.Sin(hoverTime + randomHeightOffset) * amplitude + Offset;

            GroundDist = Mathf.Max(MinimumGroundDistance, targetY);
        }

        private void UpdateHoverSpeed()
        {
            speedTimer -= Time.deltaTime;

            if (speedTimer <= 0f)
            {
                targetVerticalSpeed = Random.Range(
                    VerticalSpeedRange.x,
                    VerticalSpeedRange.y
                );

                targetAmplitude = Random.Range(
                    AmplitudeRange.x,
                    AmplitudeRange.y
                );

                SetNewSpeedTimer();
            }

            verticalSpeed = Mathf.Lerp(
                verticalSpeed,
                targetVerticalSpeed,
                Time.deltaTime * SpeedSmoothTime
            );

            amplitude = Mathf.Lerp(
                amplitude,
                targetAmplitude,
                Time.deltaTime * AmplitudeSmoothTime
            );
        }

        private void SetNewSpeedTimer()
        {
            speedTimer = Random.Range(
                SpeedChangeIntervalRange.x,
                SpeedChangeIntervalRange.y
            );
        }

        protected override void OnValidate()
        {
            AmplitudeRange.x = Mathf.Max(0f, AmplitudeRange.x);
            AmplitudeRange.y = Mathf.Max(AmplitudeRange.x, AmplitudeRange.y);

            VerticalSpeedRange.x = Mathf.Max(0f, VerticalSpeedRange.x);
            VerticalSpeedRange.y = Mathf.Max(VerticalSpeedRange.x, VerticalSpeedRange.y);

            SpeedChangeIntervalRange.x = Mathf.Max(0.01f, SpeedChangeIntervalRange.x);
            SpeedChangeIntervalRange.y = Mathf.Max(SpeedChangeIntervalRange.x, SpeedChangeIntervalRange.y);

            Frequency = Mathf.Max(0f, Frequency);
            MinimumGroundDistance = Mathf.Max(0f, MinimumGroundDistance);
        }
    }
}