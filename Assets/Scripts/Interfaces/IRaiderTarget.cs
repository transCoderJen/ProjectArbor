using UnityEngine;

namespace ShiftedSignal.Garden.Interfaces
{
    public interface IRaiderTarget
    {
        Transform TargetTransform { get; }
        RaiderTargetType TargetType { get; }
        int Priority { get; }
        bool IsValidTarget { get; }
    }
}