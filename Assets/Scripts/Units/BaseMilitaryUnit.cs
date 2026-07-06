using ShiftedSignal.Garden.Combat;

namespace ShiftedSignal.Garden.Units
{
    public class BaseMilitaryUnit : AbstractUnit
    {
        public override CombatTeam Team => CombatTeam.Friendly;
    }
}