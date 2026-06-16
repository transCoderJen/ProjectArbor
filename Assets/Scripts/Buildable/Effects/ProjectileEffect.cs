using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    [CreateAssetMenu(fileName = "Attack Nearest Effect", menuName = "Data/Buildable Effects/Attack Nearest")]
    public class ProjectileEffect : BuildableEffect
    {
        [Header("Targeting")]
        [SerializeField] private LayerMask EnemyLayer;

        public override void Apply(BaseBuildable buildable)
        {
            if (buildable == null)
                return;

            if (buildable is not DefenseTower tower)
                return;

            if (!buildable.IsEffectReady(this, tower.AttackCooldown))
                return;

            Transform spawnPoint = buildable.ProjectileSpawnPoint;

            int hitCount = Physics.OverlapSphereNonAlloc(
                spawnPoint.position,
                tower.AttackRange,
                tower.EnemyBuffer,
                EnemyLayer,
                QueryTriggerInteraction.Ignore);

            if (hitCount <= 0)
                return;

            Collider targetCollider = GetNearestEnemyCollider(
                spawnPoint.position,
                tower.EnemyBuffer,
                hitCount);

            if (targetCollider == null)
                return;

            Vector3 targetPoint = GetTargetGroundPoint(targetCollider);
            Vector3 direction = targetPoint - spawnPoint.position;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion rotation = Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);

            GameObject projectileObject = ObjectPoolManager.SpawnObject(
                PooledObjectList.Bullet,
                spawnPoint.position,
                rotation,
                null,
                1);

            if (projectileObject == null)
                return;

            if (projectileObject.TryGetComponent(out Projectile projectile))
            {
                projectile.Initialize(
                    speed: tower.ProjectileSpeed,
                    accuracy: tower.ProjectileAccuracy,
                    buildUpTime: tower.ProjectileBuildUpTime,
                    rotate: tower.ProjectileRotate,
                    rotateAmount: tower.ProjectileRotateAmount,
                    bounce: tower.ProjectileBounce,
                    bounceForce: tower.ProjectileBounceForce,
                    maxLifetime: tower.ProjectileLifetime);
            }

            buildable.MarkEffectUsed(this);
        }

        private Collider GetNearestEnemyCollider(
            Vector3 origin,
            Collider[] enemies,
            int hitCount)
        {
            Collider nearestEnemy = null;
            float nearestDistanceSqr = Mathf.Infinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider enemy = enemies[i];

                if (enemy == null)
                    continue;

                Vector3 targetGroundPoint = GetTargetGroundPoint(enemy);
                float distanceSqr = (targetGroundPoint - origin).sqrMagnitude;

                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearestEnemy = enemy;
                }
            }

            return nearestEnemy;
        }

        private Vector3 GetTargetGroundPoint(Collider targetCollider)
        {
            Bounds bounds = targetCollider.bounds;

            return new Vector3(
                bounds.center.x,
                bounds.min.y,
                bounds.center.z);
        }
    }
}