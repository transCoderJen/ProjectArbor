using System;
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

        [field: SerializeField]
        public List<ShopItemEntry> Items { get; private set; } = new();
    }

    [Serializable]
    public class ShopItemEntry
    {
        [field: SerializeField]
        public ItemData Item { get; private set; }

        [field: SerializeField, Min(0)]
        public int Price { get; private set; } = 1;
    }
}