using System;
using System.Collections;
using ShiftedSignal.Garden.Effects;
using ShiftedSignal.Garden.Stats;
using ShiftedSignal.Garden.UserInterface;
using ShiftedSignal.Garden.UserInterface.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace
{
    public enum RotationAdjustmentDirection
    {
        Up,
        UpRight,
        Right,
        DownRight,
        Down,
        DownLeft,
        Left,
        UpLeft
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(EntityFX))]
    public class Entity : MonoBehaviour
    {
        [Header("Grounding")]
        public LayerMask TerrainLayer;
        public float GroundDist;

        #region Components

        public UI UI;
        public Animator Anim { get; private set; }
        public Rigidbody Rb { get; private set; }
        public SpriteRenderer Sr { get; private set; }
        public CharacterHealth Health { get; private set; }
        public CapsuleCollider Cd { get; private set; }
        public EntityFX Fx { get; private set; }

        #endregion

        [Header("Movement")]
        [SerializeField] protected float MoveSpeed = 4f;

        [Header("Facing")]
        public Vector3 FacingDir = Vector3.right;
        public Vector2 LastFacingDir = Vector2.right;
        public RotationAdjustmentDirection RotationAdjustmentDirection = RotationAdjustmentDirection.Right;
        public bool FacingRight { get; private set; } = true;

        [Header("Knockback")]
        [SerializeField] protected float KnockbackForce = 6f;
        [SerializeField] protected float KnockbackDuration = 0.15f;
        protected bool IsKnocked;

        [Header("Combat")]
        public Transform AttackCheck;
        public float AttackCheckRadius = 0.5f;

        [Tooltip("The Distance in front of the player")]
        public float AttackCheckDistance = 1f;

        [Tooltip("How High the Checks Should Be")]
        public float CheckHeight = 1f;

        [SerializeField] public Vector3 RotationAdjustment { get; private set; }
        [SerializeField] public float AttackCoolDown;
        [HideInInspector] public float AttackTimer;

        public bool IsDead = false;

        public Action OnFacingChanged = delegate { };

        private float lastAnimX = 0f;
        private float lastAnimY = 0f;

        protected virtual void Awake()
        {
            Sr = GetComponentInChildren<SpriteRenderer>();
            Anim = GetComponentInChildren<Animator>();
            Rb = GetComponent<Rigidbody>();
            Health = GetComponent<CharacterHealth>();
            Cd = GetComponentInChildren<CapsuleCollider>();
            Fx = GetComponent<EntityFX>();

            if (Rb != null)
            {
                Rb.constraints =
                    RigidbodyConstraints.FreezeRotationX |
                    RigidbodyConstraints.FreezeRotationY |
                    RigidbodyConstraints.FreezeRotationZ;
            }
        }

        protected virtual void Start()
        {
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void Update()
        {
        }

        protected virtual void FixedUpdate()
        {
            SnapToTerrain();
        }

        private void SnapToTerrain()
        {
            Vector3 castPos = transform.position + Vector3.up;

            if (Physics.Raycast(
                    castPos,
                    Vector3.down,
                    out RaycastHit hit,
                    Mathf.Infinity,
                    TerrainLayer))
            {
                Vector3 movePos = transform.position;
                movePos.y = hit.point.y + GroundDist;
                transform.position = movePos;
            }
        }

        #region Movement

        public virtual void ApplyMovement(Vector2 input, bool normalized = true)
        {
            if (IsKnocked || IsDead || Rb == null)
                return;

            Vector3 moveDirection = Vector3.zero;

            if (input.sqrMagnitude > 0.01f)
            {
                Transform cameraTransform = Camera.main.transform;

                Vector3 cameraForward = cameraTransform.forward;
                Vector3 cameraRight = cameraTransform.right;

                cameraForward.y = 0f;
                cameraRight.y = 0f;

                cameraForward.Normalize();
                cameraRight.Normalize();

                moveDirection = cameraRight * input.x + cameraForward * input.y;

                if (normalized)
                    moveDirection.Normalize();
            }

            Vector3 velocity = moveDirection * MoveSpeed;

            Rb.linearVelocity = new Vector3(
                velocity.x,
                Rb.linearVelocity.y,
                velocity.z);

            UpdateAnimationDirection(new Vector2(moveDirection.x, moveDirection.z));
            UpdateFacingData();
            UpdateAttackCheckPosition();
        }

        public void StopMovement()
        {
            if (Rb == null)
                return;

            Rb.linearVelocity = Vector3.zero;
        }

        public void SetMoveSpeed(float speed)
        {
            this.MoveSpeed = speed;
        }

        public virtual void SlowEntityBy(float slowPercentage, float slowDuration)
        {
            StartCoroutine(SlowCoroutine(slowPercentage, slowDuration));
        }

        private IEnumerator SlowCoroutine(float slowPercentage, float slowDuration)
        {
            float originalSpeed = MoveSpeed;

            MoveSpeed = Mathf.Max(
                0.1f,
                originalSpeed * (1f - slowPercentage));

            yield return new WaitForSeconds(slowDuration);

            MoveSpeed = originalSpeed;
        }

        protected virtual void ReturnDefaultSpeed()
        {
            if (Anim != null)
                Anim.speed = 1f;
        }

        #endregion

        #region Facing / Animation

        private void UpdateAnimationDirection(Vector2 input)
        {
            float animX = Mathf.Abs(input.x) > 0.5f
                ? Mathf.Sign(input.x)
                : 0f;

            float animY = Mathf.Abs(input.y) > 0.5f
                ? Mathf.Sign(input.y)
                : 0f;

            bool hasSnappedDirection = animX != 0f || animY != 0f;

            if (!hasSnappedDirection)
                return;

            if (Anim != null && animX != lastAnimX)
            {
                Anim.SetFloat("MovementX", animX);
                lastAnimX = animX;
            }

            if (Anim != null && animY != lastAnimY)
            {
                Anim.SetFloat("MovementY", animY);
                lastAnimY = animY;
            }

            UpdateFacingFromSnappedDirection(animX, animY);
        }

        protected virtual void UpdateFacingFromSnappedDirection(float x, float y)
        {
            Vector3 newFacingDir = new Vector3(x, 0f, y);

            if (newFacingDir == FacingDir)
                return;

            FacingDir = newFacingDir;
            OnFacingChanged?.Invoke();
        }

        private void UpdateFacingData()
        {
            if (FacingDir.sqrMagnitude <= 0.01f)
                return;

            LastFacingDir = new Vector2(FacingDir.x, FacingDir.z);
            RotationAdjustmentDirection =
                GetRotationAdjustmentFromDirection(LastFacingDir);
        }

        private RotationAdjustmentDirection GetRotationAdjustmentFromDirection(
            Vector2 lastFacingDir)
        {
            return (lastFacingDir.x, lastFacingDir.y) switch
            {
                (1f, 0f) => RotationAdjustmentDirection.Right,
                (-1f, 0f) => RotationAdjustmentDirection.Left,
                (0f, 1f) => RotationAdjustmentDirection.Up,
                (0f, -1f) => RotationAdjustmentDirection.Down,
                (1f, 1f) => RotationAdjustmentDirection.UpRight,
                (-1f, 1f) => RotationAdjustmentDirection.UpLeft,
                (1f, -1f) => RotationAdjustmentDirection.DownRight,
                (-1f, -1f) => RotationAdjustmentDirection.DownLeft,
                _ => RotationAdjustmentDirection.Right,
            };
        }

        #endregion

        #region Attack Check

        protected virtual void OnValidate()
        {
            UpdateAttackCheckPosition();
        }

        private void UpdateAttackCheckPosition()
        {
            if (AttackCheck == null)
                return;

            AttackCheck.position =
                transform.position +
                FacingDir * AttackCheckDistance +
                Vector3.up * CheckHeight;
        }

        #endregion

        #region Damage / Knockback

        public virtual void DamageEffect(bool knockback, Transform attacker = null)
        {
            if (!knockback || IsDead)
                return;

            Vector3 direction;

            if (attacker != null)
            {
                direction = transform.position - attacker.position;
                direction.y = 0f;
                direction.Normalize();
            }
            else
            {
                direction = FacingDir.sqrMagnitude > 0f
                    ? FacingDir.normalized
                    : Vector3.forward;
            }

            StartCoroutine(HitKnockback(direction));
        }

        protected virtual IEnumerator HitKnockback(Vector3 direction)
        {
            if (Rb == null)
                yield break;

            IsKnocked = true;

            Rb.linearVelocity = Vector3.zero;
            Rb.AddForce(direction * KnockbackForce, ForceMode.Impulse);

            yield return new WaitForSeconds(KnockbackDuration);

            IsKnocked = false;
            Rb.linearVelocity = Vector3.zero;
        }

        #endregion

        #region Gizmos

        protected virtual void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;

            if (AttackCheck != null)
                Gizmos.DrawWireSphere(
                    AttackCheck.position,
                    AttackCheckRadius);

            Gizmos.DrawLine(
                transform.position,
                transform.position + FacingDir);
        }

        #endregion

        public virtual void Die()
        {
            IsDead = true;
            StopMovement();

            Destroy(gameObject);
        }
    }
}