using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Units;
using Unity.AppUI.Core;
using UnityEngine;

namespace ShiftedSignal.Garden.Effects
{
    public class Projectile : MonoBehaviour
    {
        [Header("VFX")]
        [SerializeField] private bool AlignWithNormals = true;
        [SerializeField] private GameObject MuzzlePrefab;
        [SerializeField] private GameObject HitPrefab;
        [SerializeField] private Vector3 OffsetHit = Vector3.zero;
        [SerializeField] private List<GameObject> Trails = new();

        [Header("Collision")]
        [SerializeField] private LayerMask HitMask = ~0;
        [SerializeField] private Collider[] explosionBuffer = new Collider[32];

        private Rigidbody rb;

        private AttackConfigSO attackConfig;
        private DamageData damageData;
        private GameObject owner;
        private GameObject target;
        private RotateToMouseScript rotateToMouse;

        private Vector3 accuracyOffset;
        private Vector3 arcStartPoint;
        private Vector3 arcTargetPoint;
        private float arcTravelProgress;

        private bool initialized;
        private bool collided;
        private bool canMove;
        private bool isReturningToPool;

        private Coroutine lifetimeRoutine;
        private Coroutine buildUpRoutine;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            isReturningToPool = false;
            ResetProjectileState();
        }

        private void OnDisable()
        {
            StopRunningCoroutines();

            initialized = false;
            canMove = false;
            collided = false;
            isReturningToPool = false;

            owner = null;
            target = null;
            rotateToMouse = null;
            attackConfig = null;
        }

        public void Initialize(
            AttackConfigSO attackConfig,
            GameObject target = null,
            Vector3? targetPointOverride = null,
            RotateToMouseScript rotateToMouse = null)
        {
            if (attackConfig == null)
                return;

            this.attackConfig = attackConfig;
            this.target = target;
            this.rotateToMouse = rotateToMouse;

            initialized = true;
            isReturningToPool = false;

            ResetProjectileState();

            if (attackConfig.ProjectileMovementType == ProjectileMovementType.Arc)
                SetupArcTarget(targetPointOverride);

            SpawnMuzzle();

            lifetimeRoutine = StartCoroutine(ReturnAfterLifetime());

            if (attackConfig.ProjectileBuildUpTime > 0f)
                buildUpRoutine = StartCoroutine(ActivateAfterDelay());
            else
                ActivateProjectile();
        }

        public void SetOwner(GameObject owner)
        {
            this.owner = owner;

            if (owner == null)
                return;

            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            Collider[] projectileColliders = GetComponentsInChildren<Collider>();

            foreach (Collider ownerCollider in ownerColliders)
            {
                if (ownerCollider == null)
                    continue;

                foreach (Collider projectileCollider in projectileColliders)
                {
                    if (projectileCollider == null)
                        continue;

                    Physics.IgnoreCollision(ownerCollider, projectileCollider, true);
                }
            }
        }

        public void SetDamageData(DamageData damageData)
        {
            this.damageData = damageData;
        }

        private void FixedUpdate()
        {
            if (!initialized || !canMove || collided || attackConfig == null)
                return;

            if (target != null && rotateToMouse != null)
                rotateToMouse.RotateToMouse(gameObject, target.transform.position);

            if (attackConfig.ProjectileRotate)
                transform.Rotate(0f, 0f, attackConfig.ProjectileRotateAmount, Space.Self);

            if (attackConfig.ProjectileMovementType == ProjectileMovementType.Arc)
                MoveArcProjectile();
            else
                MoveStraightProjectile();
        }

        private void MoveStraightProjectile()
        {
            if (rb == null || attackConfig.ProjectileSpeed <= 0f)
                return;

            Vector3 moveDirection = (transform.forward + accuracyOffset).normalized;
            Vector3 movement = moveDirection * (attackConfig.ProjectileSpeed * Time.fixedDeltaTime);
            
            bool flowControl = DetectHit(moveDirection, ref movement);
            if (!flowControl)
            {
                return;
            }

            rb.MovePosition(rb.position + movement);
        }

        private bool DetectHit(Vector3 moveDirection, ref Vector3 movement)
        {
            if (Physics.Raycast(
                    rb.position,
                    moveDirection,
                    out RaycastHit hit,
                    movement.magnitude,
                    HitMask,
                    QueryTriggerInteraction.Ignore))
            {

                if (owner != null &&
                    hit.collider.transform.IsChildOf(owner.transform))
                {
                    rb.MovePosition(rb.position + movement);
                    return false;
                }

                IDamageable damageable =
                    hit.collider.GetComponentInParent<IDamageable>();


                if (damageable != null &&
                    damageable.Owner == damageData.Owner)
                {
                    rb.MovePosition(rb.position + movement);
                    return false;
                }

                Vector3 impactPoint =
                    damageable != null
                        ? damageable.TargetPoint
                        : hit.point;

                Quaternion hitRotation =
                    AlignWithNormals
                        ? Quaternion.FromToRotation(
                            Vector3.up,
                            hit.normal)
                        : Quaternion.identity;

                HandleImpact(
                    impactPoint,
                    damageable,
                    hitRotation);

                return false;
            }

            return true;
        }

