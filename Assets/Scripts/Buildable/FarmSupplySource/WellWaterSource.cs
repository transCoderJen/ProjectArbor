using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class WellWaterSource : MonoBehaviour, IFarmSupplySource
    {
        [Header("Collection")]
        [SerializeField] private bool infiniteWater = true;
        [SerializeField] private int currentWater = 100;

        private Worker reservedWorker;

        public FarmSupplyType SupplyType => FarmSupplyType.Water;

        public bool CanProvide(int amount)
        {
            if (amount <= 0)
                return false;

            if (infiniteWater)
                return true;

            return currentWater >= amount;
        }

        public bool TryBeginCollect(Worker worker)
        {
            if (worker == null)
                return false;

            if (reservedWorker != null && reservedWorker != worker)
                return false;

            if (!CanProvide(1))
                return false;

            reservedWorker = worker;
            return true;
        }

        public int CompleteCollect(Worker worker, int requestedAmount)
        {
            if (worker == null)
                return 0;

            if (reservedWorker != worker)
                return 0;

            int collectedAmount;

            if (infiniteWater)
            {
                collectedAmount = requestedAmount;
            }
            else
            {
                collectedAmount = Mathf.Min(requestedAmount, currentWater);
                currentWater -= collectedAmount;
            }

            reservedWorker = null;
            return collectedAmount;
        }

        public void AbortCollect(Worker worker)
        {
            if (worker == null)
                return;

            if (reservedWorker != worker)
                return;

            reservedWorker = null;
        }
    }
}