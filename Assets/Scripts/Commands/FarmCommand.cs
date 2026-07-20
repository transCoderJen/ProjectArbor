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
            return context.Commandable is Worker;
        }

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is Worker worker)
                worker.Farm();
        }

        public override bool IsLocked(CommandContext context) => false;
    }
}