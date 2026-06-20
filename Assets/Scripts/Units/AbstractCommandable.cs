using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ShiftedSignal.Garden.Units
{
    public abstract class AbstractCommandable : MonoBehaviour, ISelectable, IDamageable, IHealable
    {
        [Header("Commandable Stats")]
        [field: SerializeField] public int MaxHealth { get; private set; } = 5;
        [field: SerializeField] public int CurrentHealth { get; private set; }

        [Header("Selection")]
        [SerializeField] private DecalProjector decalProjector;

        public abstract CombatTeam Team { get; }

        protected abstract UnitSO Config { get; }

        protected virtual void Start()
        {
            int maxHealth = Config != null ? Config.Health : MaxHealth;
            SetHealth(maxHealth, maxHealth);
        }

        public virtual void TakeDamage(DamageData damageData)
        {
            if (!DamageRules.CanDamage(damageData.AttackerTeam, Team))
                return;

            DoDamage(damageData.Amount);
        }

        public virtual void DoDamage(int damage)
        {
            if (CurrentHealth <= 0)
                return;

            CurrentHealth -= damage;

            if (CurrentHealth <= 0)
                Die();
        }

        public virtual void Heal(int healAmount)
        {
            if (CurrentHealth <= 0)
                return;

            CurrentHealth = Mathf.Min(CurrentHealth + healAmount, MaxHealth);
        }

        protected void SetHealth(int currentHealth, int maxHealth)
        {
            MaxHealth = Mathf.Max(1, maxHealth);
            CurrentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        }

        protected virtual void Die()
        {
            Destroy(gameObject);
        }

        public void Select()
        {
            if (decalProjector != null)
                decalProjector.gameObject.SetActive(true);

            Bus<UnitSelectedEvent>.Raise(new UnitSelectedEvent(this));
        }

        public void Deselect()
        {
            if (decalProjector != null)
                decalProjector.gameObject.SetActive(false);

            Bus<UnitDeselectedEvent>.Raise(new UnitDeselectedEvent(this));
        }
    }
}