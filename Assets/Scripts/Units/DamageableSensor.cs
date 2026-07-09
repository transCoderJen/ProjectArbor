using System.Collections.Generic;
using System.Linq;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
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
        private readonly HashSet<IDamageable> damageables = new();

        private void Awake()
        {
            collider = GetComponent<SphereCollider>();
            collider.isTrigger = true;
        }

        private void OnEnable()
        {
            damageables.Clear();
            ScanInitialTargets();
        }

        private void OnDisable()
        {
            Bus<UnitDeathEvent>.OnEvent -= HandleUnitDeath;
            damageables.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();

            if (damageable == null)
                return;

            if (!damageables.Add(damageable))
                return;

            OnUnitEnter?.Invoke(damageable);

            if (damageables.Count == 1)
                Bus<UnitDeathEvent>.OnEvent += HandleUnitDeath;
        }

        private void OnTriggerExit(Collider other)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();

            if (damageable == null)
                return;

            RemoveDamageable(damageable);
        }

        private void OnDestroy()
        {
            Bus<UnitDeathEvent>.OnEvent -= HandleUnitDeath;
        }

        private void HandleUnitDeath(UnitDeathEvent evt)
        {
            RemoveDamageable(evt.Unit);
        }

        private void RemoveDamageable(IDamageable damageable)
        {
            if (damageable == null)
                return;

            damageables.Remove(damageable);

            OnUnitExit?.Invoke(damageable);

            if (damageables.Count == 0)
                Bus<UnitDeathEvent>.OnEvent -= HandleUnitDeath;
        }

        private readonly List<IDamageable> damageablesToRemove = new();

        public IDamageable GetNearestValidTarget(Vector3 origin, CombatTeam attackerTeam)
        {
            IDamageable nearestTarget = null;
            float nearestDistanceSqr = Mathf.Infinity;

            damageablesToRemove.Clear();

            foreach (IDamageable damageable in damageables)
            {
                if (damageable is not Component damageableComponent || damageableComponent == null)
                {
                    damageablesToRemove.Add(damageable);
                    continue;
                }

                if (!DamageRules.CanDamage(attackerTeam, damageable.Team))
                    continue;

                Vector3 targetPoint = damageable.TargetPoint;
                float distanceSqr = (targetPoint - origin).sqrMagnitude;

                if (distanceSqr >= nearestDistanceSqr)
                    continue;

                nearestDistanceSqr = distanceSqr;
                nearestTarget = damageable;
            }

            foreach (IDamageable damageable in damageablesToRemove)
            {
                damageables.Remove(damageable);
            }

            return nearestTarget;
        }

        private void ScanInitialTargets()
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                collider.radius,
                ~0,
                QueryTriggerInteraction.Ignore);

            foreach (Collider hit in hits)
            {
                IDamageable damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable == null)
                    continue;

                damageables.Add(damageable);
            }

            if (damageables.Count > 0)
                Bus<UnitDeathEvent>.OnEvent += HandleUnitDeath;
        }

        public void SetupFrom(AttackConfigSO attackConfig)
        {
            if (attackConfig == null)
                return;

            collider.radius = attackConfig.AttackRange;
        }
    }
}