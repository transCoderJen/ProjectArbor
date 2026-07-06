using System.Collections.Generic;
using ShiftedSignal.Garden.Interfaces;
using UnityEngine;

namespace ShiftedSignal.Garden.Misc
{
    public struct DamageableTargetGameObjectComparer : IComparer<GameObject>
    {
        private readonly Vector3 targetPosition;

        public DamageableTargetGameObjectComparer(Vector3 position)
        {
            targetPosition = position;
        }

        public int Compare(GameObject x, GameObject y)
        {
            if (x == null && y == null)
                return 0;

            if (x == null)
                return 1;

            if (y == null)
                return -1;

            if (!x.TryGetComponent(out IDamageable xDamageable))
                return 1;

            if (!y.TryGetComponent(out IDamageable yDamageable))
                return -1;

            // Highest priority first.
            int priorityComparison =
                yDamageable.TargetPriority.CompareTo(xDamageable.TargetPriority);

            if (priorityComparison != 0)
                return priorityComparison;

            // Same priority? Sort by distance.
            float xDistance = (x.transform.position - targetPosition).sqrMagnitude;
            float yDistance = (y.transform.position - targetPosition).sqrMagnitude;

            return xDistance.CompareTo(yDistance);
        }
    }
}