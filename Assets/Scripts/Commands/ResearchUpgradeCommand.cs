
using System.Linq;
using NUnit.Framework;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.TechTree;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    [CreateAssetMenu(fileName = "Research Upgrade", menuName = "Tech Tree/Research Upgrade Command", order = 140)]
    public class ResearchUpgradeCommand : BaseCommand
    {
        [field: SerializeField] public UpgradeSO Upgrade { get; private set; }

        public override bool CanHandle(CommandContext context)
        {
            return context.Commandable is BaseBuilding;
        }

        public override void Handle(CommandContext context)
        {
            BaseBuilding building = context.Commandable as BaseBuilding;

            if (Upgrade.CanAfford())
            {
                building.BuildUnlockable(Upgrade);
            }
        }

        public override bool IsLocked(CommandContext context){
            bool isLocked = !Upgrade.CanAfford() || !Upgrade.TechTree.IsUnlocked(Upgrade);

            if (!isLocked && Upgrade.IsOneTimeUnlock && context.Commandable != null && context.Commandable is BaseBuilding building)
            {
                isLocked = building.Queue.Contains(Upgrade);
            }

            return isLocked;
        }

        public override bool IsAvailable(CommandContext context)
        {
            if (Upgrade.IsOneTimeUnlock && Upgrade.TechTree.IsResearched(Upgrade))
            {
                return false;
            }

            return Upgrade.TechTree.IsUnlocked(Upgrade);
        }
        
    }
}