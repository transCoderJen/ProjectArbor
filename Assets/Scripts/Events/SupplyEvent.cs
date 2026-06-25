using ShiftedSignal.Garden.Environment;
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct SupplyEvent : IEvent
    {
        public int Amount { get; private set; }
        public SupplySO Supply { get; private set; }

        public SupplyEvent(int amount, SupplySO supply)
        {
            Amount = amount;
            Supply = supply;
        }
    }
}