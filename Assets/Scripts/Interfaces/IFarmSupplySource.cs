using ShiftedSignal.Garden.Units;

namespace ShiftedSignal.Garden.Interfaces
{
    public interface IFarmSupplySource
    {
        FarmSupplyType SupplyType { get; }

        bool CanProvide(int amount);

        bool TryBeginCollect(Worker worker);

        int CompleteCollect(Worker worker, int requestedAmount);

        void AbortCollect(Worker worker);
    }
}