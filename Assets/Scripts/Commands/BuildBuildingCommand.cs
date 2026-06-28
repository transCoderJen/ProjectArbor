using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    public class BuildBuildingCommand : BaseCommand
    {
        [field: SerializeField] public BuildingSO Building { get; private set; }
        public override bool CanHandle(CommandContext context)
        {
            return context.Commandable is IBuildingBuilder;
        }

        public override void Handle(CommandContext context)
        {
            
        }
    }
}