using ShiftedSignal.Garden.Combat;
using ShiftedSignal.Garden.Environment;

namespace ShiftedSignal.Garden.Units
{
    public class Worker : AbstractUnit
    {
        public override CombatTeam Team => CombatTeam.Friendly;

        public void Gather(GatherableSupply supply)
        {
            graphAgent.SetVariableValue("Supply", supply);
            graphAgent.SetVariableValue("TargetGameObject", supply.gameObject);
            graphAgent.SetVariableValue("Command", UnitCommands.Gather);
        }
    }
}

