using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public abstract class BuildableEffect : ScriptableObject
    {
        public abstract void Apply(BaseBuildable buildable);
    }
}