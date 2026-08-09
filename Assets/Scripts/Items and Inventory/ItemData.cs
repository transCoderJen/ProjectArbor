using System.Text;
using UnityEngine;

#if UNITY_EDITOR
   using UnityEditor;
#endif

namespace ShiftedSignal.Garden.ItemsAndInventory
{
    public enum ItemType
    {
        Material = 0,
        Seed = 1,
        Equipment = 2,
        Produce = 3
    }

    [CreateAssetMenu(fileName = "New Item Data", menuName = "Data/Item")]
    public class ItemData : ScriptableObject
    {
        public ItemType ItemType;
        public string ItemName;

        [TextArea(3, 10)]
        public string Description;
        public Sprite Icon;
        public string ItemID;


        [Header("Shop")]
        public int AmountPerPurchase = 1;
        [field: SerializeField, Min(0)]
        public int BaseValue { get; private set; }

        [field: SerializeField]
        public string BuyDialogueKnot { get; private set; }

        [field: SerializeField]
        public string SellDialogueKnot { get; private set; }
        public int BuyPrice => BaseValue;
        public int SellPrice =>
            Mathf.Max(
                1,
                Mathf.FloorToInt(BaseValue * 0.5f));

        [Range(0, 100)]
        public float DropChance;

        protected StringBuilder sb = new StringBuilder();

        private void OnValidate()
        {
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(this);
            ItemID = AssetDatabase.AssetPathToGUID(path);
#endif            
        }

        public virtual string GetDescription()
        {
            return "";
        }
    }
}

