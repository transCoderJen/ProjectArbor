using System;
using System.Collections.Generic;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.TechTree
{
    public abstract class UpgradeSO : UnlockableSO
    {
        [Header("Upgrade Identity")]
        [SerializeField, HideInInspector]
        private string upgradeID;

        [field: Header("Modifier")]
        [field: SerializeField]
        public string PropertyPath { get; private set; }

        public string UpgradeID => upgradeID;

        public abstract ScriptableObject TargetObject { get; }

        public virtual IEnumerable<string> ModifiedPropertyPaths
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PropertyPath))
                    yield return PropertyPath;
            }
        }

        public abstract void Apply();

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrWhiteSpace(upgradeID))
            {
                upgradeID = Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}