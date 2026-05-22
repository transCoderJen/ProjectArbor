using ShiftedSignal.Garden.EntitySpace.EnemySpace;
using UnityEngine;

namespace ShiftedSignal.Garden.Stats
{
    public class EnemyStats : CharacterStats
    {
        private Enemy enemy;

        public Stat soulsDropAmount;

        [Header("Level Details")]
        [SerializeField] private int level = 1;

        [Range(0f, 1f)]
        [SerializeField] private float percentageModifier = .2f;

        protected override void Start()
        {
            base.Start();

            enemy = GetComponent<Enemy>();

            soulsDropAmount.SetDefaultValue(100);

            ApplyLevelModifiers();
        }

        private void ApplyLevelModifiers()
        {
            Modify(Power);
            Modify(Vitality);
            Modify(Defense);
            Modify(Speed);
            Modify(CritChance);
            Modify(CritPower);
            Modify(Evasion);
            Modify(MagicResistance);

            Modify(soulsDropAmount);
        }

        private void Modify(Stat stat)
        {
            if (level <= 1)
                return;

            int baseValue = stat.GetValue();
            int modifier = Mathf.RoundToInt(baseValue * percentageModifier * (level - 1));

            stat.AddModifier(modifier);
        }

        public override void TakeDamage(int damage, bool knockback, Transform attacker)
        {
            base.TakeDamage(damage, knockback, attacker);
        }

        protected override void Die()
        {
            base.Die();

            enemy.Die();

            // TODO: Add SFX to enemy death
            // TODO: Generate item drops
            // TODO: Add soulsDropAmount.GetValue() to player currency
        }
    }
}