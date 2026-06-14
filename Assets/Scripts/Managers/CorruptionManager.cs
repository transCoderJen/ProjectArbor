using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
using UnityEngine;

namespace ShiftedSignal.Garden.Managers
{
    public class CorruptionManager : Singleton<CorruptionManager>, ISaveManager
    {
        [Header("Corruption")]
        [SerializeField, Range(0, 100)] private int corruption = 0;

        public int Corruption => corruption;

        public void AddCorruption(int amount)
        {
            SetCorruption(corruption + amount);
        }

        public void RemoveCorruption(int amount)
        {
            SetCorruption(corruption - Mathf.Abs(amount));
        }

        public void SetCorruption(int value)
        {
            int oldValue = corruption;
            corruption = Mathf.Clamp(value, 0, 100);

            if (oldValue == corruption)
                return;

            Bus<CorruptionChangedEvent>.Raise(
                new CorruptionChangedEvent(oldValue, corruption)
            );
        }

        public void SaveData(ref GameData data)
        {
            data.corruption = corruption;
        }

        public void LoadData(GameData data)
        {
            SetCorruption(data.corruption);
        }
    }
}