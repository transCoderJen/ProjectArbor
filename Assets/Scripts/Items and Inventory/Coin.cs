using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using UnityEngine;

public class Coin : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Player>() != null)
        {
            Bus<CurrencyUpdatedEvent>.Raise(new CurrencyUpdatedEvent(1));
            Destroy(this.gameObject);
        }
    }
}
