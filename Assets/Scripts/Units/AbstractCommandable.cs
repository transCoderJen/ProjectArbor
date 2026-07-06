using System.Windows.Input;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Commands;
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
        [field: SerializeField] public int CurrentHealth { get; private set; }
        [field: SerializeField] public int MaxHealth { get; private set; }
        public Transform Transform => transform;
        [field: SerializeField] public BaseCommand[] AvailableCommands { get; private set; }

        [Header("Selection")]
        [SerializeField] private DecalProjector decalProjector;

        [SerializeField] private TargetPriority targetPriority;

        public abstract CombatTeam Team { get; }

        protected abstract AbstractUnitSO Config { get; }

        public TargetPriority TargetPriority => targetPriority;

        private BaseCommand[] initialCommands;

        public delegate void HealthUpdatedEvent(AbstractCommandable commandable, int lastHealth, int newHealth);
        public event HealthUpdatedEvent OnHealthUpdated;

        protected virtual void Start()
        {
            int maxHealth = Config != null ? Config.Health : MaxHealth;
            SetHealth(maxHealth, maxHealth);
            initialCommands = AvailableCommands;
        }

        public virtual void TakeDamage(DamageData damageData)
        {
            if (!DamageRules.CanDamage(damageData.AttackerTeam, Team))
                return;

            if (CurrentHealth <= 0)
                return;

            SetHealth(CurrentHealth - damageData.Amount, MaxHealth);

            if (CurrentHealth <= 0)
                Die();
        }

        public virtual void Heal(int healAmount)
        {
            if (CurrentHealth <= 0)
                return;

            SetHealth(CurrentHealth + healAmount, MaxHealth);
        }

        protected void SetHealth(int currentHealth, int maxHealth)
        {
            int lastHealth = CurrentHealth;

            MaxHealth = Mathf.Max(1, maxHealth);
            CurrentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);

            if (lastHealth != CurrentHealth)
                OnHealthUpdated?.Invoke(this, lastHealth, CurrentHealth);
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

                SetCommandOverrides(null);

            Bus<UnitDeselectedEvent>.Raise(new UnitDeselectedEvent(this));
        }

        public void SetCommandOverrides(BaseCommand[] commands)
        {
            if (commands == null || commands.Length == 0)
            {
                AvailableCommands = initialCommands;
            }
            else
            {
                AvailableCommands = commands;
            }

            Bus<UnitSelectedEvent>.Raise(new UnitSelectedEvent(this));
        }
    }
}