using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ShiftedSignal.Garden.UserInterface;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Events;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.UIElements;
using TMPro;




#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShiftedSignalGames.GOF.ItemsAndInventory
{
    public enum InventorySortMode
    {
        None,
        Alphabetical,
        Power,
        Defense,
        HP,
        MP,
        Vitality,
        Speed,
        CritChance,
        CritPower,
        Evasion,
        MagicResistance,
        AttackSpeed
    }

    public class Inventory : Singleton<Inventory>, ISaveManager
    {
        [Header("Starting Equipment")]
        public List<ItemData> StartingEquipment = new();

        [Header("Runtime Collections")]
        public List<InventoryItem> equipment = new();
        public Dictionary<ItemData_Equipment, InventoryItem> equipmentDictionary = new();

        public List<InventoryItem> inventory = new();
        public Dictionary<ItemData, InventoryItem> inventoryDictionary = new();

        public List<InventoryItem> stash = new();
        public Dictionary<ItemData, InventoryItem> stashDictionary = new();

        [Header("Inventory UI")]
        [SerializeField] private Transform inventorySlotParent;
        [SerializeField] private Transform stashSlotParent;
        [SerializeField] public Transform equipmentSlotParent;
        [SerializeField] public Transform statSlotParent;

        [SerializeField] public TMP_Dropdown sortModeDropdown;
        [SerializeField] public TMP_Dropdown sortTypeDropdown;

        private UI_ItemSlot[] inventoryItemSlot = Array.Empty<UI_ItemSlot>();
        private UI_ItemSlot[] stashItemSlot = Array.Empty<UI_ItemSlot>();
        private UI_EquipmentSlot[] equipmentSlot = Array.Empty<UI_EquipmentSlot>();
        private UI_StatSlot[] statSlot = Array.Empty<UI_StatSlot>();

        private float flaskTimer;
        private float armorTimer;

        [Header("Database")]
        public List<ItemData> itemDataBase = new();
        public List<InventoryItem> loadedItems = new();
        public List<ItemData_Equipment> loadedEquipment = new();

        private bool startingItemsApplied;

        [Header("Inventory Sorting Flags")]
        [SerializeField] private bool showWeapons;
        [SerializeField] private bool showFlasks;
        [SerializeField] private bool showArmor;
        [SerializeField] private bool showAmulets;

        [Header("Inventory Sorting")]
        [SerializeField] private InventorySortMode inventorySortMode = InventorySortMode.None;
        [SerializeField] private bool sortDescending = true;

        [Header("Debug")]
        [SerializeField] private bool loadAsNewGame;

        protected override void Awake()
        {
            base.Awake();

            InitializeCollections();

            Bus<WeaponQuickSelectEvent>.OnEvent += HandleWeaponEquipped;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            CacheUIReferences();

            // Delay slightly in case save systems load in Start on other objects.
            Invoke(nameof(AddStartingItems), 0.1f);
        }

        protected override void OnDestroy()
        {
            Bus<WeaponQuickSelectEvent>.OnEvent -= HandleWeaponEquipped;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            base.OnDestroy();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CacheUIReferences();
            UpdateSlotUI();
        }

        public void SortModeChanged(int _)
        {
            inventorySortMode = (InventorySortMode) sortModeDropdown.value;
            UpdateSlotUI();

        }

        public void SortTypeChanged(int _)
        {
            // DropDown Menu Key:
            // 0 = all, 1 = weapons, 2 = armor, 3 = amulets, 4 = flask

            switch (sortTypeDropdown.value)
            {
                case 0:
                    showWeapons = true;
                    showFlasks = true;
                    showArmor = true;
                    showAmulets = true;
                    break;
                case 1:
                    showWeapons = true;
                    showFlasks = false;
                    showArmor =false;
                    showAmulets =false;
                    break;
                case 2:
                    showWeapons =false;
                    showFlasks =false;
                    showArmor = true;
                    showAmulets =false;
                    break;
                case 3:
                    showWeapons =false;
                    showFlasks =false;
                    showArmor =false;
                    showAmulets = true;
                    break;
                case 4:
                    showWeapons = false;
                    showFlasks = true;
                    showArmor =false;
                    showAmulets = false;
                    break;
            }

            UpdateSlotUI();
        }

        private void Update()
        {
            if (flaskTimer > 0)
                flaskTimer -= Time.deltaTime;

            if (armorTimer > 0)
                armorTimer -= Time.deltaTime;
        }

        private void InitializeCollections()
        {
            inventory ??= new List<InventoryItem>();
            inventoryDictionary ??= new Dictionary<ItemData, InventoryItem>();

            stash ??= new List<InventoryItem>();
            stashDictionary ??= new Dictionary<ItemData, InventoryItem>();

            equipment ??= new List<InventoryItem>();
            equipmentDictionary ??= new Dictionary<ItemData_Equipment, InventoryItem>();

            itemDataBase ??= new List<ItemData>();
            loadedItems ??= new List<InventoryItem>();
            loadedEquipment ??= new List<ItemData_Equipment>();
        }

        private void CacheUIReferences()
        {
            if (inventorySlotParent != null)
                inventoryItemSlot = inventorySlotParent.GetComponentsInChildren<UI_ItemSlot>(true);
            else
                inventoryItemSlot = Array.Empty<UI_ItemSlot>();

            if (stashSlotParent != null)
                stashItemSlot = stashSlotParent.GetComponentsInChildren<UI_ItemSlot>(true);
            else
                stashItemSlot = Array.Empty<UI_ItemSlot>();

            if (equipmentSlotParent != null)
                equipmentSlot = equipmentSlotParent.GetComponentsInChildren<UI_EquipmentSlot>(true);
            else
                equipmentSlot = Array.Empty<UI_EquipmentSlot>();

            if (statSlotParent != null)
                statSlot = statSlotParent.GetComponentsInChildren<UI_StatSlot>(true);
            else
                statSlot = Array.Empty<UI_StatSlot>();
        }

        private void HandleWeaponEquipped(WeaponQuickSelectEvent evt)
        {
            if (evt.Weapon is null)
                return;

            EquipItem(evt.Weapon);
        }

        private void AddStartingItems()
        {
            if (startingItemsApplied)
                return;

            InitializeCollections();

            if (!loadAsNewGame)
            {
                if (loadedEquipment.Count > 0)
                {
                    Debug.Log("1st hook");
                    foreach (ItemData_Equipment item in loadedEquipment)
                    {
                        if (item == null)
                            continue;

                        EquipItem(item);
                    }
                }

                if (loadedItems.Count > 0)
                {
                    Debug.Log("2nd hook");
                    foreach (InventoryItem item in loadedItems)
                    {
                        if (item == null || item.data == null)
                            continue;

                        for (int i = 0; i < item.stackSize; i++)
                        {
                            AddItem(item.data, false);
                        }
                    }

                    startingItemsApplied = true;
                    UpdateSlotUI();
                    return;
                }
            }

            if (SaveManager.Instance != null && !loadAsNewGame)
            {
                Debug.Log("Saved file: " + SaveManager.Instance.HasSavedData());

                if (!SaveManager.Instance.HasSavedData())
                {
                    for (int i = 0; i < StartingEquipment.Count; i++)
                    {
                        if (StartingEquipment[i] == null)
                            continue;

                        EquipItem(StartingEquipment[i]);
                    }
                }
            }
            else
            {
                Debug.Log("SaveManager.Instance is null or debug option selected to load starting equipment.");
                for (int i = 0; i < StartingEquipment.Count; i++)
                {
                    if (StartingEquipment[i] == null)
                        continue;

                    EquipItem(StartingEquipment[i]);
                }
            }

            startingItemsApplied = true;
            UpdateSlotUI();
        }

        public void EquipItem(ItemData item)
        {
            InitializeCollections();

            if (item == null)
                return;

            ItemData_Equipment newEquipment = item as ItemData_Equipment;
            if (newEquipment == null)
            {
                Debug.LogWarning($"Tried to equip non-equipment item: {item.name}");
                return;
            }

            InventoryItem newItem = new InventoryItem(newEquipment);
            ItemData_Equipment oldEquipment = null;

            if (newEquipment.EquipmentType == EquipmentType.Flask)
            {
                // Unlock flask UI here if needed
            }

            foreach (KeyValuePair<ItemData_Equipment, InventoryItem> equippedItem in equipmentDictionary)
            {
                if (equippedItem.Key != null && equippedItem.Key.EquipmentType == newEquipment.EquipmentType)
                {
                    oldEquipment = equippedItem.Key;
                    break;
                }
            }

            if (oldEquipment != null)
            {
                UnequipItem(oldEquipment);
                AddItem(oldEquipment, false);
            }

            equipment.Add(newItem);
            equipmentDictionary[newEquipment] = newItem;
            newEquipment.AddModifiers();

            if (newEquipment.EquipmentType == EquipmentType.Weapon)
                Bus<WeaponEquipEvent>.Raise(new WeaponEquipEvent(newItem.data as ItemData_Equipment));

            RemoveItem(item, false);
            UpdateSlotUI();
        }

        public void UnequipItem(ItemData_Equipment oldEquipment)
        {
            InitializeCollections();

            if (oldEquipment == null)
                return;

            if (equipmentDictionary.TryGetValue(oldEquipment, out InventoryItem value))
            {
                equipment.Remove(value);
                equipmentDictionary.Remove(oldEquipment);
                oldEquipment.RemoveModifiers();
            }

            UpdateSlotUI();
        }

        [ContextMenu("Update Slot UI")]
        private void UpdateSlotUI()
        {
            if (equipmentSlot != null)
            {
                for (int i = 0; i < equipmentSlot.Length; i++)
                {
                    if (equipmentSlot[i] == null)
                        continue;

                    equipmentSlot[i].CleanUpSlot();

                    foreach (KeyValuePair<ItemData_Equipment, InventoryItem> item in equipmentDictionary)
                    {
                        if (item.Key != null && item.Key.EquipmentType == equipmentSlot[i].slotType)
                        {
                            equipmentSlot[i].UpdateSlot(item.Value);
                            break;
                        }
                    }
                }
            }

            if (inventoryItemSlot != null)
            {
                for (int i = 0; i < inventoryItemSlot.Length; i++)
                {
                    if (inventoryItemSlot[i] != null)
                        inventoryItemSlot[i].CleanUpSlot();
                }

                List<InventoryItem> filteredInventory = GetFilteredAndSortedInventory();

                int maxSlots = Mathf.Min(filteredInventory.Count, inventoryItemSlot.Length);
                for (int i = 0; i < maxSlots; i++)
                {
                    if (inventoryItemSlot[i] != null)
                        inventoryItemSlot[i].UpdateSlot(filteredInventory[i]);
                }
            }

            if (stashItemSlot != null)
            {
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

            UpdateStatsUI();
        }

        private List<InventoryItem> GetFilteredAndSortedInventory()
        {
            List<InventoryItem> filteredInventory = new List<InventoryItem>();

            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i] == null || inventory[i].data == null)
                    continue;

                ItemData_Equipment itemData = inventory[i].data as ItemData_Equipment;
                if (itemData == null)
                    continue;

                if (!ShouldShowItem(itemData))
                    continue;

                filteredInventory.Add(inventory[i]);
            }

            return SortInventory(filteredInventory);
        }

        private bool ShouldShowItem(ItemData_Equipment itemData)
        {
            switch (itemData.EquipmentType)
            {
                case EquipmentType.Weapon:
                    return showWeapons;

                case EquipmentType.Armor:
                    return showArmor;

                case EquipmentType.Amulet:
                    return showAmulets;

                case EquipmentType.Flask:
                    return showFlasks;

                default:
                    return false;
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

                case InventorySortMode.Power:
                    return SortByStat(items, data => data.Power);

                case InventorySortMode.Defense:
                    return SortByStat(items, data => data.Defense);

                case InventorySortMode.HP:
                    return SortByStat(items, data => data.HP);

                case InventorySortMode.MP:
                    return SortByStat(items, data => data.MP);

                case InventorySortMode.Vitality:
                    return SortByStat(items, data => data.Vitality);

                case InventorySortMode.Speed:
                    return SortByStat(items, data => data.Speed);

                case InventorySortMode.CritChance:
                    return SortByStat(items, data => data.CritChance);

                case InventorySortMode.CritPower:
                    return SortByStat(items, data => data.CritPower);

                case InventorySortMode.Evasion:
                    return SortByStat(items, data => data.Evasion);

                case InventorySortMode.MagicResistance:
                    return SortByStat(items, data => data.MagicResistance);

                case InventorySortMode.AttackSpeed:
                    return SortByStat(items, data => data.AttackSpeed);

                case InventorySortMode.None:
                default:
                    return items;
            }
        }

        private List<InventoryItem> SortByStat(List<InventoryItem> items, System.Func<ItemData_Equipment, int> statSelector)
        {
            if (sortDescending)
            {
                return items
                    .OrderByDescending(item => statSelector(item.data as ItemData_Equipment))
                    .ThenBy(item => item.data.name)
                    .ToList();
            }

            return items
                .OrderBy(item => statSelector(item.data as ItemData_Equipment))
                .ThenBy(item => item.data.name)
                .ToList();
        }

        private void UpdateStatsUI()
        {
            
            if (statSlot == null)
                return;

            for (int i = 0; i < statSlot.Length; i++)
            {
                if (statSlot[i] != null)
                    statSlot[i].UpdateStatValueUI();
            }
        }

        public void AddItem(ItemData item, bool updateUI = true)
        {
            InitializeCollections();

            if (item == null)
                return;

            if (item.ItemType == ItemType.Equipment)
            {
                if (CanAddEquipment())
                    AddToInventory(item);
                else
                    Debug.Log("Inventory full, could not add equipment: " + item.name);
            }
            else if (item.ItemType == ItemType.Material)
            {
                AddToStash(item);
            }

            if (updateUI)
                UpdateSlotUI();
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

            if (updateUI)
                UpdateSlotUI();
        }

        public bool CanAddEquipment()
        {
            return inventoryItemSlot == null || inventory.Count < inventoryItemSlot.Length;
        }

        public bool CanCraft(ItemData_Equipment itemToCraft, List<InventoryItem> requiredMaterials)
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
                {
                    RemoveItem(requiredMaterials[i].data, false);
                }
            }

            AddItem(itemToCraft, false);
            UpdateSlotUI();

            Debug.Log("Here is your item " + itemToCraft.name);
            return true;
        }

        public List<InventoryItem> GetEquipmentList() => equipment;
        public List<InventoryItem> GetStashList() => stash;
        public List<InventoryItem> GetInventoryList() => inventory;
        public UI_EquipmentSlot[] GetUI_EquipmentSlots() => equipmentSlot;
        public UI_ItemSlot[] GetUI_StashSlots() => stashItemSlot;
        public UI_ItemSlot[] GetUI_InventorySlots() => inventoryItemSlot;

        public ItemData_Equipment GetEquipment(EquipmentType type)
        {
            InitializeCollections();

            foreach (KeyValuePair<ItemData_Equipment, InventoryItem> item in equipmentDictionary)
            {
                if (item.Key != null && item.Key.EquipmentType == type)
                    return item.Key;
            }

            return null;
        }

        public void UseFlask()
        {
            ItemData_Equipment flask = GetEquipment(EquipmentType.Flask);
            Player player = PlayerManager.Instance != null ? PlayerManager.Instance.Player : null;

            if (flask == null || player == null)
                return;

            if (flaskTimer <= 0)
            {
                flask.Effect(player.transform);
                flaskTimer = flask.ItemCooldown;
            }
        }

        public float GetFlaskCooldownRatio()
        {
            ItemData_Equipment flask = GetEquipment(EquipmentType.Flask);

            if (flask == null || flask.ItemCooldown <= 0)
                return 0f;

            return flaskTimer / flask.ItemCooldown;
        }

        public float FlaskCooldown()
        {
            ItemData_Equipment flask = GetEquipment(EquipmentType.Flask);
            return flask == null ? 0f : flask.ItemCooldown;
        }

        public bool CanUseArmor()
        {
            ItemData_Equipment armor = GetEquipment(EquipmentType.Armor);

            if (armor != null && armorTimer <= 0)
            {
                armorTimer = armor.ItemCooldown;
                return true;
            }

            return false;
        }

        public void LoadData(GameData data)
        {
            InitializeCollections();

            loadedItems.Clear();
            loadedEquipment.Clear();

            if (data == null)
                return;

            foreach (KeyValuePair<string, int> pair in data.inventory)
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

            foreach (string loadedItemId in data.equipmentId)
            {
                foreach (ItemData item in itemDataBase)
                {
                    if (item != null && item.ItemID == loadedItemId)
                    {
                        if (item is ItemData_Equipment equipmentItem)
                            loadedEquipment.Add(equipmentItem);

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
            data.equipmentId.Clear();

            foreach (KeyValuePair<ItemData, InventoryItem> pair in inventoryDictionary)
            {
                if (pair.Key != null)
                    data.inventory[pair.Key.ItemID] = pair.Value.stackSize;
            }

            foreach (KeyValuePair<ItemData, InventoryItem> pair in stashDictionary)
            {
                if (pair.Key != null)
                    data.inventory[pair.Key.ItemID] = pair.Value.stackSize;
            }

            foreach (KeyValuePair<ItemData_Equipment, InventoryItem> pair in equipmentDictionary)
            {
                if (pair.Key != null)
                    data.equipmentId.Add(pair.Key.ItemID);
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