using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.QuestSystem;
using UnityEngine;

namespace ShiftedSignal.Garden.SaveAndLoad
{
    [System.Serializable]
    public class GameData
    {
        public SerializableDictionary<string, bool> skillTree;
        public SerializableDictionary<string, int> inventory;
        public SerializableDictionary<string, int> stash;
        public SerializableDictionary<string, int> seedBank;
        public SerializableDictionary<string, QuestData> quests;
        public SerializableDictionary<string, float> volumeSettings;
        public List<string> equipmentId;
        public List<InfoRow> gridRows;
        public List<string> TriggeredDialogueIds;
        public int currency;
        public int lostCurrencyAmount;
        public int unlockedFarmingArea;
        public float lostCurrencyX;
        public float lostCurrencyY;
        public bool showPopupText;

        public Vector3 playerPosition;
        public List<string> weaponWheelIds;
        public List<string> seedWheelIds;
        public float currentTime;
        public int currentDay;
        public int weaponDamageLevel;
        public int villageReputation;
        public int corruption;

        public GameData()
        {
            currency = 100;
            skillTree = new SerializableDictionary<string, bool>();
            inventory = new SerializableDictionary<string, int>();
            stash = new SerializableDictionary<string, int>();
            seedBank = new SerializableDictionary<string, int>();
            quests = new SerializableDictionary<string, QuestData>();
            volumeSettings = new SerializableDictionary<string, float>();
            equipmentId = new List<string>();
            gridRows = new List<InfoRow>();
            TriggeredDialogueIds = new List<string>();
            lostCurrencyAmount = 0;
            lostCurrencyX = 0;
            lostCurrencyY = 0;
            unlockedFarmingArea = 0;
            villageReputation = 0;
            corruption = 0;
            showPopupText = true;

            playerPosition = Vector3.zero;
            weaponWheelIds = new List<string>();
            seedWheelIds = new List<string>();

            // Default new games to Day 1 at 8:00 AM
            currentTime = 8f; 
            currentDay = 1;  
        }
    }
}