using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.GridSystem;

namespace ShiftedSignal.Garden.Units
{
    public interface IBuildingBuilder
    {
        bool HasBuildAssignment { get; }

        void Build(BaseBuilding building);
        void Build(BaseBuilding building, GrowBlock targetBlock);

        void ResumeBuilding(BaseBuilding building);

        void CancelBuilding();
    }
}