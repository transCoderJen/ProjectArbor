using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using IEnumerator = System.Collections.IEnumerator;
using ShiftedSignal.Garden.EntitySpace;
using ShiftedSignal.Garden.Effects;

namespace ShiftedSignal.Garden.Stats
{
    
    public enum StatType
    {
        MaxHP,
        MaxMP,
        Power,
        Vitality,
        Defense,
        Speed,
        CritChance,
        CritPower,
        Evasion,
        MagicResistance
    }

    public class CharacterStats : MonoBehaviour
    {
        private EntityFX Fx;
        private Entity entity;

        [Header("Core Stats")]
        public Stat MaxHP;           // Base HP stat (pairs with Vitality scaling below)
        public Stat MaxMP;            // Base MP stat
        public Stat Power;              // Scales all damage (physical + magical if you want)
        public Stat Vitality;           // Scales max health
        public Stat Defense;            // Damage reduction stat
        public Stat Speed;              // Movement speed stat

        [Header("Combat Stats")]
        public Stat CritChance;
        public Stat CritPower;          // default 150%
        public Stat Evasion;
        public Stat MagicResistance;

        [Header("State")]
        public int CurrentHealth;

        public Action OnHealthChanged = delegate { };
        public bool IsDead { get; private set; }
        public bool IsInvincible { get; private set; }

        public bool Vulnerable;
        private float VulnerabilityAmount;

        protected virtual void Start()
        {
            CritPower.SetDefaultValue(150);

            Fx = GetComponent<EntityFX>();
            entity = GetComponent<Entity>();

            CurrentHealth = GetMaxHealthValue();
        }

        protected virtual void Update()
        {
            if (CurrentHealth > GetMaxHealthValue())
                CurrentHealth = GetMaxHealthValue();
        }

        #region Temporary Modifiers
        public void MakeVulnerable(float Amount, float Duration)
        {
            VulnerabilityAmount = Amount;
            StartCoroutine(VulnerableForCoroutine(Duration));
        }

        private IEnumerator VulnerableForCoroutine(float Duration)
        {
            Vulnerable = true;
            yield return new WaitForSeconds(Duration);
            Vulnerable = false;
            VulnerabilityAmount = 0f;
        }

        public virtual void IncreaseStatBy(int Modifier, float Duration, Stat StatToModify)
        {
            StartCoroutine(StatModCoroutine(Modifier, Duration, StatToModify));
        }

        private IEnumerator StatModCoroutine(int Modifier, float Duration, Stat StatToModify)
        {
            StatToModify.AddModifier(Modifier);
            yield return new WaitForSeconds(Duration);
            StatToModify.RemoveModifier(Modifier);
        }
        #endregion

        #region Damage / Healing
        public virtual void DoDamage(CharacterStats TargetStats, bool Knockback, float DamagePercentage = 1f)
        {
            if (TargetCanAvoidAttack(TargetStats))
                return;

            int TotalDamage = Mathf.RoundToInt(GetTotalDamage() * DamagePercentage);

            if (CanCrit())
            {
                TotalDamage = CalculateCriticalDamage(TotalDamage);
                // if (Fx != null && TargetStats != null)
                //     Fx.CreateCritHitFx(TargetStats.transform, Entity != null ? Entity.facingDir : 1);
            }

            // if (Fx != null && TargetStats != null)
            //     Fx.CreateHitFx(TargetStats.transform);

            TotalDamage = ApplyDefenseReduction(TargetStats, TotalDamage);
            TargetStats.TakeDamage(TotalDamage, Knockback, this.transform);
        }

        // "Power = all damage scaling"
        // Keep this simple: base damage is just Power.
        // If you later add weapon damage, add it here.
        public int GetTotalDamage()
        {
            return Power.GetValue();
        }

        public virtual void TakeDamage(int Damage, bool Knockback, Transform attacker)
        {
            if (IsInvincible)
                return;

            if (GetComponent<Entity>() != null)
            {
                GetComponent<Entity>()?.DamageEffect(Knockback, attacker);
            }


            DecreaseHealthBy(Damage);

            if (CurrentHealth <= 0 && !IsDead)
                Die();
        }

        public virtual void IncreaseHealthBy(int Amount)
        {
            CurrentHealth = Math.Min(GetMaxHealthValue(), CurrentHealth + Amount);
            OnHealthChanged?.Invoke();
        }

        public virtual void DecreaseHealthBy(int Damage)
        {
            int TotalDamage = Mathf.RoundToInt(Damage * (1 + VulnerabilityAmount));
            TotalDamage = Math.Max(1, TotalDamage); // always at least 1

            CurrentHealth -= TotalDamage;

            if (Fx != null)
                 Fx.CreatePopUpText(TotalDamage.ToString());

            OnHealthChanged?.Invoke();
        }

        protected virtual void Die()
        {
            IsDead = true;
            Destroy(gameObject); //TODO DeathStates for player and enemies.
        }

        public void MakeInvincible(bool Invincible) => IsInvincible = Invincible;
        #endregion

        #region Calculations
        // Vitality scales Max HP. Tweak this one number and your whole game balance shifts cleanly.
        // Example: Vitality * 5 (same as your old script)
        public int GetMaxHealthValue() => MaxHP.GetValue() + Vitality.GetValue() * 5;

        private int ApplyDefenseReduction(CharacterStats TargetStats, int IncomingDamage)
        {
            // Simple, consistent: subtract target Defense and clamp.
            // If you want "percent reduction" later, convert Defense to a curve here.
            int Reduced = IncomingDamage - TargetStats.Defense.GetValue();
            return Math.Max(0, Reduced);
        }

        public virtual void OnEvasion()
        {
            // Hook for dodge VFX/SFX/animation.
        }

        private bool TargetCanAvoidAttack(CharacterStats TargetStats)
        {
            int TotalEvasion = TargetStats.Evasion.GetValue();

            if (Random.Range(0, 100) < TotalEvasion)
            {
                TargetStats.OnEvasion();
                return true;
            }

            return false;
        }

        private bool CanCrit()
        {
            int TotalCritChance = CritChance.GetValue();

            return Random.Range(0, 100) <= TotalCritChance;
        }

        private int CalculateCriticalDamage(int Damage)
        {
            // CritPower is stored like 150 = 150%
            float Mult = CritPower.GetValue() * 0.01f;
            return Mathf.RoundToInt(Damage * Mult);
        }
        #endregion

        public Stat GetStat(StatType StatType)
        {
            switch (StatType)
            {
                case StatType.MaxHP: return MaxHP;
                case StatType.MaxMP: return MaxMP;
                case StatType.Power: return Power;
                case StatType.Vitality: return Vitality;
                case StatType.Defense: return Defense;
                case StatType.Speed: return Speed;
                case StatType.CritChance: return CritChance;
                case StatType.CritPower: return CritPower;
                case StatType.Evasion: return Evasion;
                case StatType.MagicResistance: return MagicResistance;
                default:
                    throw new ArgumentException("Invalid stat type");
            }
        }
    }
}