using System;
using System.Collections.Generic;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.TechTree;
using UnityEngine;

namespace ShiftedSignal.Garden.Managers
{
    public class UpgradeManager : Singleton<UpgradeManager>
    {
        [SerializeField]
        private UpgradeBaselineDatabaseSO baselineDatabase;

        private readonly HashSet<UpgradeSO> appliedUpgrades = new();

        public IReadOnlyCollection<UpgradeSO> AppliedUpgrades =>
            appliedUpgrades;

        protected override void Awake()
        {
            base.Awake();

            if (baselineDatabase != null)
            {
                baselineDatabase.RestoreAll();
            }
            else
            {
                Debug.LogWarning(
                    $"{nameof(UpgradeManager)} has no baseline database assigned."
                );
            }

            appliedUpgrades.Clear();

            Bus<UpgradeResearchEvent>.OnEvent += HandleUpgradeResearch;
        }

        private void OnApplicationQuit()
        {
            RestoreBaselines();
        }

        protected override void OnDestroy()
        {
            Bus<UpgradeResearchEvent>.OnEvent -= HandleUpgradeResearch;

            RestoreBaselines();

            base.OnDestroy();
        }

        private void RestoreBaselines()
        {
            if (baselineDatabase == null)
            {
                return;
            }

            baselineDatabase.RestoreAll();
        }

        private void HandleUpgradeResearch(UpgradeResearchEvent evt)
        {
            if (evt.Upgrade == null)
            {
                Debug.LogWarning(
                    $"{nameof(UpgradeManager)} received an " +
                    $"{nameof(UpgradeResearchEvent)} with a null upgrade."
                );

                return;
            }

            ApplyUpgrade(evt.Upgrade);
        }

        public bool ApplyUpgrade(UpgradeSO upgrade)
        {
            if (upgrade == null)
            {
                Debug.LogWarning(
                    $"{nameof(UpgradeManager)} cannot apply a null upgrade."
                );

                return false;
            }

            if (appliedUpgrades.Contains(upgrade))
            {
                Debug.Log(
                    $"Upgrade '{upgrade.name}' has already been applied. " +
                    "Skipping duplicate application."
                );

                return false;
            }

            if (upgrade.TargetObjects == null ||
                upgrade.TargetObjects.Count == 0)
            {
                Debug.LogWarning(
                    $"Upgrade '{upgrade.name}' has no target objects assigned."
                );

                return false;
            }

            bool appliedToAtLeastOneTarget = false;

            for (int i = 0; i < upgrade.TargetObjects.Count; i++)
            {
                ScriptableObject targetObject =
                    upgrade.TargetObjects[i];

                if (targetObject == null)
                {
                    Debug.LogWarning(
                        $"Upgrade '{upgrade.name}' has a null target object " +
                        $"at index {i}. Skipping that target."
                    );

                    continue;
                }

                try
                {
                    Debug.Log(
                        $"Applying upgrade '{upgrade.name}' to " +
                        $"'{targetObject.name}'."
                    );

                    upgrade.Apply(targetObject);

                    appliedToAtLeastOneTarget = true;

                    Debug.Log(
                        $"Upgrade '{upgrade.name}' was applied to " +
                        $"'{targetObject.name}'."
                    );
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Upgrade '{upgrade.name}' failed while being applied " +
                        $"to target '{targetObject.name}'.\n" +
                        exception
                    );
                }
            }

            if (!appliedToAtLeastOneTarget)
            {
                Debug.LogWarning(
                    $"Upgrade '{upgrade.name}' was not applied to any targets."
                );

                return false;
            }

            appliedUpgrades.Add(upgrade);

            Debug.Log(
                $"Upgrade '{upgrade.name}' was successfully applied."
            );

            return true;
        }

        public bool IsApplied(UpgradeSO upgrade)
        {
            return upgrade != null &&
                   appliedUpgrades.Contains(upgrade);
        }
    }
}