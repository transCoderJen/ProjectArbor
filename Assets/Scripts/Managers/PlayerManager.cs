using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
using UnityEngine;

namespace ShiftedSignal.Garden.Managers
{
    public class PlayerManager : Singleton<PlayerManager>, ISaveManager
    {
        public int Currency;
        public int UnlockedFarmingArea;
        public GameObject FarmGameObject;

        private void OnEnable()
        {
            Bus<CurrencyUpdatedEvent>.OnEvent += HandleUpdateCurrency;
            Bus<UnlockFarmingAreaEvent>.OnEvent += HandleFarmingAreaUnlocked;
        }

        private void OnDisable()
        {
            Bus<CurrencyUpdatedEvent>.OnEvent -= HandleUpdateCurrency;
            Bus<UnlockFarmingAreaEvent>.OnEvent -= HandleFarmingAreaUnlocked;
        }

        private void HandleFarmingAreaUnlocked(UnlockFarmingAreaEvent evt)
        {
            UnlockedFarmingArea++;
        }

        private void HandleUpdateCurrency(CurrencyUpdatedEvent evt)
        {
            Currency += evt.Coins;

            if (Currency < 0)
                Currency = 0;
        }

        public bool CanAfford(int amount)
        {
            return Currency >= amount;
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0)
                return;

            Currency += amount;
        }

        public bool TrySpendCurrency(int amount)
        {
            if (amount <= 0)
                return true;

            if (!CanAfford(amount))
                return false;

            Currency -= amount;
            return true;
        }

        public void UnlockFarmingArea()
        {
            UnlockedFarmingArea++;
        }

        public void LoadData(GameData data)
        {
            Debug.Log("Loading currency and farming area");

            Currency = data.currency;
            UnlockedFarmingArea = data.unlockedFarmingArea;
        }

        public void SaveData(ref GameData data)
        {
            data.currency = Currency;
            data.unlockedFarmingArea = UnlockedFarmingArea;
        }
    }
}