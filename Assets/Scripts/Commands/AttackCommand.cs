using ShiftedSignal.Garden.Behavior;
using ShiftedSignal.Garden.Interfaces;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    [CreateAssetMenu(fileName = "Attack Command", menuName = "Units/Commands/Attack", order = 108)]
    public class AttackCommand : BaseCommand
    {
        public override bool CanHandle(CommandContext context)
        {
            return context.Commandable is IAttacker
                && context.Hit.collider != null;
        }

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is not IAttacker attacker)
                return;

            IDamageable damageable =
                context.Hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                attacker.Attack(damageable);
            }
            else
            {
                attacker.Attack(context.Hit.point);
            }
        }

        public override bool IsLocked(CommandContext context) => false;
    }
}