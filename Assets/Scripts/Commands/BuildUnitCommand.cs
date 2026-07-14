using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.TechTree;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    [CreateAssetMenu(fileName = "Build Unit", menuName = "Buildiings/Commands/Build Unit", order = 120)]
    public class BuildUnitCommand : BaseCommand
    {
        // [field: SerializeField] private TechTreeSO techTree;
        [field: SerializeField] public AbstractUnitSO Unit { get; private set; }

        public override bool CanHandle(CommandContext context)
        {
            return context.Commandable is BaseBuilding;// && Unit.Cost.HasEnoughSupplies();
        }

        public override void Handle(CommandContext context)
        {
            // if (!Unit.Cost.HasEnoughSupplies()) return;

            BaseBuilding building = (BaseBuilding)context.Commandable;
            building.BuildUnlockable(Unit);
        }

        public override bool IsLocked(CommandContext context) => !Unit.CanAfford() || !Unit.TechTree.IsUnlocked(Unit);
    }
}