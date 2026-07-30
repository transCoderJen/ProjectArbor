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
        public List<ShopEntry> Items { get; private set; } = new();
        
        [Header("Dialogue")]
        public string ShopDialogueKnot;
        [Tooltip("Ink knot used after closing shop")]
        public string ExitShopKnot;
        [Tooltip("Ink knot used when player cannot afford item")]
        
        public string InsufficientFundsKnot => ShopDialogueKnot + "InsufficientFunds";
    }

        [Serializable]
    public class ShopEntry
    {
        public ItemData Item;

        [Min(0)]
        public int Price;

        [Header("Dialogue")]
        [Tooltip("Ink knot used after successfully buying this item.")]
        public string PurchaseDialogueKnot;

        public bool IsValid =>
            Item != null &&
            Price >= 0;
    }
}