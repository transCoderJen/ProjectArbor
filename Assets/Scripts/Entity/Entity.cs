using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Base entity class for 2.5D gameplay.
/// Movement occurs on the X/Z plane while Y remains locked.
/// </summary>
public class Entity : MonoBehaviour
{
    public LayerMask TerrainLayer;
    public float GroundDist;
    #region Components
    // public UI UI { get; private set; }
    public Animator Anim { get; private set; }
    public Rigidbody Rb { get; private set; }
    public SpriteRenderer Sr { get; private set; }
    public CharacterStats Stats { get; private set; }
    public CapsuleCollider Cd { get; private set; }
    #endregion

    [Header("Movement")]
    [SerializeField] protected float BaseMoveSpeed = 4f;
    [SerializeField] private float LockedYPosition;
    protected float CurrentMoveSpeed;

    [Header("Facing")]
    public Vector3 FacingDir { get; private set; } = Vector3.right;
    public Vector2 LastFacingDir { get; private set; } = Vector3.right;
    public bool FacingRight { get; private set; } = true;

    [Header("Knockback")]
    [SerializeField] protected float KnockbackForce = 6f;
    [SerializeField] protected float KnockbackDuration = 0.15f;
    protected bool IsKnocked;

    [Header("Combat")]
    public Transform AttackCheck;
    public float AttackCheckRadius = 0.5f;

    public bool IsDead = false;

    public Action OnFacingChanged = delegate { };

    private float lastAnimX = 0f;
    private float lastAnimY = 0f;

    protected virtual void Awake()
    {
        CurrentMoveSpeed = BaseMoveSpeed;
        LockedYPosition = transform.position.y;
    }

    protected virtual void Start()
    {
        Sr = GetComponentInChildren<SpriteRenderer>();
        Anim = GetComponentInChildren<Animator>();
        Rb = GetComponent<Rigidbody>();
        Stats = GetComponent<CharacterStats>();
        Cd = GetComponent<CapsuleCollider>();

        if (Rb != null)
        {
            Rb.constraints = RigidbodyConstraints.FreezePositionY
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationY
                           | RigidbodyConstraints.FreezeRotationZ;
        }

        GameObject canvas = GameObject.Find("Canvas");
        // if (canvas != null)
        //     UI = canvas.GetComponent<UI>();
    }

    protected virtual void Update()
    {
        RaycastHit hit;
        Vector3 castPos = transform.position;
        castPos.y += 1;

        if (Physics.Raycast(castPos, -transform.up, out hit, Mathf.Infinity, TerrainLayer))
        {
            if (hit.collider != null)
            {
                Vector3 movePos = transform.position;
                movePos.y = hit.point.y + GroundDist;
                transform.position = movePos;
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        // LockYPosition();
    }

    #region Movement
    /// <summary>
    /// Applies movement on the X/Z plane.
    /// Input.x = left/right
    /// Input.y = forward/back
    /// </summary>
    public virtual void ApplyMovement(Vector2 Input)
    {
        if (IsKnocked || IsDead || Rb == null)
            return;

        Vector3 moveDirection = new Vector3(Input.x, 0f, Input.y).normalized;

        float speedStat = Stats != null ? Stats.Speed.GetValue() : 0f;
        float finalSpeed = CurrentMoveSpeed + speedStat;

        Vector3 velocity = moveDirection * finalSpeed;
        Rb.linearVelocity = velocity;

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
            LastFacingDir = FacingDir;
        }
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

    private void LockYPosition()
    {
        Vector3 position = transform.position;
        if (!Mathf.Approximately(position.y, LockedYPosition))
        {
            position.y = LockedYPosition;
            transform.position = position;
        }
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
    public virtual void DamageEffect(bool Knockback, Transform Attacker = null)
    {
        if (!Knockback || IsDead)
            return;

        Vector3 direction;

        if (Attacker != null)
        {
            direction = transform.position - Attacker.position;
            direction.y = 0f;
            direction.Normalize();
        }
        else
        {
            direction = FacingDir.sqrMagnitude > 0f ? FacingDir.normalized : Vector3.forward;
        }

        StartCoroutine(HitKnockback(direction));
    }

    protected virtual IEnumerator HitKnockback(Vector3 Direction)
    {
        if (Rb == null)
            yield break;

        IsKnocked = true;

        Rb.linearVelocity = Vector3.zero;
        Rb.AddForce(Direction * KnockbackForce, ForceMode.Impulse);

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
    }
    #endregion

    public virtual void Die()
    {
        IsDead = true;
        StopMovement();
    }
}