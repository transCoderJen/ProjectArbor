using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public enum BuildPlacementMode
    {
        GridOnly,
        Anywhere
    }

    [CreateAssetMenu(fileName = "New Buildable Data", menuName = "Data/Buildable")]
    public class BuildingSO : UnitSO
    {
        [Header("Ghost")]
        public Material GhostMaterial;

        [Header("Placement Rules")]
        public BuildPlacementMode PlacementMode = BuildPlacementMode.GridOnly;
        public bool RequiresActiveGrowBlock = true;
        public float XRotation = 30f;
    }
}