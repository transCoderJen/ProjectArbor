using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignalGames.GOF.ItemsAndInventory;
using Unity.VisualScripting;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable.Effects
{
    [CreateAssetMenu(
        fileName = "SpawnItemEffect",
        menuName = "Data/Buildable Effects/Spawn Item Effect")]
    public class SpawnItemEffect : BuildableEffect
    {
        [SerializeField]
        private ItemData Item;

        [SerializeField]
        private int Amount = 1;

        public override void Apply(BaseBuildable buildable)
        {
            for (int i = 0; i < Amount; i++)
            {
                Inventory.Instance.AddItem(Item);
            }
        }
    }
}