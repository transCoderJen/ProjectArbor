using System;
using UnityEngine;
using ShiftedSignal.Garden.EntitySpace;

namespace ShiftedSignal.Garden.Stats
{
    public class CharacterHealth : MonoBehaviour
    {
        [Header("Hearts")]
        [SerializeField] private int maxHearts = 3;
        [SerializeField] private int currentHearts;

        [Header("Damage")]
        [SerializeField] private bool isInvincible;

        private Entity entity;

        public int MaxHearts => maxHearts;
        public int CurrentHearts => currentHearts;

        public bool IsDead { get; private set; }

        public Action OnHealthChanged = delegate { };

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
    }
}