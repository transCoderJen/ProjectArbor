using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Units;

namespace ShiftedSignal.Garden.Interfaces
{
    public interface ISeedSource
    {
        bool HasAnySeed { get; }

        bool CanProvideSeed(ItemData_Seed seed, int amount);

        bool TryBeginCollectSeed(Worker worker);

        ItemData_Seed GetNextSeed();

        bool TryCompleteCollectSeed(
            Worker worker,
            ItemData_Seed seed,
            int amount);

        void AbortCollectSeed(Worker worker);
    }
}