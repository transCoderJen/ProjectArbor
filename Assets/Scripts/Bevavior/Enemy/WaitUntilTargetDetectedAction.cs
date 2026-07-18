using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Combat;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wait Until Target Detected", story: "[Self] waits until a valid target is detected", category: "Unit/Enemy", id: "2075014691dc53ea08c0f3ef482b8ed6")]
public partial class WaitUntilTargetDetectedAction : Action
    {
        [SerializeReference]
        public BlackboardVariable<GameObject> Self;

        private DamageableSensor sensor;
        private AbstractCommandable self;

        private bool targetDetected;

        protected override Status OnStart()
        {
            targetDetected = false;

            if (Self?.Value == null)
                return Status.Failure;

            self = Self.Value.GetComponent<AbstractCommandable>();
            sensor = Self.Value.GetComponentInChildren<DamageableSensor>();

            if (self == null || sensor == null)
                return Status.Failure;

            sensor.OnUnitEnter += HandleTargetEntered;

            // A target might already be inside the sensor before this node starts.
            if (HasValidTarget())
                targetDetected = true;

            return targetDetected
                ? Status.Success
                : Status.Running;
        }

        protected override Status OnUpdate()
            {
                return targetDetected
                    ? Status.Success
                    : Status.Running;
            }

            protected override void OnEnd()
            {
                if (sensor != null)
                    sensor.OnUnitEnter -= HandleTargetEntered;

                sensor = null;
                self = null;
            }

            private void HandleTargetEntered(IDamageable damageable)
            {
                if (IsValidTarget(damageable))
                    targetDetected = true;
            }

            private bool HasValidTarget()
            {
                foreach (IDamageable damageable in sensor.Damageables)
                {
                    if (IsValidTarget(damageable))
                        return true;
                }

                return false;
            }

            private bool IsValidTarget(IDamageable damageable)
            {
                if (damageable == null || damageable.CurrentHealth <= 0)
                    return false;

                if (damageable is not AbstractCommandable commandable)
                    return false;

                if (commandable == self)
                    return false;

                return DamageRules.CanDamage(self.Owner, commandable.Owner);
            }
        }


