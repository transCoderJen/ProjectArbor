using System.Collections.Generic;

namespace ShiftedSignal.Garden.Interfaces
{
    public enum RaiderTargetType
    {
        Farm,
        Building,
        Fence,
        Crop
    }
    
    public static class RaiderTargetRegistry
    {
        private static readonly List<IRaiderTarget> targets = new();

        public static IReadOnlyList<IRaiderTarget> Targets => targets;
        public static int Count => targets.Count;

        public static void Register(IRaiderTarget target)
        {
            if (target == null)
                return;

            if (!targets.Contains(target))
                targets.Add(target);
        }

        public static void Unregister(IRaiderTarget target)
        {
            if (target == null)
                return;

            targets.Remove(target);
        }
    }
}