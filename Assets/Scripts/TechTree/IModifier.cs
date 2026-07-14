using ShiftedSignal.Garden.Units;

namespace ShiftedSignal.Garden.TechTree
{
    public interface IModifier
    {
        public string PropertyPath { get; }
        public void Apply(AbstractUnitSO unit);
    }
}