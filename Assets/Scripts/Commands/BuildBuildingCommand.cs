using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.TechTree;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    [CreateAssetMenu(
        fileName = "Build Building",
        menuName = "Units/Commands/Build Building")]
    public class BuildBuildingCommand : BaseCommand
    {
        [field: SerializeField] public bool AllowDragPlacement { get; private set; }
        [field: SerializeField] public BuildingSO Building { get; private set; }

        public override bool CanHandle(CommandContext context)
        {
            return context.Commandable is IBuildingBuilder
                   && Building != null
                   && Building.Prefab != null;
        }

        public override void Activate(AbstractCommandable commandable)
        {
            if (commandable is not IBuildingBuilder)
                return;

            ActivatePlacement();
        }

        public void ActivatePlacement()
        {
            if (Building == null || Building.Prefab == null)
            {
                Debug.LogWarning(
                    "Build command has no valid Building or Prefab.");

                return;
            }

            if (Player.Instance == null)
            {
                Debug.LogWarning("Player.Instance is null.");
                return;
            }

            Player.Instance.BeginBuildingPlacement(
                Building,
                AllowDragPlacement);

            Debug.Log(
                $"After BeginBuildingPlacement: " +
                $"IsPlacingBuilding={Player.Instance.IsPlacingBuilding}");
        }

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is not IBuildingBuilder)
                return;

            GrowBlock targetBlock = Player.Instance.GetBlock();

            if (targetBlock == null)
                return;

            Player.Instance.TryPlaceSelectedBuilding(targetBlock);
        }

        public override bool IsLocked(CommandContext context) => !Building.CanAfford() || !Building.TechTree.IsUnlocked(Building);

        
    }
}