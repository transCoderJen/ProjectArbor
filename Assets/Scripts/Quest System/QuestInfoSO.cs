using System;
using ShiftedSignal.Garden.ItemsAndInventory;
using UnityEngine;

namespace ShiftedSignal.Garden.QuestSystem
{    
    [Serializable]
    public struct ItemReward
    {
        public ItemData Data;
        public int Amount;
    }

    [CreateAssetMenu(fileName = "QuestInfoSO", menuName = "Data/QuestInfoSO")]
    public class QuestInfoSO : ScriptableObject
    {
        [field: SerializeField] public string ID { get; private set; }

        [Header("General")]
        public string DisplayName;

        [Header("Completion")]
        public bool RequiresTurnIn = true;
        
        [Header("Requirements")]
        public int LevelRequirement;
        public QuestInfoSO[] QuestPrerequisites;

        [Header("Steps")]
        public GameObject[] QuestStepPrefabs;

        [Header("Rewards")]
        public int GoldReward;
        public int ExperienceReward;
        public ItemReward[] ItemRewards;

        [Header("UI")]
        public Sprite QuestIcon;

        void OnValidate()
        {
    #if  UNITY_EDITOR
            ID = this.name;
            UnityEditor.EditorUtility.SetDirty(this);
    #endif

        }
    }
}
