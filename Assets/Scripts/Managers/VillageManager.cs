using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
using UnityEngine;

namespace ShiftedSignal.Garden.Managers
{
    public class VillageManager : Singleton<VillageManager>, ISaveManager
    {
        [Header("Village Reputation")]
        [SerializeField, Range(-100, 100)] private int villageReputation = 0;

        public int VillageReputation => villageReputation;

        public void AddReputation(int amount)
        {
            SetReputation(villageReputation + amount);
        }

        public void RemoveReputation(int amount)
        {
            SetReputation(villageReputation - Mathf.Abs(amount));
        }

        public void SetReputation(int value)
        {
            int oldValue = villageReputation;
            villageReputation = Mathf.Clamp(value, -100, 100);

            if (oldValue == villageReputation)
                return;

            Bus<VillageReputationChangedEvent>.Raise(
                new VillageReputationChangedEvent(oldValue, villageReputation)
            );
        }

        public void SaveData(ref GameData data)
        {
            data.villageReputation = villageReputation;
        }

        public void LoadData(GameData data)
        {
            SetReputation(data.villageReputation);
        }
    }
}