using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class CompostFertilizerSource : MonoBehaviour, IFarmSupplySource
    {
        [Header("Collection")]
        [SerializeField] private int currentFertilizer = 25;

        private Worker reservedWorker;

        public FarmSupplyType SupplyType => FarmSupplyType.Fertilizer;

        public bool CanProvide(int amount)
        {
            if (amount <= 0)
                return false;

            return currentFertilizer >= amount;
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

            int collectedAmount = Mathf.Min(requestedAmount, currentFertilizer);

            currentFertilizer -= collectedAmount;
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