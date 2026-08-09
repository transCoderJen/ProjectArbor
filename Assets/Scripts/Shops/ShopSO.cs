using System.Collections.Generic;
using ShiftedSignal.Garden.ItemsAndInventory;
using UnityEngine;

namespace ShiftedSignal.Garden.Shops
{
    [CreateAssetMenu(
        fileName = "Shop",
        menuName = "Shops/Shop",
        order = 1)]
    public class ShopSO : ScriptableObject
    {
        [field: SerializeField]
        public string DisplayName { get; private set; }

        [Header("Items")]
        [field: SerializeField]
        public List<ItemData> Items { get; private set; } = new();

        [Header("AcceptedSellTypes")]
        [field: SerializeField]
        public List<ItemType> AcceptedSellTypes { get; private set; } = new();

        [Header("Dialogue")]
        [field: SerializeField]
        public string DialogueKnotPrefix { get; private set; }



        public string ExitShopKnot =>
            DialogueKnotPrefix + "Close";

        public string InsufficientFundsKnot =>
            DialogueKnotPrefix + "InsufficientFunds";
    }
}