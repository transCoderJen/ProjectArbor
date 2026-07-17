using System;
using System.Collections.Generic;
using System.Linq;
using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using UnityEngine;

namespace ShiftedSignal.Garden.TechTree
{
    [CreateAssetMenu(fileName = "Tech Tree", menuName = "Tech Tree/Tech Tree", order = 1)]
    public class TechTreeSO : ScriptableObject
    {
        [SerializeField] private List<UnlockableSO> allUnlockables = new();

        public IEnumerable<UnlockableSO> AllUnlockables => allUnlockables.ToList();

        private Dictionary<UnlockableSO, Dependency> techTree;
        private HashSet<UnlockableSO> unlockedDependencies;

        public bool IsUnlocked(UnlockableSO unlockable) => techTree.TryGetValue(unlockable, out Dependency dependency)
            && dependency.IsUnlocked;
        public bool IsResearched(UnlockableSO unlockable) => unlockedDependencies.Contains(unlockable);

        private void OnEnable()
        {
            if (techTree == null)
            {
                BuildTechTrees();
            }

            Bus<BuildingSpawnEvent>.OnEvent += HandleBuildingSpawn;
            Bus<BuildingDeathEvent>.OnEvent += HandleBuildingDeath;
            Bus<UpgradeResearchEvent>.OnEvent += HandleUpgradeResearch;
        }

        void OnDisable()
        {
            techTree = null;
            Bus<BuildingSpawnEvent>.OnEvent -= HandleBuildingSpawn;
            Bus<BuildingDeathEvent>.OnEvent -= HandleBuildingDeath;
            Bus<UpgradeResearchEvent>.OnEvent -= HandleUpgradeResearch;
        }

        private void HandleBuildingDeath(BuildingDeathEvent evt)
        {
            if (evt.Building == null)
                return;

            UnlockableSO spawnedBuilding = evt.Building.UnitSO;

            foreach (Dependency dependency in techTree.Values)
            {
                dependency.LoseDependency(spawnedBuilding);
            }
        }

        private void HandleUpgradeResearch(UpgradeResearchEvent evt)
        {
            Debug.Log($"Upgrade {evt.Upgrade.Name} applied" );
            unlockedDependencies.Add(evt.Upgrade);

            foreach(KeyValuePair<UnlockableSO, Dependency> keyValuePair in techTree)
            {
                keyValuePair.Value.UnlockDependency(evt.Upgrade);
            }
        }

        private void HandleBuildingSpawn(BuildingSpawnEvent evt)
        {
            if (evt.Building == null)
                return;

            UnlockableSO spawnedBuilding = evt.Building.UnitSO;

            foreach (Dependency dependency in techTree.Values)
            {
                dependency.UnlockDependency(spawnedBuilding);
            }
        }

        private void BuildTechTrees()
        {
            techTree = new Dictionary<UnlockableSO, Dependency>(
                allUnlockables.Count
            );

            unlockedDependencies = new HashSet<UnlockableSO>();

            foreach (UnlockableSO unlockableSO in allUnlockables)
            {
                techTree.Add(
                    unlockableSO,
                    new Dependency(unlockableSO)
                );

                Debug.Log(
                    $"Configuring {unlockableSO.name} with " +
                    $"{unlockableSO.UnlockRequirements.Count()} dependencies"
                );
            }
        }

        public IEnumerable<UnlockableSO> GetUnmetDependencies(UnlockableSO unlockable)
        {
            if (unlockable == null)
                return Enumerable.Empty<UnlockableSO>();

            if (!techTree.TryGetValue(unlockable, out Dependency dependency))
                return Enumerable.Empty<UnlockableSO>();

            return dependency.UnmetDependencies;
        }

        private readonly struct Dependency
        {
            public HashSet<UnlockableSO> Dependencies { get; }
            public bool IsUnlocked => Dependencies.Count == metDependencies.Count;
            private readonly Dictionary<UnlockableSO, int> metDependencies;

            public IEnumerable<UnlockableSO> UnmetDependencies
            {
                get
                {
                    List<UnlockableSO> unmet = new();

                    foreach (UnlockableSO dependency in Dependencies)
                    {
                        if (!metDependencies.ContainsKey(dependency))
                        {
                            unmet.Add(dependency);
                        }
                    }

                    return unmet;
                }
            }

            public Dependency(UnlockableSO unlockable)
            {
                Dependencies = new HashSet<UnlockableSO>(unlockable.UnlockRequirements);
                metDependencies = new Dictionary<UnlockableSO, int>(Dependencies.Count);
            }

            public void UnlockDependency(UnlockableSO dependency)
            {
                if (Dependencies.Contains(dependency) && !metDependencies.TryAdd(dependency, 1))
                {
                    metDependencies[dependency]++;
                }
            }

            public void LoseDependency(UnlockableSO dependency)
            {
                if (dependency.IsOneTimeUnlock || !metDependencies.TryGetValue(dependency, out int count)) return;

                count--;

                if (count > 0)
                {
                    metDependencies[dependency] = count;
                }
                else
                {
                    metDependencies.Remove(dependency);
                }


            }
        }
    }
}