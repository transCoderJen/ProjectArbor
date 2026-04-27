using System;
using System.Collections;
using ShiftedSignal.Garden.Stats;
using ShiftedSignal.Garden.UserInterface;
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

    /// <summary>
    /// Base entity class for 2.5D gameplay.
    /// </summary>
    public class Entity : MonoBehaviour
    {
        public LayerMask TerrainLayer;
        public float GroundDist;
        #region Components
        public UI UI;
        public Animator Anim { get; private set; }
        public Rigidbody Rb { get; private set; }
        public SpriteRenderer Sr { get; private set; }
        public CharacterStats Stats { get; private set; }
        public CapsuleCollider Cd { get; private set; }

        #endregion

        [Header("Movement")]
        [SerializeField] protected float BaseMoveSpeed = 4f;
        protected float CurrentMoveSpeed;


        [Header("Facing")]
        public Vector3 FacingDir  = Vector3.right;
        public Vector2 LastFacingDir = Vector3.right;
        public RotationAdjustmentDirection RotationAdjustmentDirection = RotationAdjustmentDirection.Right;
        public bool FacingRight { get; private set; } = true;

        [Header("Knockback")]
        [SerializeField] protected float KnockbackForce = 6f;
        [SerializeField] protected float KnockbackDuration = 0.15f;
        protected bool IsKnocked;

        [Header("Combat")]
        public Transform AttackCheck;
        public float AttackCheckRadius = 0.5f;
        [Tooltip("The Distance in front of the player")]public float AttackCheckDistance = 1f;
        [Tooltip("How High the Checks Should Be")] public float CheckHeight = 1f;
        [SerializeField] public Vector3 RotationAdjustment { get; private set; }

        public bool IsDead = false;

        public Action OnFacingChanged = delegate { };

        private float lastAnimX = 0f;
        private float lastAnimY = 0f;

        protected virtual void Awake()
        {
            CurrentMoveSpeed = BaseMoveSpeed;

            Sr = GetComponentInChildren<SpriteRenderer>();
            Anim = GetComponentInChildren<Animator>();
            Rb = GetComponent<Rigidbody>();
            Stats = GetComponent<CharacterStats>();
            Cd = GetComponentInChildren<CapsuleCollider>();
            

            if (Rb != null)
            {
                Rb.constraints = RigidbodyConstraints.FreezeRotationX
                            | RigidbodyConstraints.FreezeRotationY
                            | RigidbodyConstraints.FreezeRotationZ;
            }
        }

        protected virtual void Start()
        {
            UI = FindAnyObjectByType<UI>();
            GameObject canvas = GameObject.Find("Canvas");
            // if (canvas != null)
            //     UI = canvas.GetComponent<UI>();
        }



        protected virtual void Update()
        {
            Vector3 castPos = transform.position + Vector3.up * 1f;

            if (Physics.Raycast(castPos, Vector3.down, out RaycastHit hit , Mathf.Infinity, TerrainLayer))
            {
                Vector3 movePos = transform.position;
                movePos.y = hit.point.y + GroundDist;
                transform.position = movePos;
            }
            
        
        }

        protected virtual void FixedUpdate()
        {

        }

        #region Movement
        /// <summary>
        /// Applies movement on the X/Z plane.
        /// Input.x = left/right
        /// Input.y = forward/back
        /// </summary>
        public virtual void ApplyMovement(Vector2 Input, bool normalized = true)
        {
            if (IsKnocked || IsDead || Rb == null)
                return;
            Vector3 moveDirection;
            if (normalized)
                moveDirection = new Vector3(Input.x, 0f, Input.y).normalized;
            else
                moveDirection = new Vector3(Input.x, 0f, Input.y);

            float speedStat = Stats != null ? Stats.Speed.GetValue() : 0f;
            float finalSpeed = CurrentMoveSpeed + speedStat;

            Vector3 velocity = moveDirection * finalSpeed;
            Rb.linearVelocity = new Vector3(velocity.x, Rb.linearVelocity.y, velocity.z);

            float animX = Mathf.Abs(Input.x) > 0.5f ? Mathf.Sign(Input.x) : 0f;
            float animY = Mathf.Abs(Input.y) > 0.5f ? Mathf.Sign(Input.y) : 0f;

            bool hasSnappedDirection = animX != 0f || animY != 0f;

            if (hasSnappedDirection)
            {
                if (animX != lastAnimX)
                {
                    Anim.SetFloat("MovementX", animX);
                    lastAnimX = animX;
                }

                if (animY != lastAnimY)
                {
                    Anim.SetFloat("MovementY", animY);
                    lastAnimY = animY;
                }

                UpdateFacingFromSnappedDirection(animX, animY);
            }

            if (FacingDir.sqrMagnitude > 0.01f)
            {
                LastFacingDir = new Vector2 (FacingDir.x, FacingDir.z);
                RotationAdjustmentDirection = GetRotationAdjustmentFromDirection(LastFacingDir);
            }

            UpdateAttackCheckPosition();
        }

        private RotationAdjustmentDirection GetRotationAdjustmentFromDirection(Vector2 lastFacingDir)
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

        protected virtual void OnValidate() {
            UpdateAttackCheckPosition();
        }

        private void UpdateAttackCheckPosition()
        {
            AttackCheck.position = transform.position + FacingDir * AttackCheckDistance + Vector3.up * CheckHeight;
        }

        public void StopMovement()
        {
            if (IsKnocked || Rb == null)
            {
                Debug.Log("RB is null");
                return;
            }
            
            Rb.linearVelocity = Vector3.zero;
        }

        public virtual void SlowEntityBy(float SlowPercentage, float SlowDuration)
        {
            StartCoroutine(SlowCoroutine(SlowPercentage, SlowDuration));
        }

        private IEnumerator SlowCoroutine(float SlowPercentage, float SlowDuration)
        {
            float original = CurrentMoveSpeed;
            CurrentMoveSpeed = Mathf.Max(0.1f, original * (1f - SlowPercentage));
            yield return new WaitForSeconds(SlowDuration);
            CurrentMoveSpeed = original;
        }

        protected virtual void ReturnDefaultSpeed()
        {
            Anim.speed = 1;
        }
        #endregion

        #region Facing
        protected virtual void UpdateFacingFromSnappedDirection(float x, float y)
        {
            Vector3 newFacingDir = new Vector3(x, 0f, y);

            if (newFacingDir == FacingDir)
                return;

            FacingDir = newFacingDir;

            if (FacingDir.x > 0f && !FacingRight)
                FlipSprite();
            else if (FacingDir.x < 0f && FacingRight)
                FlipSprite();

            OnFacingChanged?.Invoke();
        }

        protected virtual void FlipSprite()
        {
            FacingRight = !FacingRight;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (FacingRight ? 1f : -1f);
            transform.localScale = scale;
        }
        #endregion

        #region Damage / Knockback
        public virtual void DamageEffect(bool Knockback, Transform attacker = null)
        {
            if (!Knockback || IsDead)
                return;

            Vector3 direction;

            if (attacker != null)
            {
                Debug.Log("Attacker being passed in to Damage Effect Function");
                direction = transform.position - attacker.position;
                direction.Normalize();
            }
            else
            {
                direction = FacingDir.sqrMagnitude > 0f ? FacingDir.normalized : Vector3.forward;
            }

            StartCoroutine(HitKnockback(direction));
        }

        protected virtual IEnumerator HitKnockback(Vector3 direction)
        {
            if (Rb == null)
                yield break;
            
            IsKnocked = true;

            Debug.Log("Enemy is being knocked back");

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
            if (AttackCheck != null)
                Gizmos.DrawWireSphere(AttackCheck.position, AttackCheckRadius);

            Gizmos.DrawLine(transform.position, transform.position + FacingDir);
            // Gizmos.DrawLine(GroundCheck.position, new Vector3(GroundCheck.position.x, GroundCheck.position.y - GroundCheckDistance));
        }
        #endregion

        public virtual void Die()
        {
            IsDead = true;
            StopMovement();
        }
    }
}