        private void MoveArcProjectile()
        {
            if (rb == null || attackConfig.ProjectileSpeed <= 0f)
                return;

            float distance = Vector3.Distance(arcStartPoint, arcTargetPoint);

            if (distance <= 0.01f)
            {
                HandleArcImpact();
                return;
            }

            arcTravelProgress += Time.fixedDeltaTime * attackConfig.ProjectileSpeed / distance;
            arcTravelProgress = Mathf.Clamp01(arcTravelProgress);

            Vector3 flatPosition = Vector3.Lerp(
                arcStartPoint,
                arcTargetPoint,
                arcTravelProgress);

            float arc = Mathf.Sin(arcTravelProgress * Mathf.PI) * attackConfig.ArcHeight;

            Vector3 nextPosition = flatPosition + Vector3.up * arc;

            rb.MovePosition(nextPosition);

            if (arcTravelProgress >= 1f)
                HandleArcImpact();
        }

        private void HandleArcImpact()
        {
            IDamageable damageable = null;

            if (target != null)
                damageable = target.GetComponentInParent<IDamageable>();

            Vector3 impactPoint = damageable != null
                ? damageable.TargetPoint
                : arcTargetPoint;

            HandleImpact(impactPoint, damageable, Quaternion.identity);
        }

        private void HandleImpact(
            Vector3 impactPoint,
            IDamageable directTarget,
            Quaternion hitRotation)
        {
            collided = true;
            canMove = false;

            if (rb != null)
            {
                rb.position = impactPoint;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            if (attackConfig != null && attackConfig.Exploding)
            {
                Explode(impactPoint);
            }
            else
            {
                if (directTarget != null)
                {
                    directTarget.TakeDamage(damageData);
                }

                SpawnHitVFX(impactPoint, hitRotation);
            }

            DetachTrails();
            ReturnToPool();
        }

        private void Explode(Vector3 position)
        {
            ObjectPoolManager.SpawnObject(
                attackConfig.ExplosionType,
                position,
                Quaternion.identity,
                null,
                .25f);

            int hitCount = Physics.OverlapSphereNonAlloc(
                position,
                attackConfig.ExplosionRadius,
                explosionBuffer,
                HitMask,
                QueryTriggerInteraction.Ignore);

            HashSet<IDamageable> damagedTargets = new();

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = explosionBuffer[i];

                if (hit == null)
                    continue;

                if (owner != null && hit.transform.IsChildOf(owner.transform))
                    continue;

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable == null)
                    continue;

                if (!damagedTargets.Add(damageable))
                    continue;

                DamageData explosionDamage = new DamageData(
                    attackConfig.ExplosionDamage,
                    damageData.Owner,
                    damageData.Attacker,
                    damageData.Knockback,
                    damageData.IgnoreFriendlyFire,
                    damageData.CanDamageBuildables);

                damageable.TakeDamage(explosionDamage);
            }
        }

        private void SetupArcTarget(Vector3? targetPointOverride)
        {
            arcStartPoint = transform.position;
            arcTravelProgress = 0f;

            if (targetPointOverride.HasValue)
            {
                arcTargetPoint = targetPointOverride.Value;
                return;
            }

            IDamageable damageable = null;

            if (target != null)
                damageable = target.GetComponentInParent<IDamageable>();

            arcTargetPoint = damageable != null
                ? damageable.TargetPoint
                : transform.position + transform.forward * 5f;
        }

        private void ResetProjectileState()
        {
            accuracyOffset = GetAccuracyOffset();

            collided = false;
            canMove = false;
            arcTravelProgress = 0f;

            if (rb == null)
                return;

            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        private Vector3 GetAccuracyOffset()
        {
            if (attackConfig == null || attackConfig.ProjectileAccuracy >= 100f)
                return Vector3.zero;

            float missAmount = 1f - attackConfig.ProjectileAccuracy / 100f;

            return new Vector3(
                0f,
                Random.Range(-missAmount, missAmount),
                Random.Range(-missAmount, missAmount));
        }

        private IEnumerator ActivateAfterDelay()
        {
            yield return new WaitForSeconds(attackConfig.ProjectileBuildUpTime);
            ActivateProjectile();
        }

        private void ActivateProjectile()
        {
            canMove = true;
        }

        private IEnumerator ReturnAfterLifetime()
        {
            yield return new WaitForSeconds(attackConfig.ProjectileLifetime);
            ReturnToPool();
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

            ParticleSystem childParticleSystem =
                particleObject.GetComponentInChildren<ParticleSystem>();

            if (childParticleSystem != null)
            {
                Destroy(particleObject, childParticleSystem.main.duration);
                return;
            }

            Destroy(particleObject, 2f);
        }

        private void DetachTrails()
        {
            if (!Application.isPlaying)
                return;

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
            if (isReturningToPool)
                return;

            isReturningToPool = true;

            StopRunningCoroutines();
            ResetOwnerCollision();

            ObjectPoolManager.ReturnObjectToPool(gameObject);
        }

        private void ResetOwnerCollision()
        {
            if (!Application.isPlaying)
            {
                owner = null;
                return;
            }

            if (owner == null)
                return;

            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            Collider[] projectileColliders = GetComponentsInChildren<Collider>();

            foreach (Collider ownerCollider in ownerColliders)
            {
                if (ownerCollider == null)
                    continue;

                foreach (Collider projectileCollider in projectileColliders)
                {
                    if (projectileCollider == null)
                        continue;

                    Physics.IgnoreCollision(ownerCollider, projectileCollider, false);
                }
            }

            owner = null;
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
    }
}