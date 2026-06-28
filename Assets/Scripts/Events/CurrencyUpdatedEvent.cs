using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct CurrencyUpdatedEvent : IEvent
    {
        public int Coins { get; private set; }

        public CurrencyUpdatedEvent(int coins)
        {
            Coins = coins;
        }
    }
}