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

            baselineDatabase.RestoreAll();

            appliedUpgrades.Clear();

            Bus<UpgradeResearchEvent>.OnEvent += HandleUpgradeResearch;
        }

        private void OnApplicationQuit()
        {
            baselineDatabase.RestoreAll();
        }

        protected override void OnDestroy()
        {
            Bus<UpgradeResearchEvent>.OnEvent -= HandleUpgradeResearch;

            baselineDatabase.RestoreAll();

            base.OnDestroy();
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
                    $"Upgrade {upgrade.name} has already been applied. " +
                    $"Skipping duplicate application."
                );

                return false;
            }

            try
            {
                Debug.Log($"Applying upgrade {upgrade.name}.");

                upgrade.Apply();

                appliedUpgrades.Add(upgrade);

                Debug.Log(
                    $"Upgrade {upgrade.name} was successfully applied."
                );

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Upgrade {upgrade.name} failed while being applied.\n" +
                    exception
                );

                return false;
            }
        }

        public bool IsApplied(UpgradeSO upgrade)
        {
            return upgrade != null && appliedUpgrades.Contains(upgrade);
        }
    }
}