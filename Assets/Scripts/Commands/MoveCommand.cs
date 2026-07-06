using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    [CreateAssetMenu(fileName = "Move Command", menuName = "Units/Commands/Move", order = 100)]
    public class MoveCommand :BaseCommand, ICommand
    {
        [SerializeField] private float radiusMultiplier = 3.5f;
        private int unitsOnLayer = 0;
        private int maxUnitsOnLayer = 1;
        private float circleRadius = 0;
        private float radialOffset = 0;

        public override bool CanHandle(CommandContext context)
        {
            return context.Commandable is AbstractUnit;
        }

        public override void Handle(CommandContext context)
        {
            AbstractUnit unit = (AbstractUnit) context.Commandable;
            if (context.UnitIndex == 0)
            {
                unitsOnLayer = 0;
                maxUnitsOnLayer = 1;
                circleRadius = 0;
                radialOffset = 0;
            }
            
            Vector3 targetPosition = new(
                context.Hit.point.x + circleRadius * Mathf.Cos(radialOffset * unitsOnLayer),
                context.Hit.point.y,
                context.Hit.point.z + circleRadius * Mathf.Sin(radialOffset * unitsOnLayer)
            );

            unit.MoveTo(targetPosition);
            unitsOnLayer++;

            if (unitsOnLayer >= maxUnitsOnLayer)
            {
                unitsOnLayer = 0;
                circleRadius += unit.AgentRadius * radiusMultiplier;
                maxUnitsOnLayer = Mathf.FloorToInt(2 * Mathf.PI * circleRadius / (unit.AgentRadius * 2));
                radialOffset = 2 * Mathf.PI / maxUnitsOnLayer;
            }
        }

                public override bool IsLocked(CommandContext context) => false;
    }
}