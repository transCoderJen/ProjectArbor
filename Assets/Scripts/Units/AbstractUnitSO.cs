using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    public abstract class AbstractUnitSO : ScriptableObject
    {
        [field: Header("Identity")]
        [field: SerializeField] public string ItemID { get; private set; }

        [field: Header("Display")]
        [field: SerializeField] public string Name { get; private set; } = "Unit";
        [field: SerializeField] public Sprite Icon { get; private set; }

        [field: Header("Prefab")]
        [field: SerializeField] public GameObject Prefab { get; private set; }

        [field: Header("Stats")]
        [field: SerializeField] public int Health { get; private set; } = 100;
        [field: SerializeField] public float BuildTime { get; private set; } = 5f;
        [field: SerializeField] public AttackConfigSO AttackConfig { get; private set; }
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(ItemID))
                ItemID = name;

            if (string.IsNullOrWhiteSpace(Name))
                Name = name;

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}