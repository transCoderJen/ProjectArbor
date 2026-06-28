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
            Debug.Log("[FarmCommand] CanHandle called.");

            if (context.Commandable is not Worker)
            {
                Debug.Log("[FarmCommand] Failed - Commandable is not a Worker.");
                return false;
            }

            Debug.Log("[FarmCommand] Worker detected.");

            if (context.Hit.collider != null)
            {
                Debug.Log($"[FarmCommand] Hit collider: {context.Hit.collider.name}");
            }
            else
            {
                Debug.Log("[FarmCommand] No collider hit.");
            }

            if (IsFarmSupplySource(context.Hit.collider))
            {
                Debug.Log("[FarmCommand] Success - Hit is a Farm Supply Source.");
                return true;
            }

            GrowBlock hoveredBlock = GetHoveredGrowBlock();

            if (hoveredBlock == null)
            {
                Debug.Log("[FarmCommand] Failed - No hovered GrowBlock.");
                return false;
            }

            Debug.Log(
                $"[FarmCommand] Hovered Block - Active: {hoveredBlock.IsActive}, " +
                $"Stage: {hoveredBlock.CurrentStage}"
            );

            bool result =
                hoveredBlock.IsActive &&
                hoveredBlock.CurrentStage >= GrowBlock.GrowthStage.Ploughed;

            Debug.Log($"[FarmCommand] Returning {result}");

            return result;
        }

        public override void Handle(CommandContext context)
        {
            Debug.Log("[FarmCommand] Handle called.");

            if (context.Commandable is not Worker worker)
            {
                Debug.Log("[FarmCommand] Handle failed - Commandable is not a Worker.");
                return;
            }

            Debug.Log($"[FarmCommand] Calling Farm() on {worker.name}");

            worker.Farm();
        }

        private bool IsFarmSupplySource(Collider collider)
        {
            if (collider == null)
            {
                Debug.Log("[FarmCommand] IsFarmSupplySource - Collider is null.");
                return false;
            }

            bool result = collider.GetComponentInParent<IFarmSupplySource>() != null;

            Debug.Log($"[FarmCommand] IsFarmSupplySource = {result}");

            return result;
        }

        private GrowBlock GetHoveredGrowBlock()
        {
            if (GridManager.Instance == null)
            {
                Debug.Log("[FarmCommand] GridManager.Instance is null.");
                return null;
            }

            GrowBlock block = GridManager.Instance.GetBlock();

            Debug.Log(block == null
                ? "[FarmCommand] GridManager returned null block."
                : $"[FarmCommand] GridManager returned block: {block.name}");

            return block;
        }
    }
}