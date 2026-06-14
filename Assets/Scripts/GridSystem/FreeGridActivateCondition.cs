using UnityEngine;

namespace ShiftedSignal.Garden.GridSystem
{
    [CreateAssetMenu(
        fileName = "Free Grid Activation Condition",
        menuName = "Data/Grid Activation Conditions/Free")]
    public class FreeGridActivationCondition : GridActivationCondition
    {
        public override bool CanActivate()
        {
            return true;
        }
    }
}