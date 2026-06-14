using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Interfaces;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildings
{
    public class Building : MonoBehaviour, IDamageable, IHealable, IRaiderTarget
    {
        [Header("Health")]
        [SerializeField] protected int maxHealth = 10;
        [SerializeField] protected int currentHealth;

        [Header("Combat")]
        [SerializeField] protected CombatTeam team = CombatTeam.Player;

        [Header("Raid")]
        [SerializeField] protected int priority = 0;
        [SerializeField] protected bool canBeTargetedByRaiders = true;

        public CombatTeam Team => team;

        public Transform TargetTransform => transform;

        public int Priority => priority;

        public bool IsValidTarget =>
            canBeTargetedByRaiders &&
            currentHealth > 0;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
        }

        public virtual void TakeDamage(DamageData damageData)
        {
            if (currentHealth <= 0)
                return;

            if (damageData.AttackerTeam == Team)
                return;

            currentHealth -= damageData.Amount;
            currentHealth = Mathf.Max(currentHealth, 0);

            OnDamaged(damageData);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public virtual void Heal(int healAmount)
        {
            if (currentHealth <= 0)
                return;

            currentHealth += healAmount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }

        protected virtual void OnDamaged(DamageData damageData)
        {
        }

        public virtual void SetRaidTargetable(bool value)
        {
            canBeTargetedByRaiders = value;
        }

        protected virtual void Die()
        {
            canBeTargetedByRaiders = false;

            Debug.Log($"{name} was destroyed.", this);
        }
    }
}