using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    [CreateAssetMenu(fileName = "Farm Command", menuName = "Units/Commands/Farm", order = 106)]
    public class FarmCommand : BaseCommand
    {
        public override bool CanHandle(CommandContext context)
        {
            if (context.Commandable is not Worker)
                return false;

            if (IsFarmSupplySource(context.Hit.collider))
                return true;

            GrowBlock hoveredBlock = GetHoveredGrowBlock();

            return hoveredBlock != null
                && hoveredBlock.IsActive
                && hoveredBlock.CurrentStage >= GrowBlock.GrowthStage.Ploughed;
        }

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is not Worker worker)
                return;

            worker.Farm();
        }

        private bool IsFarmSupplySource(Collider collider)
        {
            if (collider == null)
                return false;

            return collider.GetComponentInParent<IFarmSupplySource>() != null;
        }

        private GrowBlock GetHoveredGrowBlock()
        {
            if (GridManager.Instance == null)
                return null;

            return GridManager.Instance.GetBlock();
        }
    }
}