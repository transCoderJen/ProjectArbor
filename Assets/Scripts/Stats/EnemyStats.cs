

using ShiftedSignal.Garden.EntitySpace.EnemySpace;
using UnityEngine;

namespace ShiftedSignal.Garden.Stats
{
    public class EnemyStats : CharacterStats
    {
        private Enemy enemy;
        // private ItemDrop myDropSystem;
        public Stat soulsDropAmount;

        [Header("Level Details")]
        [SerializeField] private int level = 1;
        
        [Range(0f, 1f)]
        [SerializeField] private float percentageModifier = .2f;

        protected override void Start()
        {

            base.Start();

            soulsDropAmount.SetDefaultValue(100);
            ApplyLevelModifiers();
            enemy = GetComponent<Enemy>();
            // myDropSystem = GetComponent<ItemDrop>();

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

        private void Modify(Stat _stat)
        {
            for (int i = 1; i < level; i++)
            {
                float modifier = _stat.GetValue() * percentageModifier;

                _stat.AddModifier(Mathf.RoundToInt(modifier));
            }
        }

        public override void TakeDamage(int _damage, bool _knockback, Transform _attacker)
        {
            base.TakeDamage(_damage, _knockback, _attacker);
        }

        protected override void Die()
        {
            base.Die();
            // AudioManager.instance.PlaySFX(SFXSounds.attack3, null); TODO add SFX to enemy death
            enemy.Die();

            // myDropSystem.GenerateDrop();  TODO drop system

            // PlayerManager.Instance.currency += soulsDropAmount.GetValue(); // TODO add currency to player when enemy killed
        }
    }
}