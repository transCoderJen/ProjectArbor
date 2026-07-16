using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    public enum AttackType
    {
        Melee,
        Projectile
    }

    public enum ProjectileMovementType
    {
        Straight,
        Arc
    }

    [CreateAssetMenu(fileName = "Attack Config", menuName = "Units/Attack Config", order = 7)]
    public class AttackConfigSO : ScriptableObject
    {
        [Header("Attack")]
        [field: SerializeField] public AttackType Type { get; private set; } = AttackType.Melee;
        [field: SerializeField] public int Damage { get; private set; } = 5;
        [field: SerializeField] public float Range { get; private set; } = 2f;
        [field: SerializeField] public float Delay { get; private set; } = 1f;

        [Header("Damage")]
        [field: SerializeField] public bool Knockback { get; private set; } = true;
        [field: SerializeField] public bool IgnoreFriendlyFire { get; private set; } = true;
        [field: SerializeField] public bool CanDamageBuildables { get; private set; } = true;

        [Header("Projectile")]
        [field: SerializeField] public PooledObjectList ProjectileType { get; private set; } = PooledObjectList.RedArrowProjectile;
        [field: SerializeField] public float ProjectileSpeed { get; private set; } = 12f;
        [field: SerializeField] public ProjectileMovementType ProjectileMovementType { get; private set; }
        [field: SerializeField] public float ArcHeight { get; private set; } = 3f;

        [Header("Explosion")]
        [field: SerializeField] public bool Exploding { get; private set; }
        [field: SerializeField] public PooledObjectList ExplosionType { get; private set; } = PooledObjectList.RedArrowProjectile;
        [field: SerializeField] public float ExplosionRadius { get; private set; } = 2f;
        [field: SerializeField] public int ExplosionDamage { get; private set; } = 3;
        [field: SerializeField] public LayerMask ExplosionHitMask { get; private set; }

        [field: SerializeField]
        [field: Range(0f, 100f)]
        public float ProjectileAccuracy { get; private set; } = 100f;

        [field: SerializeField] public float ProjectileBuildUpTime { get; private set; } = 0f;
        [field: SerializeField] public bool ProjectileRotate { get; private set; }
        [field: SerializeField] public float ProjectileRotateAmount { get; private set; }
        [field: SerializeField] public bool ProjectileBounce { get; private set; }
        [field: SerializeField] public float ProjectileBounceForce { get; private set; }
        [field: SerializeField] public float ProjectileLifetime { get; private set; } = 5f;
    }
}