using System.Linq;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.TechTree;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    [CreateAssetMenu(
        fileName = "Research Upgrade",
        menuName = "Tech Tree/Research Upgrade Command",
        order = 140)]
    public class ResearchUpgradeCommand : BaseCommand
    {
        [field: SerializeField]
        public UpgradeSO Upgrade { get; private set; }

        private BaseBuilding researchingBuilding;
        private BaseBuilding.QueueUpdatedEvent updateQueue;

        private bool IsBeingResearched => updateQueue != null;

        public override bool CanHandle(CommandContext context)
        {
            return context.Commandable is BaseBuilding;
        }

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is not BaseBuilding building)
                return;

            if (!Upgrade.CanAfford())
                return;

            if (!Upgrade.TechTree.IsUnlocked(Upgrade))
                return;

            if (Upgrade.IsOneTimeUnlock && IsBeingResearched)
                return;

            researchingBuilding = building;
            updateQueue = GetQueueUpdatedFunction(building);

            building.OnQueueUpdated += updateQueue;

            building.BuildUnlockable(Upgrade);
        }

        private BaseBuilding.QueueUpdatedEvent GetQueueUpdatedFunction(
            BaseBuilding building)
        {
            return unlockables =>
                HandleQueueUpdated(building, unlockables);
        }

        private void HandleQueueUpdated(
            BaseBuilding building,
            UnlockableSO[] unlockablesInQueue)
        {
            Debug.Log($"Handle Queue Updated in {Name}");

            if (unlockablesInQueue.Contains(Upgrade))
                return;

            StopTrackingResearch();
        }

        private void StopTrackingResearch()
        {
            if (researchingBuilding != null && updateQueue != null)
            {
                researchingBuilding.OnQueueUpdated -= updateQueue;
            }

            researchingBuilding = null;
            updateQueue = null;
        }

        public override bool IsLocked(CommandContext context)
        {
            if (!Upgrade.CanAfford())
                return true;

            if (!Upgrade.TechTree.IsUnlocked(Upgrade))
                return true;

            if (Upgrade.IsOneTimeUnlock && IsBeingResearched)
                return true;

            return false;
        }

        public override bool IsAvailable(CommandContext context)
        {
            if (Upgrade.IsOneTimeUnlock &&
                Upgrade.TechTree.IsResearched(Upgrade))
            {
                return false;
            }

            return Upgrade.TechTree.IsUnlocked(Upgrade);
        }

        private void OnDisable()
        {
            StopTrackingResearch();
        }
    }
}