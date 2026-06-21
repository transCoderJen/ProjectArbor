using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    [CreateAssetMenu(fileName = "Currency Effect", menuName = "Data/Buildable Effects/Currency")]
    public class CurrencyBuildableEffect : BuildableEffect
    {
        [SerializeField] private int CoinsToAdd = 5;

        public override void Apply(BaseBuilding buildable)
        {
            Bus<CurrencyUpdatedEvent>.Raise(new CurrencyUpdatedEvent(CoinsToAdd));
        }
    }
}