using ShiftedSignal.Garden.Buildable;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    public interface IBuildingBuilder
    {
        public bool IsBuilding { get; }
        public GameObject Build(BuildingSO building, Vector3 targetLocation);
        public void ResumeBuilding(BaseBuilding building);
        public void CancelBuilding();
        
    }
}