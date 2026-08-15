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

        public Owner Owner { get; private set; }

        public delegate void UnitDetectionEvent(IDamageable damageable);
        public event UnitDetectionEvent OnUnitEnter;
        public event UnitDetectionEvent OnUnitExit;

        private new SphereCollider collider;

        private readonly HashSet<IDamageable> damageables = new();
        private readonly List<IDamageable> damageablesToRemove = new();

        private bool subscribedToDeathEvent;

        private void Awake()
        {
            collider = GetComponent<SphereCollider>();
            collider.isTrigger = true;
        }

        private void OnEnable()
        {
            damageables.Clear();
        }

        private void OnDisable()
        {
            UnsubscribeFromDeathEvent();
            damageables.Clear();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDeathEvent();
        }

        private void OnTriggerEnter(Collider other)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();

            if (!IsValidTarget(damageable))
                return;

            if (!damageables.Add(damageable))
                return;

            SubscribeToDeathEvent();

            OnUnitEnter?.Invoke(damageable);
        }

        private void OnTriggerExit(Collider other)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();

            if (damageable == null)
                return;

            if (!damageables.Remove(damageable))
                return;

            OnUnitExit?.Invoke(damageable);

            if (damageables.Count == 0)
                UnsubscribeFromDeathEvent();
        }

        // public void SetupFrom(AttackConfigSO attackConfig)
        // {
        //     if (attackConfig == null)
        //         return;

        //     collider.radius = attackConfig.Range;
        // }

        public void SetupFrom(AttackConfigSO attackConfig, Owner owner)
        {
            Owner = owner;

            if (attackConfig != null)
                collider.radius = attackConfig.Range;

            // OnEnable already happened before AbstractUnit.Start,
            // so scan again after the radius and owner are configured.
            ScanInitialTargets();
        }

        private bool IsValidTarget(IDamageable damageable)
        {
            if (damageable == null)
                return false;

            if (damageable is not Component component || component == null)
                return false;

            if (damageable.CurrentHealth <= 0)
                return false;

            if (damageable.Owner == Owner)
                return false;

            return DamageRules.CanDamage(Owner, damageable.Owner);
        }

        private void ScanInitialTargets()
        {
            damageables.Clear();

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                collider.radius,
                ~0,
                QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                IDamageable damageable =
                    hit.GetComponentInParent<IDamageable>();

                if (!IsValidTarget(damageable))
                    continue;

                if (!damageables.Add(damageable))
                    continue;

                Debug.Log(
                    $"{name} INITIAL TARGET: {damageable.Transform.name}, " +
                    $"owner: {damageable.Owner}");

                OnUnitEnter?.Invoke(damageable);
            }

            if (damageables.Count > 0)
                SubscribeToDeathEvent();
        }

        private void HandleUnitDeath(UnitDeathEvent evt)
        {
            if (evt.Unit == null)
                return;

            if (!damageables.Remove(evt.Unit))
                return;

            Debug.Log($"{name} REMOVED DEAD TARGET: {evt.Unit.name}");

            OnUnitExit?.Invoke(evt.Unit);

            if (damageables.Count == 0)
                UnsubscribeFromDeathEvent();
        }

        public IDamageable GetNearestValidTarget(
            Vector3 origin,
            Owner attackerOwner)
        {
            IDamageable nearestTarget = null;
            float nearestDistanceSqr = Mathf.Infinity;

            damageablesToRemove.Clear();

            foreach (IDamageable damageable in damageables)
            {
                if (damageable is not Component component ||
                    component == null)
                {
                    damageablesToRemove.Add(damageable);
                    continue;
                }

                if (!DamageRules.CanDamage(
                        attackerOwner,
                        damageable.Owner))
                {
                    continue;
                }

                float distanceSqr =
                    (damageable.TargetPoint - origin).sqrMagnitude;

                if (distanceSqr >= nearestDistanceSqr)
                    continue;

                nearestDistanceSqr = distanceSqr;
                nearestTarget = damageable;
            }

            foreach (IDamageable damageable in damageablesToRemove)
                damageables.Remove(damageable);

            return nearestTarget;
        }

        private void SubscribeToDeathEvent()
        {
            if (subscribedToDeathEvent)
                return;

            Bus<UnitDeathEvent>.OnEvent += HandleUnitDeath;
            subscribedToDeathEvent = true;
        }

        private void UnsubscribeFromDeathEvent()
        {
            if (!subscribedToDeathEvent)
                return;

            Bus<UnitDeathEvent>.OnEvent -= HandleUnitDeath;
            subscribedToDeathEvent = false;
        }
    }
}








