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
        [field: SerializeField] public BaseCommand[] AvailableCommands { get; private set; }

        [Header("Selection")]
        [SerializeField] private DecalProjector decalProjector;

        [Header("Targeting")]
        [SerializeField] private TargetPriority targetPriority;

        protected abstract AbstractUnitSO config { get; }

        public AbstractUnitSO Config => config;

        public Transform Transform => transform;
        public Owner Owner => config != null ? config.Team : Owner.Unowned;
        public TargetPriority TargetPriority => targetPriority;

        private BaseCommand[] initialCommands;

        public delegate void HealthUpdatedEvent(AbstractCommandable commandable, int lastHealth, int newHealth);
        public event HealthUpdatedEvent OnHealthUpdated;

        public virtual Vector3 TargetPoint
        {
            get
            {
                Collider[] colliders = GetComponentsInChildren<Collider>();

                Bounds? combinedBounds = null;

                foreach (Collider collider in colliders)
                {
                    if (collider == null)
                        continue;

                    if (collider.isTrigger)
                        continue;

                    if (combinedBounds == null)
                    {
                        combinedBounds = collider.bounds;
                    }
                    else
                    {
                        Bounds bounds = combinedBounds.Value;
                        bounds.Encapsulate(collider.bounds);
                        combinedBounds = bounds;
                    }
                }

                if (combinedBounds.HasValue)
                    return combinedBounds.Value.center;

                return transform.position;
            }
        }

        protected virtual void Start()
        {
            int maxHealth = config != null ? config.Health : MaxHealth;

            SetHealth(maxHealth, maxHealth);

            initialCommands = AvailableCommands;
        }

        public virtual void TakeDamage(DamageData damageData)
        {
            Debug.Log(
                $"{name} TakeDamage | " +
                $"AttackerTeam: {damageData.Owner} | " +
                $"TargetTeam: {Owner} | " +
                $"RulesPass: {DamageRules.CanDamage(damageData.Owner, Owner)}");

            if (!DamageRules.CanDamage(damageData.Owner, Owner))
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
            AvailableCommands =
                commands == null || commands.Length == 0
                    ? initialCommands
                    : commands;

            Bus<UnitSelectedEvent>.Raise(new UnitSelectedEvent(this));
        }
    }
}