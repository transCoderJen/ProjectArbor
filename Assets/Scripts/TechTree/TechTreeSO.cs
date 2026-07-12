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

        private Dictionary<CombatTeam, Dictionary<UnlockableSO, Dependency>> techTrees;

        private void OnEnable()
        {
            // if (techTrees == null)
            // {
            //     BuildTechTrees();
            // }



        }

        void OnDisable()
        {
            techTrees = null;
        }

        private void BuildTechTrees()
        {
            techTrees = new Dictionary<CombatTeam, Dictionary<UnlockableSO, Dependency>>();
            Debug.Log($"Building Tech Tree {name}");

            foreach(CombatTeam owner in Enum.GetValues(typeof(CombatTeam)))
            {
                Debug.Log(($"Adding {owner} to Tech Tree Dictionary"));
                techTrees.Add(owner, new Dictionary<UnlockableSO, Dependency>());

                foreach(UnlockableSO unlockableSO in allUnlockables)
                {
                    techTrees[owner].Add(unlockableSO, new Dependency(unlockableSO));
                    Debug.Log($"Configuring {unlockableSO}'s {unlockableSO.UnlockRequirements.Count()} dependencies");
                }
            }
        }

        private readonly struct Dependency
        {
            public HashSet<UnlockableSO> Dependencies { get; }

            public Dependency(UnlockableSO unlockable)
            {
                Dependencies = new HashSet<UnlockableSO>(unlockable.UnlockRequirements);
            }
        }
    }
}