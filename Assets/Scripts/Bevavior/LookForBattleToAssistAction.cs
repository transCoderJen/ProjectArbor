using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.Interfaces;
using System.Collections.Generic;

namespace ShiftedSignal.Garden.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Look For Battle To Assist", story: "[Self] looks for another battle to assist", category: "Action/Units", id: "98cddd706aeb92abc4cc5155bd3a205e")]
    public partial class LookForBattleToAssistAction : Action
    {
        [SerializeReference]
        public BlackboardVariable<GameObject> Self;

        [SerializeReference]
        public BlackboardVariable<GameObject> RetaliationTarget;

        [SerializeReference]
        public BlackboardVariable<GameObject> DefenseTarget;

        [SerializeReference]
        public BlackboardVariable<bool> HasDefenseAssignment;

        [SerializeReference]
        public BlackboardVariable<List<GameObject>> NearbyEnemies;

        [SerializeField]
        private float checkInterval = 0.5f;

        private BaseMilitaryUnit militaryUnit;
        private float nextCheckTime;

        protected override Status OnStart()
        {
            if (Self == null ||
                Self.Value == null)
            {
                return Status.Failure;
            }

            militaryUnit =
                Self.Value.GetComponent<BaseMilitaryUnit>();

            if (militaryUnit == null)
                return Status.Failure;

            nextCheckTime = 0f;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (militaryUnit == null)
                return Status.Failure;

            // Higher-priority behavior appeared.
            if (HasValidRetaliationTarget())
                return Status.Success;

            if (HasValidDefenseTarget())
                return Status.Success;

            if (HasValidNearbyEnemy())
                return Status.Success;

            if (Time.time < nextCheckTime)
                return Status.Running;

            nextCheckTime =
                Time.time + checkInterval;

            if (MilitaryDefenseManager.Instance == null)
                return Status.Running;

            if (!MilitaryDefenseManager.Instance
                    .TryFindBattleToAssist(
                        militaryUnit,
                        out IDamageable assistTarget))
            {
                return Status.Running;
            }

            if (assistTarget == null ||
                assistTarget.CurrentHealth <= 0)
            {
                return Status.Running;
            }

            /*
             * Directly attack the enemy involved
             * in the other friendly unit's battle.
             */
            militaryUnit.Attack(
                assistTarget);

            return Status.Success;
        }

        private bool HasValidRetaliationTarget()
        {
            if (RetaliationTarget == null)
                return false;

            return IsLivingTarget(
                RetaliationTarget.Value);
        }

        private bool HasValidDefenseTarget()
        {
            if (HasDefenseAssignment == null ||
                !HasDefenseAssignment.Value)
            {
                return false;
            }

            if (DefenseTarget == null)
                return false;

            return IsLivingTarget(
                DefenseTarget.Value);
        }

        private bool HasValidNearbyEnemy()
        {
            if (NearbyEnemies == null ||
                NearbyEnemies.Value == null)
            {
                return false;
            }

            foreach (GameObject enemy
                     in NearbyEnemies.Value)
            {
                if (IsLivingTarget(enemy))
                    return true;
            }

            return false;
        }

        private bool IsLivingTarget(
            GameObject target)
        {
            if (target == null)
                return false;

            IDamageable damageable =
                target.GetComponentInParent<IDamageable>();

            return damageable != null &&
                   damageable.CurrentHealth > 0;
        }
    }
}

