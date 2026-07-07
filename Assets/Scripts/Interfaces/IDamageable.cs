using ShiftedSignal.Garden.Combat;
using Unity.VisualScripting;
using UnityEngine;

namespace ShiftedSignal.Garden.Interfaces
{
    public interface IDamageable
    {
        public int MaxHealth { get; }
        public int CurrentHealth { get; }
        public Transform Transform { get; }
        Vector3 TargetPoint { get; }

        CombatTeam Team { get; }
        TargetPriority TargetPriority { get; }

        virtual void TakeDamage(DamageData damageData)
        {
            
        }

        // public void TakeDamage(int damage);

        virtual public void Die()
        {
            
        }
    }
}