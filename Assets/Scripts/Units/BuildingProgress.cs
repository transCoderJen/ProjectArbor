using System;
using Unity.Behavior;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    [Serializable]
    public class BuildingProgress
    {
        [BlackboardEnum]
        public enum BuildingState
        {
            Building,
            Completed,
            Destroyed
        }

        [field: SerializeField]
        public float Progress { get; private set; }

        [field: SerializeField]
        public BuildingState State { get; private set; }

        public bool IsBuilding =>
            State == BuildingState.Building;

        public bool IsCompleted =>
            State == BuildingState.Completed;

        public bool IsDestroyed =>
            State == BuildingState.Destroyed;

        public BuildingProgress()
        {
            Progress = 0f;
            State = BuildingState.Completed;
        }

        public void Start()
        {
            Progress = 0f;
            State = BuildingState.Building;
        }

        public void AddProgress(float amount, float buildTime)
        {
            if (!IsBuilding)
                return;

            Progress = Mathf.Clamp(
                Progress + amount,
                0f,
                buildTime);

            if (Progress >= buildTime)
                Complete(buildTime);
        }

        public void Complete(float buildTime)
        {
            Progress = buildTime;
            State = BuildingState.Completed;
        }

        public void MarkDestroyed()
        {
            State = BuildingState.Destroyed;
        }

        public void Restore(
            BuildingState state,
            float progress)
        {
            State = state;
            Progress = progress;
        }
    }
}