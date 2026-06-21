using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class Windmill : BaseBuilding
    {
        [SerializeField] private float activationRadius;

        public override void Build()
        {
            base.Build();

            if (GridManager.Instance == null)
                return;

            GridManager.Instance.ActivateBlocksInRadius(
                transform.position,
                activationRadius);
        }
    }
}