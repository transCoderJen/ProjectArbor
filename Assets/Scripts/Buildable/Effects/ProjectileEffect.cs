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
        public override void Apply(BaseBuilding buildable)
        {
            if (buildable == null)
                return;

            if (buildable is not DefenseTower tower)
                return;

            if (tower.AttackConfig == null)
                return;

            if (tower.ProjectileSpawnPoint == null)
                return;

            if (tower.DamageableSensor == null)
                return;

            if (!tower.IsEffectReady(this, tower.AttackCooldown))
                return;

            Transform spawnPoint = tower.ProjectileSpawnPoint;

            IDamageable target = tower.DamageableSensor.GetNearestValidTarget(
                spawnPoint.position,
                tower.Owner);

            if (target == null)
                return;

            Vector3 targetPoint = target.TargetPoint;
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
                    target: target.Transform.gameObject,
                    targetPointOverride: targetPoint);

                projectile.SetOwner(tower.gameObject);

                projectile.SetDamageData(new DamageData(
                    tower.AttackConfig.Damage,
                    tower.Owner,
                    tower.transform,
                    tower.AttackConfig.Knockback,
                    tower.AttackConfig.IgnoreFriendlyFire,
                    tower.AttackConfig.CanDamageBuildables));
            }

            tower.MarkEffectUsed(this);
        }
    }
}