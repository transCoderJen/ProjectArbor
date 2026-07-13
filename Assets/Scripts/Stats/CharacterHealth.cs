using System;
using UnityEngine;
using ShiftedSignal.Garden.EntitySpace;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Interfaces;

namespace ShiftedSignal.Garden.Stats
{
    public class CharacterHealth : MonoBehaviour, IDamageable, IHealable
    {
        [Header("Hearts")]
        [SerializeField] private int maxHearts = 3;
        [SerializeField] private int currentHearts;

        [Header("Damage")]
        [SerializeField] private bool isInvincible;
        [SerializeField] private Owner owner;
        [SerializeField] private TargetPriority targetPriority;

        public Owner Owner => owner;

        private Entity entity;

        public int MaxHearts => maxHearts;
        public int CurrentHearts => currentHearts;

        public bool IsDead { get; private set; }

        public int MaxHealth => maxHearts;

        public int CurrentHealth => currentHearts;

        public Transform Transform => transform;

        public TargetPriority TargetPriority => targetPriority;

        public Action OnHealthChanged = delegate { };

        public virtual Vector3 TargetPoint
        {
            get
            {
                Collider collider = GetComponentInParent<Collider>();

                return collider != null
                    ? collider.bounds.center
                    : transform.position;
            }
        }

        protected virtual void Awake()
        {
            entity = GetComponent<Entity>();
        }

        protected virtual void Start()
        {
            currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);

            if (currentHearts <= 0)
                currentHearts = maxHearts;

            OnHealthChanged?.Invoke();
        }

        public virtual void TakeDamage(int amount, bool knockback = true, Transform attacker = null)
        {
            if (IsDead || isInvincible)
                return;

            amount = Mathf.Max(1, amount);

            if (entity != null)
                entity.DamageEffect(knockback, attacker);

            currentHearts = Mathf.Max(0, currentHearts - amount);

            OnHealthChanged?.Invoke();

            if (currentHearts <= 0)
                Die();
        }

        public virtual void Heal(int amount)
        {
            if (IsDead)
                return;

            amount = Mathf.Max(1, amount);

            currentHearts = Mathf.Min(maxHearts, currentHearts + amount);

            OnHealthChanged?.Invoke();
        }

        public virtual void RestoreFullHealth()
        {
            if (IsDead)
                return;

            currentHearts = maxHearts;

            OnHealthChanged?.Invoke();
        }

        public void SetMaxHearts(int value, bool restoreToFull = true)
        {
            maxHearts = Mathf.Max(1, value);

            if (restoreToFull)
                currentHearts = maxHearts;
            else
                currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);

            OnHealthChanged?.Invoke();
        }

        public void SetInvincible(bool value)
        {
            isInvincible = value;
        }

        protected virtual void Die()
        {
            if (IsDead)
                return;

            IsDead = true;

            if (entity != null)
                entity.Die();
        }

        public void TakeDamage(DamageData damageData)
        {
            if (!DamageRules.CanDamage(damageData.Owner, Owner))
                return;

            TakeDamage(
                damageData.Amount,
                damageData.Knockback,
                damageData.Attacker);
        }
    }
}