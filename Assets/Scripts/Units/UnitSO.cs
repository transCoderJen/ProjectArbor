using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    [CreateAssetMenu(fileName = "Unit", menuName = "Units/Unit")]
    public class UnitSO : AbstractUnitSO
    {
        [Header("Save")]
        public string SaveID;
    }
}