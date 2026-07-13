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

        public bool IsUnlocked(UnlockableSO unlockable) => techTree.TryGetValue(unlockable, out Dependency dependency)
            && dependency.IsUnlocked;

        private void OnEnable()
        {
            if (techTree == null)
            {
                BuildTechTrees();
            }

            Bus<BuildingSpawnEvent>.OnEvent += HandleBuildingSpawn;
        }

        void OnDisable()
        {
            techTree = null;
            Bus<BuildingSpawnEvent>.OnEvent -= HandleBuildingSpawn;
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
                allUnlockables.Count);

            foreach(UnlockableSO unlockableSO in allUnlockables)
            {
                techTree.Add(unlockableSO, new Dependency(unlockableSO));
                Debug.Log($"Configuring {unlockableSO}'s {unlockableSO.UnlockRequirements.Count()} dependencies");
            }   
        }

        private readonly struct Dependency
        {
            public HashSet<UnlockableSO> Dependencies { get; }
            public bool IsUnlocked => Dependencies.Count == metDependencies.Count;
            private readonly Dictionary<UnlockableSO, int> metDependencies;

            public Dependency(UnlockableSO unlockable)
            {
                Dependencies = new HashSet<UnlockableSO>(unlockable.UnlockRequirements);
                metDependencies = new Dictionary<UnlockableSO, int>(Dependencies.Count);
            }

            public void UnlockDependency(UnlockableSO dependency)
            {
                Debug.Log($"Attempting to unlock dependancy { dependency.Name}");

                if (Dependencies.Contains(dependency) && !metDependencies.TryAdd(dependency, 1))
                {
                    metDependencies[dependency]++;
                }
            }
        }
    }
}