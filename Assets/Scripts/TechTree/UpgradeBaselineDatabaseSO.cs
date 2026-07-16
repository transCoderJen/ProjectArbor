using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ShiftedSignal.Garden.TechTree
{
    [CreateAssetMenu(
        fileName = "Upgrade Baseline Database",
        menuName = "Tech Tree/Upgrade Baseline Database",
        order = 150)]
    public class UpgradeBaselineDatabaseSO : ScriptableObject
    {
        [SerializeField]
        private List<UpgradeSO> upgrades = new();

        [SerializeField]
        private List<PropertyBaseline> baselines = new();

        public IReadOnlyList<PropertyBaseline> Baselines => baselines;

        [ContextMenu("Capture Baselines")]
        public void CaptureBaselines()
        {
            baselines.Clear();

            HashSet<string> capturedKeys = new();

            foreach (UpgradeSO upgrade in upgrades)
            {
                if (upgrade == null)
                    continue;

                if (upgrade.TargetObject == null)
                {
                    Debug.LogWarning(
                        $"Upgrade {upgrade.name} has no target object."
                    );

                    continue;
                }

                foreach (string propertyPath in upgrade.ModifiedPropertyPaths)
                {
                    if (string.IsNullOrWhiteSpace(propertyPath))
                        continue;

                    string key =
                        $"{upgrade.TargetObject.GetInstanceID()}:{propertyPath}";

                    if (!capturedKeys.Add(key))
                        continue;

                    if (!TryResolveProperty(
                            upgrade.TargetObject,
                            propertyPath,
                            out object propertyOwner,
                            out PropertyInfo property))
                    {
                        Debug.LogError(
                            $"Could not capture baseline for " +
                            $"{upgrade.TargetObject.name}.{propertyPath}."
                        );

                        continue;
                    }

                    object value = property.GetValue(propertyOwner);

                    PropertyBaseline baseline = new()
                    {
                        TargetObject = upgrade.TargetObject,
                        PropertyPath = propertyPath
                    };

                    if (!baseline.TryStoreValue(value))
                    {
                        Debug.LogError(
                            $"Unsupported baseline type " +
                            $"{value?.GetType().Name ?? "null"} for " +
                            $"{upgrade.TargetObject.name}.{propertyPath}."
                        );

                        continue;
                    }

                    baselines.Add(baseline);

                    Debug.Log(
                        $"Captured baseline " +
                        $"{upgrade.TargetObject.name}.{propertyPath} = {value}"
                    );
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif

            Debug.Log(
                $"Captured {baselines.Count} upgrade baseline value(s)."
            );
        }

        public void RestoreAll()
        {
            foreach (PropertyBaseline baseline in baselines)
            {
                if (baseline.TargetObject == null)
                    continue;

                if (!TryResolveProperty(
                        baseline.TargetObject,
                        baseline.PropertyPath,
                        out object propertyOwner,
                        out PropertyInfo property))
                {
                    Debug.LogError(
                        $"Could not restore " +
                        $"{baseline.TargetObject.name}." +
                        $"{baseline.PropertyPath}."
                    );

                    continue;
                }

                object value = baseline.GetStoredValue();

                try
                {
                    property.SetValue(propertyOwner, value);

                    Debug.Log(
                        $"Restored {baseline.TargetObject.name}." +
                        $"{baseline.PropertyPath} to {value}."
                    );
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Failed to restore " +
                        $"{baseline.TargetObject.name}." +
                        $"{baseline.PropertyPath}.\n{exception}"
                    );
                }
            }
        }

        private static bool TryResolveProperty(
            object rootObject,
            string propertyPath,
            out object propertyOwner,
            out PropertyInfo finalProperty)
        {
            propertyOwner = null;
            finalProperty = null;

            if (rootObject == null ||
                string.IsNullOrWhiteSpace(propertyPath))
            {
                return false;
            }

            string[] parts = propertyPath.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries
            );

            if (parts.Length == 0)
                return false;

            object currentObject = rootObject;

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            for (int i = 0; i < parts.Length; i++)
            {
                if (currentObject == null)
                    return false;

                Type currentType = currentObject.GetType();

                PropertyInfo property = currentType.GetProperty(
                    parts[i],
                    flags
                );

                if (property == null)
                    return false;

                bool isFinal = i == parts.Length - 1;

                if (isFinal)
                {
                    if (!property.CanRead ||
                        property.GetSetMethod(true) == null)
                    {
                        return false;
                    }

                    propertyOwner = currentObject;
                    finalProperty = property;

                    return true;
                }

                currentObject = property.GetValue(currentObject);
            }

            return false;
        }
    }

    [Serializable]
    public class PropertyBaseline
    {
        public enum StoredValueType
        {
            Int,
            Float,
            Bool,
            String,
            Vector2,
            Vector3,
            Color
        }

        [field: SerializeField]
        public ScriptableObject TargetObject { get; set; }

        [field: SerializeField]
        public string PropertyPath { get; set; }

        [SerializeField]
        private StoredValueType valueType;

        [SerializeField]
        private int intValue;

        [SerializeField]
        private float floatValue;

        [SerializeField]
        private bool boolValue;

        [SerializeField]
        private string stringValue;

        [SerializeField]
        private Vector2 vector2Value;

        [SerializeField]
        private Vector3 vector3Value;

        [SerializeField]
        private Color colorValue;

        public bool TryStoreValue(object value)
        {
            switch (value)
            {
                case int typedValue:
                    valueType = StoredValueType.Int;
                    intValue = typedValue;
                    return true;

                case float typedValue:
                    valueType = StoredValueType.Float;
                    floatValue = typedValue;
                    return true;

                case bool typedValue:
                    valueType = StoredValueType.Bool;
                    boolValue = typedValue;
                    return true;

                case string typedValue:
                    valueType = StoredValueType.String;
                    stringValue = typedValue;
                    return true;

                case Vector2 typedValue:
                    valueType = StoredValueType.Vector2;
                    vector2Value = typedValue;
                    return true;

                case Vector3 typedValue:
                    valueType = StoredValueType.Vector3;
                    vector3Value = typedValue;
                    return true;

                case Color typedValue:
                    valueType = StoredValueType.Color;
                    colorValue = typedValue;
                    return true;

                default:
                    return false;
            }
        }

        public object GetStoredValue()
        {
            return valueType switch
            {
                StoredValueType.Int => intValue,
                StoredValueType.Float => floatValue,
                StoredValueType.Bool => boolValue,
                StoredValueType.String => stringValue,
                StoredValueType.Vector2 => vector2Value,
                StoredValueType.Vector3 => vector3Value,
                StoredValueType.Color => colorValue,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}