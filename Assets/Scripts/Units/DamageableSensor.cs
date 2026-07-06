using System.Collections.Generic;
using System.Linq;
using ShiftedSignal.Garden.Interfaces;
using Unity.Behavior;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    [RequireComponent(typeof(SphereCollider))]
    public class DamageableSensor : MonoBehaviour
    {
        public List<IDamageable> Damageables => damageables.ToList();

        public delegate void UnitDetectionEvent(IDamageable damageable);
        public event UnitDetectionEvent OnUnitEnter;
        public event UnitDetectionEvent OnUnitExit;

        private new SphereCollider collider;
        private HashSet<IDamageable> damageables = new();

        private void Awake()
        {
            collider = GetComponent<SphereCollider>();
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (collider.TryGetComponent(out IDamageable damageable))
            {
                damageables.Add(damageable);
                OnUnitEnter?.Invoke(damageable);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if (collider.TryGetComponent(out IDamageable damageable))
            {
                damageables.Remove(damageable);
                OnUnitExit?.Invoke(damageable);
            }
        }

        public void SetupFrom(AttackConfigSO attackConfig)
        {
            collider.radius = attackConfig.AttackRange;
        }
    }
}
