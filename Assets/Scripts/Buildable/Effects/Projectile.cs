using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Defaults")]
    [SerializeField] private float DefaultSpeed = 10f;
    [SerializeField] private float DefaultBuildUpTime = 0f;
    [SerializeField, Range(0f, 100f)] private float DefaultAccuracy = 100f;
    [SerializeField] private bool DefaultRotate = false;
    [SerializeField] private float DefaultRotateAmount = 45f;
    [SerializeField] private bool DefaultBounce = false;
    [SerializeField] private float DefaultBounceForce = 10f;
    [SerializeField] private float DefaultMaxLifetime = 5f;

    [Header("VFX")]
    [SerializeField] private bool AlignWithNormals = true;
    [SerializeField] private GameObject MuzzlePrefab;
    [SerializeField] private GameObject HitPrefab;
    [SerializeField] private Vector3 OffsetHit = Vector3.zero;
    [SerializeField] private List<GameObject> Trails = new();

    private Rigidbody rb;

    private float speed;
    private float buildUpTime;
    private float accuracy;
    private bool rotate;
    private float rotateAmount;
    private bool bounce;
    private float bounceForce;
    private float maxLifetime;

    private Vector3 startPos;
    private Vector3 accuracyOffset;

    private bool initialized;
    private bool collided;
    private bool canMove;

    private GameObject target;
    private RotateToMouseScript rotateToMouse;

    private Coroutine lifetimeRoutine;
    private Coroutine buildUpRoutine;

    private DamageData damageData;
    private GameObject owner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        ResetProjectileState();
    }

    private void OnDisable()
    {
        StopRunningCoroutines();
        ResetOwnerCollision();

        initialized = false;
        target = null;
        rotateToMouse = null;
    }

    public void Initialize(
        float speed,
        float accuracy = 100f,
        float buildUpTime = 0f,
        bool rotate = false,
        float rotateAmount = 45f,
        bool bounce = false,
        float bounceForce = 10f,
        float maxLifetime = 5f,
        GameObject target = null,
        RotateToMouseScript rotateToMouse = null)
    {
        this.speed = speed;
        this.accuracy = Mathf.Clamp(accuracy, 0f, 100f);
        this.buildUpTime = Mathf.Max(0f, buildUpTime);
        this.rotate = rotate;
        this.rotateAmount = rotateAmount;
        this.bounce = bounce;
        this.bounceForce = bounceForce;
        this.maxLifetime = Mathf.Max(0.1f, maxLifetime);
        this.target = target;
        this.rotateToMouse = rotateToMouse;

        initialized = true;

        ResetProjectileState();

        SpawnMuzzle();

        lifetimeRoutine = StartCoroutine(ReturnAfterLifetime());

        if (this.buildUpTime > 0f)
            buildUpRoutine = StartCoroutine(ActivateAfterDelay());
        else
            ActivateProjectile();
    }

    public void InitializeWithDefaults()
    {
        Initialize(
            DefaultSpeed,
            DefaultAccuracy,
            DefaultBuildUpTime,
            DefaultRotate,
            DefaultRotateAmount,
            DefaultBounce,
            DefaultBounceForce,
            DefaultMaxLifetime);
    }

    private void FixedUpdate()
    {
        if (!initialized || !canMove || collided)
            return;

        if (target != null && rotateToMouse != null)
            rotateToMouse.RotateToMouse(gameObject, target.transform.position);

        if (rotate)
            transform.Rotate(0f, 0f, rotateAmount, Space.Self);

        MoveProjectile();
    }

    public void SetOwner(GameObject owner)
    {
        this.owner = owner;

        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        Collider[] projectileColliders = GetComponentsInChildren<Collider>();

        foreach (Collider ownerCollider in ownerColliders)
        {
            foreach (Collider projectileCollider in projectileColliders)
            {
                Physics.IgnoreCollision(ownerCollider, projectileCollider, true);
            }
        }
    }

    private void ResetOwnerCollision()
    {
        if (owner == null)
            return;

        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        Collider[] projectileColliders = GetComponentsInChildren<Collider>();

        foreach (Collider ownerCollider in ownerColliders)
        {
            foreach (Collider projectileCollider in projectileColliders)
            {
                Physics.IgnoreCollision(ownerCollider, projectileCollider, false);
            }
        }

        owner = null;
    }

    public void SetDamageData(DamageData damageData)
    {
        this.damageData = damageData;
        Debug.Log($"Projectile received damage data: {damageData.Amount}, team: {damageData.AttackerTeam}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!initialized || collided)
            return;

        if (owner != null &&
            collision.transform.IsChildOf(owner.transform))
            return;

        IDamageable damageable =
            collision.collider.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damageData);

            if (!bounce)
            {
                HandleImpact(collision.contacts[0]);
                return;
            }

            BounceProjectile(collision);
            return;
        }

        // Hit the world.
        if (bounce)
        {
            BounceProjectile(collision);
        }
        else
        {
            HandleImpact(collision.contacts[0]);
        }
    }

    private void ResetProjectileState()
    {
        startPos = transform.position;
        accuracyOffset = GetAccuracyOffset();

        collided = false;
        canMove = false;

        if (rb == null)
            return;

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void MoveProjectile()
    {
        if (rb == null || speed <= 0f)
            return;

        Vector3 moveDirection = (transform.forward + accuracyOffset).normalized;
        rb.MovePosition(rb.position + moveDirection * (speed * Time.fixedDeltaTime));
    }

    private Vector3 GetAccuracyOffset()
    {
        if (accuracy >= 100f)
            return Vector3.zero;

        float missAmount = 1f - accuracy / 100f;

        return new Vector3(
            0f,
            Random.Range(-missAmount, missAmount),
            Random.Range(-missAmount, missAmount));
    }

    private IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(buildUpTime);
        ActivateProjectile();
    }

    private void ActivateProjectile()
    {
        canMove = true;
    }

    private IEnumerator ReturnAfterLifetime()
    {
        yield return new WaitForSeconds(maxLifetime);
        ReturnToPool();
    }

    private void BounceProjectile(Collision collision)
    {
        if (rb == null)
            return;

        ContactPoint contact = collision.contacts[0];

        rb.useGravity = true;
        rb.linearDamping = 0.5f;

        Vector3 reflectDirection = Vector3.Reflect(
            (contact.point - startPos).normalized,
            contact.normal);

        rb.AddForce(reflectDirection * bounceForce, ForceMode.Impulse);

        collided = false;
        canMove = false;
    }

    private void SpawnMuzzle()
    {
        if (MuzzlePrefab == null)
            return;

        GameObject muzzleVFX = Instantiate(
            MuzzlePrefab,
            transform.position,
            Quaternion.identity);

        muzzleVFX.transform.forward = transform.forward + accuracyOffset;

        DestroyParticleObjectAfterDuration(muzzleVFX);
    }

    private void SpawnHitVFX(Vector3 position, Quaternion rotation)
    {
        if (HitPrefab == null)
            return;

        GameObject hitVFX = Instantiate(
            HitPrefab,
            position + OffsetHit,
            rotation);

        DestroyParticleObjectAfterDuration(hitVFX);
    }

    private void DestroyParticleObjectAfterDuration(GameObject particleObject)
    {
        if (particleObject == null)
            return;

        ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();

        if (particleSystem != null)
        {
            Destroy(particleObject, particleSystem.main.duration);
            return;
        }

        ParticleSystem childParticleSystem = particleObject.GetComponentInChildren<ParticleSystem>();

        if (childParticleSystem != null)
        {
            Destroy(particleObject, childParticleSystem.main.duration);
            return;
        }

        Destroy(particleObject, 2f);
    }

    private void DetachTrails()
    {
        if (Trails == null || Trails.Count == 0)
            return;

        foreach (GameObject trail in Trails)
        {
            if (trail == null)
                continue;

            trail.transform.parent = null;

            ParticleSystem particleSystem = trail.GetComponent<ParticleSystem>();

            if (particleSystem == null)
                continue;

            particleSystem.Stop();

            float destroyDelay =
                particleSystem.main.duration +
                particleSystem.main.startLifetime.constantMax;

            Destroy(particleSystem.gameObject, destroyDelay);
        }
    }

    private void ReturnToPool()
    {
        StopRunningCoroutines();
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }

    private void StopRunningCoroutines()
    {
        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }

        if (buildUpRoutine != null)
        {
            StopCoroutine(buildUpRoutine);
            buildUpRoutine = null;
        }
    }

    private void HandleImpact(ContactPoint contact)
    {
        collided = true;
        canMove = false;

        DetachTrails();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Quaternion rotation = AlignWithNormals
            ? Quaternion.FromToRotation(Vector3.up, contact.normal)
            : Quaternion.identity;

        SpawnHitVFX(contact.point, rotation);

        ReturnToPool();
    }
}