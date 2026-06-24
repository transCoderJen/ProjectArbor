using ShiftedSignal.Garden.Environment;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    [CreateAssetMenu(fileName = "Gather Command", menuName = "Units/Commands/Gather", order = 105)]
    public class GatherCommand : BaseCommand
    {
        public override bool CanHandle(CommandContext context)
        {
            return context.Commandable is Worker 
                && context.Hit.collider != null
                && context.Hit.collider.TryGetComponent(out GatherableSupply _);
        }

        public override void Handle(CommandContext context)
        {
            Worker worker = context.Commandable as Worker;

            worker.Gather(context.Hit.collider.GetComponent<GatherableSupply>());
        }
    }
}