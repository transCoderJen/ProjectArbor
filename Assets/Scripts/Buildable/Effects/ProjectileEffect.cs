using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Effects;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    [CreateAssetMenu(fileName = "Attack Nearest Effect", menuName = "Data/Buildable Effects/Attack Nearest")]
    public class ProjectileEffect : BuildableEffect
    {
        [Header("Targeting")]
        [SerializeField] private LayerMask EnemyLayer;

        public override void Apply(BaseBuilding buildable)
        {
            if (buildable == null)
                return;

            if (buildable is not DefenseTower tower)
                return;

            if (tower.AttackConfig == null)
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

            IDamageable damageable =
                targetCollider.GetComponentInParent<IDamageable>();

            Vector3 targetPoint = damageable != null
                ? damageable.TargetPoint
                : GetTargetGroundPoint(targetCollider);

            Vector3 direction = targetPoint - spawnPoint.position;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion rotation = Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);

            GameObject projectileObject = ObjectPoolManager.SpawnObject(
                tower.AttackConfig.ProjectileType,
                spawnPoint.position,
                rotation,
                null,
                1);

            if (projectileObject == null)
                return;

            if (projectileObject.TryGetComponent(out Projectile projectile))
            {
                projectile.Initialize(
                    tower.AttackConfig,
                    target: targetCollider.gameObject,
                    targetPointOverride: targetPoint);

                projectile.SetOwner(tower.gameObject);

                projectile.SetDamageData(new DamageData(
                    tower.AttackConfig.AttackDamage,
                    tower.Team,
                    tower.transform,
                    tower.AttackConfig.Knockback,
                    tower.AttackConfig.IgnoreFriendlyFire,
                    tower.AttackConfig.CanDamageBuildables));
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

                IDamageable damageable =
                    enemy.GetComponentInParent<IDamageable>();

                Vector3 targetPoint = damageable != null
                    ? damageable.TargetPoint
                    : GetTargetGroundPoint(enemy);

                float distanceSqr = (targetPoint - origin).sqrMagnitude;

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