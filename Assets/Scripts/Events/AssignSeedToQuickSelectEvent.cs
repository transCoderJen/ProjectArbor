using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.ItemsAndInventory;

namespace ShiftedSignal.Garden.Events
{
    public struct AssignSeedToQuickSelectEvent : IEvent
    {
        public ItemData_Seed Seed { get; private set; }

        public AssignSeedToQuickSelectEvent(ItemData seed)
        {
            Seed = (ItemData_Seed) seed;
        }
    }
}