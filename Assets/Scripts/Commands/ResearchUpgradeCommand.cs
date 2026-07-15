
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

        public override bool IsLocked(CommandContext context) => !Upgrade.CanAfford() || !Upgrade.TechTree.IsUnlocked(Upgrade);

        public override bool IsAvailable(CommandContext context)
        {
            Debug.Log($"{Upgrade.Name} is researched {Upgrade.TechTree.IsResearched(Upgrade)}");
            if (Upgrade.IsOneTimeUnlock && Upgrade.TechTree.IsResearched(Upgrade))
            {
                return false;
            }

            return Upgrade.TechTree.IsUnlocked(Upgrade);
        }
        
    }
}