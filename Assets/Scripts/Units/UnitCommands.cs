using Unity.Behavior;

namespace ShiftedSignal.Garden.Units
{
    [BlackboardEnum]
    public enum UnitCommands
    {
        Stop,
        Move,
        Gather,
        ReturnSupplies,
        Build,
        Farm,
        Attack
    }
}
