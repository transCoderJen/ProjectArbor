using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    [CreateAssetMenu(fileName = "Gather Command", menuName = "Units/Commands/Gather", order = 105)]
    public class GatherCommand : BaseCommand
    {
        public override bool CanHandle(CommandContext context)
        {
            return context.Commandable is Worker;
        }

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is Worker worker)
            {
                worker.Gather();
            }
        }

        public override bool IsLocked(CommandContext context) => false;
    }
}