using System.Text;
using UnityEngine;

#if UNITY_EDITOR
   using UnityEditor;
#endif

namespace ShiftedSignal.Garden.ItemsAndInventory
{
    public enum ItemType
    {
    Material,
    Seed,
    Equipment
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

