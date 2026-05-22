using ShiftedSignal.Garden.ItemsAndInventory;
using UnityEngine;

namespace ShiftedSignal.Garden.QuestSystem
{    
    [CreateAssetMenu(fileName = "QuestInfoSO", menuName = "Data/QuestInfoSO")]
    public class QuestInfoSO : ScriptableObject
    {
        [field: SerializeField] public string ID { get; private set; }

        [Header("General")]
        public string DisplayName;
        
        [Header("Requirements")]
        public int LevelRequirement;
        public QuestInfoSO[] QuestPrerequisites;

        [Header("Steps")]
        public GameObject[] QuestStepPrefabs;

        [Header("Rewards")]
        public int GoldReward;
        public int ExperienceReward;
        public ItemData[] ItemRewards;

        void OnValidate()
        {
    #if  UNITY_EDITOR
            ID = this.name;
            UnityEditor.EditorUtility.SetDirty(this);
    #endif

        }
    }
}
