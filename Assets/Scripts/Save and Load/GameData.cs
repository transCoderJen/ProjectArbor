using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData 
{
    public int currency;
    public SerializableDictionary<string, bool> skillTree;
    public SerializableDictionary<string, int> inventory;
    public List<string> spells;
    public List<string> equippedSpells;
    public List<string> equipmentId;


    public SerializableDictionary<string, bool> checkpoints;
    public string lastCheckpointId;
    

    public int lostCurrencyAmount;
    public float lostCurrencyX;
    public float lostCurrencyY;

    public SerializableDictionary<string, float> volumeSettings;
    public bool showPopupText;


    public GameData()
    {
        this.lostCurrencyAmount = 0;
        this.lostCurrencyX = 0;
        this.lostCurrencyY = 0;
        this.currency = 0;
        skillTree = new SerializableDictionary<string, bool>();
        inventory = new SerializableDictionary<string, int>();
        spells = new List<string>();
        equippedSpells = new List<string>();
        equipmentId = new List<string>();

        lastCheckpointId = string.Empty;
        checkpoints = new SerializableDictionary<string, bool>();
        volumeSettings = new SerializableDictionary<string, float>();
        this.showPopupText = true;
    }
}
