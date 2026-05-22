using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Stats
{
    public class PlayerStats : CharacterStats
    {
        private Player player;

        [Header("Level Details")]
        [SerializeField] private int level = 1;
        [SerializeField] private int currentExperience;
        [SerializeField] private int experienceToLevel = 100;

        [Header("Scaling")]
        [Range(0f, 1f)]
        [SerializeField] private float percentageModifier = .1f;

        [SerializeField] private float experienceGrowthMultiplier = 1.25f;

        public int Level => level;
        public int CurrentExperience => currentExperience;
        public int ExperienceToLevel => experienceToLevel;

        protected override void Start()
        {
            base.Start();

            player = GetComponent<Player>();

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
        }

        private void Modify(Stat stat)
        {
            if (level <= 1)
                return;

            int baseValue = stat.GetValue();
            int modifier = Mathf.RoundToInt(baseValue * percentageModifier * (level - 1));

            stat.AddModifier(modifier);
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
                return;

            currentExperience += amount;

            while (currentExperience >= experienceToLevel)
            {
                currentExperience -= experienceToLevel;
                LevelUp();
            }
        }

        [ContextMenu("Level Up")]        
        private void LevelUp()
        {
            level++;

            Bus<PlayerLevelUpEvent>.Raise(new PlayerLevelUpEvent(level));

            experienceToLevel = Mathf.RoundToInt(experienceToLevel * experienceGrowthMultiplier);

            ApplySingleLevelModifiers();

            // TODO: Add level up VFX/SFX
            // TODO: Update UI
            // TODO: Restore health/mana if desired
        }

        private void ApplySingleLevelModifiers()
        {
            AddFlatLevelModifier(Power);
            AddFlatLevelModifier(Vitality);
            AddFlatLevelModifier(Defense);
            AddFlatLevelModifier(Speed);
            AddFlatLevelModifier(CritChance);
            AddFlatLevelModifier(CritPower);
            AddFlatLevelModifier(Evasion);
            AddFlatLevelModifier(MagicResistance);
        }

        private void AddFlatLevelModifier(Stat stat)
        {
            int baseValue = stat.GetValue() - stat.GetModifiersValue();
            int modifier = Mathf.RoundToInt(baseValue * percentageModifier);

            stat.AddModifier(modifier);
        }

        public override void TakeDamage(int damage, bool knockback, Transform attacker)
        {
            if (damage >= player.Stats.GetMaxHealthValue() * .3f)
                knockback = true;

            base.TakeDamage(damage, knockback, attacker);
        }

        protected override void Die()
        {
            base.Die();

            player.Die();

            PlayerManager.Instance.Currency = 0;

            // GetComponent<PlayerItemDrop>()?.GenerateDrop();
        }

        public override void DecreaseHealthBy(int damage)
        {
            base.DecreaseHealthBy(damage);

            // TODO: Armor effects
        }

        public override void OnEvasion()
        {
            // TODO: On Evasion Skill
        }
    }
}