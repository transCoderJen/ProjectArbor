using ShiftedSignal.Garden.Interfaces;
using UnityEngine;

namespace ShiftedSignal.Garden.Behavior
{
    public interface IAttacker
    {
        Transform ProjectileSpawnPoint { get; }
        public void Attack(IDamageable damageable);
        public void Attack(Vector3 location);
        public Transform Transform { get; }
    }
}