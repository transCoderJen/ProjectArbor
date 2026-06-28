using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.ItemsAndInventory;

namespace ShiftedSignal.Garden.Events
{
    public struct SeedEquipEvent : IEvent
    {
        public ItemData_Seed Seed { get; private set; }

        public SeedEquipEvent(ItemData seed)
        {
            Seed = (ItemData_Seed) seed;
        }
    }
}