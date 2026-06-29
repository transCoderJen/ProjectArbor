using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
using ShiftedSignal.Garden.UserInterface.Components;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.UserInterface.Managers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShiftedSignal.Garden.ItemsAndInventory
{
    public enum InventorySortMode
    {
        None,
        Alphabetical
    }

    public class Inventory : Singleton<Inventory>, ISaveManager
    {
        [Header("Dynamic Slot Prefabs")]
        [SerializeField] private GameObject stashSlotPrefab;

        [Header("Starting Items")]
        public List<ItemData> StartingInventoryItems = new();
        public List<ItemData> StartingStashItems = new();
        public List<ItemData> StartingSeedBankItems = new();

        [Header("Runtime Collections")]
        public List<InventoryItem> inventory = new();
        public Dictionary<ItemData, InventoryItem> inventoryDictionary = new();

        public List<InventoryItem> stash = new();
        public Dictionary<ItemData, InventoryItem> stashDictionary = new();

        public List<InventoryItem> seedBank = new();
        public Dictionary<ItemData, InventoryItem> seedBankDictionary = new();

        [Header("Inventory UI")]
        [SerializeField] private Transform inventorySlotParent;
        [SerializeField] private Transform stashSlotParent;
        [SerializeField] public Transform seedBankSlotParent;

        [SerializeField] public TMP_Dropdown sortModeDropdown;

        private UI_ItemSlot[] inventoryItemSlot = Array.Empty<UI_ItemSlot>();
        private UI_ItemSlot[] stashItemSlot = Array.Empty<UI_ItemSlot>();
        private UI_ItemSlot[] seedBankItemSlot = Array.Empty<UI_ItemSlot>();

        [Header("Database")]
        public List<ItemData> itemDataBase = new();
        public List<InventoryItem> loadedItems = new();

        private bool startingItemsApplied;

        [Header("Inventory Sorting")]
        [SerializeField] private InventorySortMode inventorySortMode = InventorySortMode.None;
        [SerializeField] private bool sortDescending = false;

        [Header("Debug")]
        [SerializeField] private bool loadAsNewGame;

        protected override void Awake()
        {
            base.Awake();

            InitializeCollections();

            SceneManager.sceneLoaded += OnSceneLoaded;
            Bus<SupplyEvent>.OnEvent += AddSupplies;
        }

        private void Start()
        {
            CacheUIReferences();
            Invoke(nameof(AddStartingItems), 0.1f);
        }
        
    #if UNITY_EDITOR
        private void OnValidate()
        {
            FillUpItemDataBase();
        }
    #endif

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Bus<SupplyEvent>.OnEvent -= AddSupplies;
            base.OnDestroy();
        }

        private void AddSupplies(SupplyEvent evt)
        {
            ItemData item = evt.Supply.Item;
            AddItem(evt.Supply.Item, evt.Amount);
            // if (PickupPopupManager.Instance != null)
            // {
            //     PickupPopupManager.Instance.Show(
            //         item.Icon,
            //         evt.Amount,
            //         item.name);
            // }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CacheUIReferences();
            UpdateSlotUI();
        }

        private void InitializeCollections()
        {
            inventory ??= new List<InventoryItem>();
            inventoryDictionary ??= new Dictionary<ItemData, InventoryItem>();

            stash ??= new List<InventoryItem>();
            stashDictionary ??= new Dictionary<ItemData, InventoryItem>();

            seedBank ??= new List<InventoryItem>();
            seedBankDictionary ??= new Dictionary<ItemData, InventoryItem>();

            itemDataBase ??= new List<ItemData>();
            loadedItems ??= new List<InventoryItem>();
        }

        private void CacheUIReferences()
        {
            inventoryItemSlot = inventorySlotParent != null
                ? inventorySlotParent.GetComponentsInChildren<UI_ItemSlot>(true)
                : Array.Empty<UI_ItemSlot>();

            stashItemSlot = stashSlotParent != null
                ? stashSlotParent.GetComponentsInChildren<UI_ItemSlot>(true)
                : Array.Empty<UI_ItemSlot>();

            seedBankItemSlot = seedBankSlotParent != null
                ? seedBankSlotParent.GetComponentsInChildren<UI_ItemSlot>(true)
                : Array.Empty<UI_ItemSlot>();
        }

        [ContextMenu("Add StashSlot")]
        private void AddStashSlot()
        {
            if (stashSlotPrefab == null || stashSlotParent == null)
                return;

            Instantiate(stashSlotPrefab, stashSlotParent);
        }

        public void SortModeChanged(int _)
        {
            if (sortModeDropdown != null)
                inventorySortMode = (InventorySortMode)sortModeDropdown.value;

            UpdateSlotUI();
        }

        private void AddStartingItems()
        {
            if (startingItemsApplied)
                return;

            InitializeCollections();

            if (!loadAsNewGame && loadedItems.Count > 0)
            {
                foreach (InventoryItem item in loadedItems)
                {
                    if (item == null || item.data == null)
                        continue;

                    AddItem(item.data, item.stackSize, false);
                }

                startingItemsApplied = true;
                UpdateSlotUI();
                return;
            }

            if (SaveManager.Instance != null && !loadAsNewGame)
            {
                if (!SaveManager.Instance.HasSavedData())
                {
                    AddStartingInventoryItems();
                    AddStartingStashItems();
                    AddStartingSeedBankItems();
                }
            }
            else
            {
                AddStartingInventoryItems();
                AddStartingStashItems();
                AddStartingSeedBankItems();
            }

            startingItemsApplied = true;
            UpdateSlotUI();
        }

        private void AddStartingInventoryItems()
        {
            for (int i = 0; i < StartingInventoryItems.Count; i++)
            {
                if (StartingInventoryItems[i] == null)
                    continue;

                AddItem(StartingInventoryItems[i], false);
            }
        }

        private void AddStartingStashItems()
        {
            for (int i = 0; i < StartingStashItems.Count; i++)
            {
                if (StartingStashItems[i] == null)
                    continue;

                AddItem(StartingStashItems[i], false);
            }
        }

        private void AddStartingSeedBankItems()
        {
            for (int i = 0; i < StartingSeedBankItems.Count; i++)
            {
                if (StartingSeedBankItems[i] == null)
                    continue;

                AddItem(StartingSeedBankItems[i], false);
            }
        }

        [ContextMenu("Update Slot UI")]
        private void UpdateSlotUI()
        {
            UpdateInventorySlots();
            UpdateStashSlots();
            UpdateSeedBankSlots();
        }

        private void UpdateInventorySlots()
        {
            if (inventoryItemSlot == null)
                return;

            for (int i = 0; i < inventoryItemSlot.Length; i++)
            {
                if (inventoryItemSlot[i] != null)
                    inventoryItemSlot[i].CleanUpSlot();
            }

            List<InventoryItem> sortedInventory = SortInventory(inventory);

            int maxSlots = Mathf.Min(sortedInventory.Count, inventoryItemSlot.Length);

            for (int i = 0; i < maxSlots; i++)
            {
                if (inventoryItemSlot[i] != null)
                    inventoryItemSlot[i].UpdateSlot(sortedInventory[i]);
            }
        }

        private void UpdateStashSlots()
        {
            if (stashItemSlot == null)
                return;

            for (int i = 0; i < stashItemSlot.Length; i++)
            {
                if (stashItemSlot[i] != null)
                    stashItemSlot[i].CleanUpSlot();
            }

            for (int i = 0; i < stash.Count && i < stashItemSlot.Length; i++)
            {
                if (stashItemSlot[i] != null)
                    stashItemSlot[i].UpdateSlot(stash[i]);
            }
        }

        private void UpdateSeedBankSlots()
        {
            if (seedBankItemSlot == null)
                return;

            for (int i = 0; i < seedBankItemSlot.Length; i++)
            {
                if (seedBankItemSlot[i] != null)
                    seedBankItemSlot[i].CleanUpSlot();
            }

            for (int i = 0; i < seedBank.Count && i < seedBankItemSlot.Length; i++)
            {
                if (seedBankItemSlot[i] != null)
                    seedBankItemSlot[i].UpdateSlot(seedBank[i]);
            }
        }

        private List<InventoryItem> SortInventory(List<InventoryItem> items)
        {
            switch (inventorySortMode)
            {
                case InventorySortMode.Alphabetical:
                    return sortDescending
                        ? items.OrderByDescending(item => item.data.name).ToList()
                        : items.OrderBy(item => item.data.name).ToList();

                case InventorySortMode.None:
                default:
                    return items;
            }
        }

        public void AddItem(ItemData item, bool updateUI = true)
        {
            InitializeCollections();

            if (item == null)
                return;

            switch (item.ItemType)
            {
                case ItemType.Equipment:
                    if (CanAddInventoryItem())
                        AddToInventory(item);
                    else
                        Debug.Log("Inventory full, could not add item: " + item.name);
                    break;

                case ItemType.Material:
                    AddToStash(item);
                    break;

                case ItemType.Seed:
                    AddToSeedBank(item);
                    break;
            }

            if (updateUI)
                UpdateSlotUI();
        }

        public void AddItem(ItemData item, int amount, bool updateUI = true)
        {
            if (amount <= 0)
                return;

            for (int i = 0; i < amount; i++)
                AddItem(item, false);

            if (updateUI)
                UpdateSlotUI();
        }

        private void AddToInventory(ItemData item)
        {
            if (inventoryDictionary.TryGetValue(item, out InventoryItem value))
            {
                value.AddStack();
            }
            else
            {
                InventoryItem newItem = new InventoryItem(item);
                inventory.Add(newItem);
                inventoryDictionary.Add(item, newItem);
            }
        }

        private void AddToStash(ItemData item)
        {
            if (stashDictionary.TryGetValue(item, out InventoryItem value))
            {
                value.AddStack();
            }
            else
            {
                InventoryItem newItem = new InventoryItem(item);
                stash.Add(newItem);
                stashDictionary.Add(item, newItem);
            }
        }

        private void AddToSeedBank(ItemData item)
        {
            if (seedBankDictionary.TryGetValue(item, out InventoryItem value))
            {
                value.AddStack();
            }
            else
            {
                InventoryItem newItem = new InventoryItem(item);
                seedBank.Add(newItem);
                seedBankDictionary.Add(item, newItem);
            }
        }

        public bool HasItem(ItemData item)
        {
            if (item == null)
                return false;

            return inventoryDictionary.ContainsKey(item) ||
                   stashDictionary.ContainsKey(item) ||
                   seedBankDictionary.ContainsKey(item);
        }

        public bool HasItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
                return false;

            int count = 0;

            if (inventoryDictionary.TryGetValue(item, out InventoryItem inv))
                count += inv.stackSize;

            if (stashDictionary.TryGetValue(item, out InventoryItem stashItem))
                count += stashItem.stackSize;

            if (seedBankDictionary.TryGetValue(item, out InventoryItem seeds))
                count += seeds.stackSize;

            return count >= amount;
        }

        public void RemoveItem(ItemData item, bool updateUI = true)
        {
            InitializeCollections();

            if (item == null)
                return;

            if (inventoryDictionary.TryGetValue(item, out InventoryItem value))
            {
                if (value.stackSize <= 1)
                {
                    inventory.Remove(value);
                    inventoryDictionary.Remove(item);
                }
                else
                {
                    value.RemoveStack();
                }
            }

            if (stashDictionary.TryGetValue(item, out InventoryItem stashValue))
            {
                if (stashValue.stackSize <= 1)
                {
                    stash.Remove(stashValue);
                    stashDictionary.Remove(item);
                }
                else
                {
                    stashValue.RemoveStack();
                }
            }

            if (seedBankDictionary.TryGetValue(item, out InventoryItem seedBankValue))
            {
                if (seedBankValue.stackSize <= 1)
                {
                    seedBank.Remove(seedBankValue);
                    seedBankDictionary.Remove(item);
                }
                else
                {
                    seedBankValue.RemoveStack();
                }
            }

            if (updateUI)
                UpdateSlotUI();
        }

        public void RemoveItem(ItemData item, int amount)
        {
            for (int i =0; i < amount; i++)
            {
                RemoveItem(item);
            }
        }

        public bool CanAddInventoryItem()
        {
            return inventoryItemSlot == null || inventory.Count < inventoryItemSlot.Length;
        }

        public bool CanCraft(ItemData itemToCraft, List<InventoryItem> requiredMaterials)
        {
            InitializeCollections();

            if (itemToCraft == null || requiredMaterials == null || requiredMaterials.Count == 0)
                return false;

            for (int i = 0; i < requiredMaterials.Count; i++)
            {
                InventoryItem required = requiredMaterials[i];

                if (required == null || required.data == null)
                    return false;

                if (!stashDictionary.TryGetValue(required.data, out InventoryItem stashValue))
                {
                    Debug.Log("Not enough materials");
                    return false;
                }

                if (stashValue.stackSize < required.stackSize)
                {
                    Debug.Log("Not enough materials");
                    return false;
                }
            }

            for (int i = 0; i < requiredMaterials.Count; i++)
            {
                for (int j = 0; j < requiredMaterials[i].stackSize; j++)
                    RemoveItem(requiredMaterials[i].data, false);
            }

            AddItem(itemToCraft, false);
            UpdateSlotUI();

            Debug.Log("Crafted item: " + itemToCraft.name);
            return true;
        }

        public List<InventoryItem> GetStashList() => stash;
        public List<InventoryItem> GetInventoryList() => inventory;
        public List<InventoryItem> GetSeedBankList() => seedBank;

        public UI_ItemSlot[] GetUI_StashSlots() => stashItemSlot;
        public UI_ItemSlot[] GetUI_InventorySlots() => inventoryItemSlot;
        public UI_ItemSlot[] GetUI_SeedBankSlots() => seedBankItemSlot;

        public void LoadData(GameData data)
        {
            InitializeCollections();

            loadedItems.Clear();

            if (data == null)
                return;

            Dictionary<string, int> allSavedItems = new Dictionary<string, int>();

            if (data.inventory != null)
            {
                foreach (KeyValuePair<string, int> pair in data.inventory)
                    allSavedItems[pair.Key] = pair.Value;
            }

            if (data.stash != null)
            {
                foreach (KeyValuePair<string, int> pair in data.stash)
                    allSavedItems[pair.Key] = pair.Value;
            }

            if (data.seedBank != null)
            {
                foreach (KeyValuePair<string, int> pair in data.seedBank)
                    allSavedItems[pair.Key] = pair.Value;
            }

            foreach (KeyValuePair<string, int> pair in allSavedItems)
            {
                foreach (ItemData item in itemDataBase)
                {
                    if (item != null && item.ItemID == pair.Key)
                    {
                        InventoryItem itemToLoad = new InventoryItem(item)
                        {
                            stackSize = pair.Value
                        };

                        loadedItems.Add(itemToLoad);
                        break;
                    }
                }
            }
        }

        public void SaveData(ref GameData data)
        {
            InitializeCollections();

            if (data == null)
                return;

            data.inventory.Clear();
            data.stash.Clear();
            data.seedBank.Clear();

            foreach (KeyValuePair<ItemData, InventoryItem> pair in inventoryDictionary)
            {
                if (pair.Key != null)
                    data.inventory[pair.Key.ItemID] = pair.Value.stackSize;
            }

            foreach (KeyValuePair<ItemData, InventoryItem> pair in stashDictionary)
            {
                if (pair.Key != null)
                    data.stash[pair.Key.ItemID] = pair.Value.stackSize;
            }

            foreach (KeyValuePair<ItemData, InventoryItem> pair in seedBankDictionary)
            {
                if (pair.Key != null)
                    data.seedBank[pair.Key.ItemID] = pair.Value.stackSize;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Fill up item data base")]
        private void FillUpItemDataBase()
        {
            itemDataBase = new List<ItemData>(GetItemDataBase());
        }

        private List<ItemData> GetItemDataBase()
        {
            List<ItemData> database = new List<ItemData>();

            string[] assetNames = AssetDatabase.FindAssets("", new[] { "Assets/Data/Items" });

            foreach (string soName in assetNames)
            {
                string soPath = AssetDatabase.GUIDToAssetPath(soName);
                ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(soPath);

                if (itemData != null)
                    database.Add(itemData);
            }

            return database;
        }
#endif
    }
}