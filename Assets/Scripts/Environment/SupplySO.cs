using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Environment
{
    [CreateAssetMenu(fileName = "Supply", menuName = "Supply", order = 5)]
    public class SupplySO : ScriptableObject
    {
        [field: SerializeField]
        public int MaxAmout { get; private set; } = 1500;

        [field: SerializeField]
        public int AmountPerGather { get; private set; } = 8;

        [field: SerializeField, Min(0f)]
        public float GatherTimeInGameMinutes { get; private set; } = 10f;

        [field: SerializeField]
        public ItemData Item { get; private set; }

        public float BaseGatherTime
        {
            get
            {
                if (TimeManger.Instance == null)
                    return GatherTimeInGameMinutes;

                return TimeManger.Instance.GetSecondsFromInGameMinutes(GatherTimeInGameMinutes);
            }
        }
    }
}