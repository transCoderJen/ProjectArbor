using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class DefenseTower : BaseBuilding
    {
        [Header("Projectile")]
        [SerializeField] private Transform ProjectileSpawnTransform;

        [Header("Targeting")]
        [SerializeField] private int TargetBufferSize = 32;

        private float nextAttackTime;
        private Collider[] enemyBuffer;

        public AttackConfigSO AttackConfig => UnitSO != null
            ? UnitSO.AttackConfig
            : null;

        public float AttackRange => AttackConfig != null
            ? AttackConfig.AttackRange
            : 5f;

        public float AttackCooldown => AttackConfig != null
            ? AttackConfig.AttackDelay
            : 0.2f;

        public Collider[] EnemyBuffer => enemyBuffer;

        public override Transform ProjectileSpawnPoint =>
            ProjectileSpawnTransform != null
                ? ProjectileSpawnTransform
                : transform;

        protected override void Awake()
        {
            base.Awake();

            enemyBuffer = new Collider[TargetBufferSize];
        }

        protected override void Update()
        {
            base.Update();

            if (!IsActive || !HasConstantEffects)
                return;

            if (AttackConfig == null)
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
            // Later this should modify a runtime stats wrapper, not the SO.
        }

        public void UpgradeAttackCooldown(float amount)
        {
            // Later this should modify a runtime stats wrapper, not the SO.
        }
    }
}