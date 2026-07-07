using System;
using System.Collections.Generic;
using System.Linq;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using Unity.Behavior;
using UnityEditor.Rendering;
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

            if (damageables.Count == 1)
            {
                Bus<UnitDeathEvent>.OnEvent += HandleUnitDeath;
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if (collider.TryGetComponent(out IDamageable damageable))
            {
                damageables.Remove(damageable);
                OnUnitExit?.Invoke(damageable);
            }

            if (damageables.Count == 0)
            {
                Bus<UnitDeathEvent>.OnEvent -= HandleUnitDeath;
            }
        }

        private void OnDestroy()
        {
            Bus<UnitDeathEvent>.OnEvent -= HandleUnitDeath;
        }

        private void HandleUnitDeath(UnitDeathEvent evt)
        {
            if (Damageables.Remove(evt.Unit))
            {
                OnTriggerExit(evt.Unit.GetComponent<Collider>());
            }
        }

        public void SetupFrom(AttackConfigSO attackConfig)
        {
            collider.radius = attackConfig.AttackRange;
        }
    }
}
