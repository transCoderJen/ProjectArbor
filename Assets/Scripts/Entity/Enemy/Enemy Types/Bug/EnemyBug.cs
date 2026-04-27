using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.EnemySpace.EnemyTypes.BugSpace
{
    public class EnemyBug : Enemy
    {
        #region States

        public BugIdleState IdleState { get; private set; }
        public BugChaseState ChaseState { get; private set; }
        #endregion

        [Header("Hover")]
        [SerializeField] private float amplitude = 4f;
        [SerializeField] private float frequency = 1f;
        [SerializeField] private float offset = 6f;

        [Header("Hover Speed Variation")]
        [SerializeField] private Vector2 verticalSpeedRange = new Vector2(1f, 10f);
        [SerializeField] private Vector2 speedChangeIntervalRange = new Vector2(.2f, 3f);
        [SerializeField] private float speedSmoothTime = 2f;

        private float randomHeightOffset;
        private float hoverTime;

        private float verticalSpeed;
        private float targetVerticalSpeed;

        private float speedTimer;

        protected override void Awake()
        {
            base.Awake();

            randomHeightOffset = UnityEngine.Random.Range(0f, 2f * math.PI);
            amplitude = UnityEngine.Random.Range(1f, 4f * math.PI);

            verticalSpeed = UnityEngine.Random.Range(
                verticalSpeedRange.x,
                verticalSpeedRange.y
            );

            targetVerticalSpeed = verticalSpeed;

            SetNewSpeedTimer();

            IdleState = new BugIdleState(this, StateMachine, "Idle", this);
            ChaseState = new BugChaseState(this, StateMachine, "Move", this);
        }

        protected override void Start()
        {
            base.Start();
            StateMachine.Initialize(IdleState);
        }

        protected override void Update()
        {
            base.Update();
        }

        public override bool CanBeStunned()
        {
            if (base.CanBeStunned())
            {
                // StateMachine.ChangeState(stunnedState); TODO add stunned state
                return true;
            }

            return false;
        }

        public override void Die()
        {
            base.Die();
        }

        public void Hover()
        {
            UpdateHoverSpeed();

            hoverTime += Time.deltaTime * verticalSpeed * frequency;

            float y = Mathf.Sin(hoverTime + randomHeightOffset) * amplitude + offset;
            GroundDist = Math.Max(2f, y);
        }

        private void UpdateHoverSpeed()
        {
            speedTimer -= Time.deltaTime;

            if (speedTimer <= 0f)
            {
                targetVerticalSpeed = UnityEngine.Random.Range(
                    verticalSpeedRange.x,
                    verticalSpeedRange.y
                );

                amplitude = UnityEngine.Random.Range(1f, 4f * math.PI);

                SetNewSpeedTimer();
            }

            verticalSpeed = Mathf.Lerp(
                verticalSpeed,
                targetVerticalSpeed,
                Time.deltaTime * speedSmoothTime
            );
        }

        private void SetNewSpeedTimer()
        {
            speedTimer = UnityEngine.Random.Range(
                speedChangeIntervalRange.x,
                speedChangeIntervalRange.y
            );
        }
    }
}