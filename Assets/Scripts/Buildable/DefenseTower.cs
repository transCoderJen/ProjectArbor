using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class DefenseTower : BaseBuildable
    {
        [SerializeField] private float ScanRate = 0.2f;
        private float nextScanTime;

        protected override void Update()
        {
            base.Update();

            if (!IsActive || !HasConstantEffects)
                return;

            if (Time.time < nextScanTime)
                return;

            nextScanTime = Time.time + ScanRate;

            foreach (BuildableEffect effect in ConstantEffects)
            {
                if (effect == null)
                    continue;

                effect.Apply(this);
            }
        }
        
    }
}