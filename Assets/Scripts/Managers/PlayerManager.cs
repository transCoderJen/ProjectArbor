using System;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Misc;

namespace ShiftedSignal.Garden.Managers
{
    public class PlayerManager : Singleton<PlayerManager>
    {
        public Player Player;
        public int Currency;
        public int UnlockedFarmingArea = 0;

        void OnEnable()
        {
            Bus<CurrencyUpdatedEvent>.OnEvent += HandleUpdateCurrency;
            Bus<UnlockFarmingAreaEvent>.OnEvent += HandleFarmingAreaUnlocked;
        }

        void OnDisable()
        {
            Bus<CurrencyUpdatedEvent>.OnEvent -= HandleUpdateCurrency;
            Bus<UnlockFarmingAreaEvent>.OnEvent -= HandleFarmingAreaUnlocked;
        }

        private void HandleFarmingAreaUnlocked(UnlockFarmingAreaEvent evt)
        {
            UnlockedFarmingArea ++;
        }

        private void HandleUpdateCurrency(CurrencyUpdatedEvent evt)
        {
            Currency += evt.Coins;
        }

        public void ResetPlayer()
        {
            Player.gameObject.SetActive(false);
            Player.gameObject.SetActive(true);
        }
    }
}