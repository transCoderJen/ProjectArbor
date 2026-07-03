using ShiftedSignal.Garden.Buildable;

namespace ShiftedSignal.Garden.Units
{
    public interface IBuildingBuilder
    {
        bool HasBuildAssignment { get; }

        void Build(BaseBuilding building);

        void ResumeBuilding(BaseBuilding building);

        void CancelBuilding();
    }
}