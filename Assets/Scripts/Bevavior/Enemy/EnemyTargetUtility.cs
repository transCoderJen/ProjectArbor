using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.Interfaces;
using UnityEngine;

namespace ShiftedSignal.Garden.Behavior
{
    public static class EnemyTargetUtility
    {
        public static bool IsValidNearbyTarget(
            GameObject self,
            GameObject target)
        {
            if (self == null ||
                target == null ||
                target == self)
            {
                return false;
            }

            // Fences are only valid for the path-clearing branch.
            if (target.GetComponentInParent<FencePost2D>() != null)
                return false;

            IDamageable damageable =
                target.GetComponentInParent<IDamageable>();

            if (damageable == null)
                return false;

            return damageable.CurrentHealth > 0;
        }
    }
}