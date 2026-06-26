using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class DefenseTower : BaseBuilding
    {
        [Header("Projectile")]
        [SerializeField] private Transform ProjectileSpawnTransform;

        private float nextAttackTime;
        private Collider[] enemyBuffer;

        private TowerStats currentStats;

        public float AttackRange => currentStats.AttackRange;
        public float AttackCooldown => currentStats.AttackCooldown;
        public Collider[] EnemyBuffer => enemyBuffer;

        public float ProjectileSpeed => currentStats.ProjectileSpeed;
        public float ProjectileAccuracy => currentStats.ProjectileAccuracy;
        public float ProjectileBuildUpTime => currentStats.ProjectileBuildUpTime;
        public bool ProjectileRotate => currentStats.ProjectileRotate;
        public float ProjectileRotateAmount => currentStats.ProjectileRotateAmount;
        public bool ProjectileBounce => currentStats.ProjectileBounce;
        public float ProjectileBounceForce => currentStats.ProjectileBounceForce;
        public float ProjectileLifetime => currentStats.ProjectileLifetime;

        private const int TargetBufferSize = 32;

        public override Transform ProjectileSpawnPoint =>
            ProjectileSpawnTransform != null
                ? ProjectileSpawnTransform
                : transform;

        protected override void Awake()
        {
            base.Awake();

            InitializeStatsFromBuildableData();
        }

        private void InitializeStatsFromBuildableData()
        {
            if (UnitSO != null && UnitSO.HasTowerStats)
                currentStats = UnitSO.BaseTowerStats;
            else
                currentStats = GetFallbackStats();

            enemyBuffer = new Collider[TargetBufferSize];
        }

        private TowerStats GetFallbackStats()
        {
            return new TowerStats
            {
                AttackRange = 5f,
                AttackCooldown = 0.2f,

                ProjectileSpeed = 20f,
                ProjectileAccuracy = 100f,
                ProjectileBuildUpTime = 0f,
                ProjectileRotate = false,
                ProjectileRotateAmount = 0f,
                ProjectileBounce = false,
                ProjectileBounceForce = 0f,
                ProjectileLifetime = 5f
            };
        }

        protected override void Update()
        {
            base.Update();

            if (!IsActive || !HasConstantEffects)
                return;

            if (Time.time < nextAttackTime)
                return;

            nextAttackTime = Time.time + AttackCooldown;

            foreach (BuildableEffect effect in ConstantEffects)
            {
                if (effect == null)
                    continue;

                effect.Apply(this);
            }
        }

        public void UpgradeAttackRange(float amount)
        {
            currentStats.AttackRange += amount;
        }

        public void UpgradeAttackCooldown(float amount)
        {
            currentStats.AttackCooldown = Mathf.Max(
                0.05f,
                currentStats.AttackCooldown - amount);
        }

        public void UpgradeProjectileSpeed(float amount)
        {
            currentStats.ProjectileSpeed += amount;
        }

        public void UpgradeProjectileAccuracy(float amount)
        {
            currentStats.ProjectileAccuracy = Mathf.Clamp(
                currentStats.ProjectileAccuracy + amount,
                0f,
                100f);
        }

        public void UpgradeProjectileLifetime(float amount)
        {
            currentStats.ProjectileLifetime += amount;
        }

        public void UpgradeProjectileBounceForce(float amount)
        {
            currentStats.ProjectileBounceForce += amount;
        }

        public void SetProjectileBounce(bool enabled)
        {
            currentStats.ProjectileBounce = enabled;
        }

        public void SetProjectileRotate(bool enabled)
        {
            currentStats.ProjectileRotate = enabled;
        }
    }
}