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
                {
                    continue;
                }

                if (upgrade.TargetObjects == null ||
                    upgrade.TargetObjects.Count == 0)
                {
                    Debug.LogWarning(
                        $"Upgrade '{upgrade.name}' has no target objects."
                    );

                    continue;
                }

                foreach (ScriptableObject targetObject in upgrade.TargetObjects)
                {
                    if (targetObject == null)
                    {
                        Debug.LogWarning(
                            $"Upgrade '{upgrade.name}' contains a null " +
                            "target object."
                        );

                        continue;
                    }

                    CaptureUpgradeTargetBaselines(
                        upgrade,
                        targetObject,
                        capturedKeys
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

        private void CaptureUpgradeTargetBaselines(
            UpgradeSO upgrade,
            ScriptableObject targetObject,
            HashSet<string> capturedKeys)
        {
            foreach (string propertyPath in upgrade.ModifiedPropertyPaths)
            {
                if (string.IsNullOrWhiteSpace(propertyPath))
                {
                    continue;
                }

                string key =
                    $"{targetObject.GetInstanceID()}:{propertyPath}";

                // Multiple upgrades may modify the same target property.
                // Capture its original value only once.
                if (!capturedKeys.Add(key))
                {
                    continue;
                }

                if (!TryResolveProperty(
                        targetObject,
                        propertyPath,
                        out object propertyOwner,
                        out PropertyInfo property))
                {
                    Debug.LogError(
                        $"Could not capture baseline for " +
                        $"'{targetObject.name}.{propertyPath}' " +
                        $"used by upgrade '{upgrade.name}'."
                    );

                    continue;
                }

                object value;

                try
                {
                    value = property.GetValue(propertyOwner);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Failed to read baseline value for " +
                        $"'{targetObject.name}.{propertyPath}'.\n" +
                        exception
                    );

                    continue;
                }

                PropertyBaseline baseline = new PropertyBaseline
                {
                    TargetObject = targetObject,
                    PropertyPath = propertyPath
                };

                if (!baseline.TryStoreValue(value, property.PropertyType))
                {
                    Debug.LogError(
                        $"Unsupported baseline type " +
                        $"'{property.PropertyType.Name}' for " +
                        $"'{targetObject.name}.{propertyPath}'."
                    );

                    continue;
                }

                baselines.Add(baseline);

                Debug.Log(
                    $"Captured baseline " +
                    $"'{targetObject.name}.{propertyPath}' = {value}."
                );
            }
        }

        public void RestoreAll()
        {
            foreach (PropertyBaseline baseline in baselines)
            {
                if (baseline == null ||
                    baseline.TargetObject == null ||
                    string.IsNullOrWhiteSpace(baseline.PropertyPath))
                {
                    continue;
                }

                if (!TryResolveProperty(
                        baseline.TargetObject,
                        baseline.PropertyPath,
                        out object propertyOwner,
                        out PropertyInfo property))
                {
                    Debug.LogError(
                        $"Could not restore " +
                        $"'{baseline.TargetObject.name}." +
                        $"{baseline.PropertyPath}'."
                    );

                    continue;
                }

                object value;

                try
                {
                    value = baseline.GetStoredValue(property.PropertyType);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Could not retrieve the stored baseline for " +
                        $"'{baseline.TargetObject.name}." +
                        $"{baseline.PropertyPath}'.\n" +
                        exception
                    );

                    continue;
                }

                try
                {
                    property.SetValue(propertyOwner, value);

                    Debug.Log(
                        $"Restored '{baseline.TargetObject.name}." +
                        $"{baseline.PropertyPath}' to {value}."
                    );
                }
                catch (TargetInvocationException exception)
                {
                    Debug.LogError(
                        $"The setter for " +
                        $"'{baseline.TargetObject.name}." +
                        $"{baseline.PropertyPath}' threw an exception.\n" +
                        (exception.InnerException ?? exception)
                    );
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Failed to restore " +
                        $"'{baseline.TargetObject.name}." +
                        $"{baseline.PropertyPath}'.\n" +
                        exception
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

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim();
            }

            if (parts.Length == 0)
            {
                return false;
            }

            object currentObject = rootObject;

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            for (int i = 0; i < parts.Length; i++)
            {
                if (currentObject == null)
                {
                    return false;
                }

                string propertyName = parts[i];

                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    return false;
                }

                Type currentType = currentObject.GetType();

                PropertyInfo property = currentType.GetProperty(
                    propertyName,
                    flags
                );

                if (property == null)
                {
                    return false;
                }

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

                if (!property.CanRead)
                {
                    return false;
                }

                try
                {
                    currentObject = property.GetValue(currentObject);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }

    [Serializable]
    public class PropertyBaseline
    {
        private enum StoredValueType
        {
            Int,
            Long,
            Float,
            Double,
            Bool,
            String,
            Vector2,
            Vector3,
            Color,
            Enum
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
        private long longValue;

        [SerializeField]
        private float floatValue;

        [SerializeField]
        private double doubleValue;

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

        [SerializeField]
        private string enumValueName;

        public bool TryStoreValue(
            object value,
            Type propertyType)
        {
            if (propertyType == null)
            {
                return false;
            }

            if (propertyType.IsEnum)
            {
                if (value == null)
                {
                    return false;
                }

                valueType = StoredValueType.Enum;
                enumValueName = value.ToString();

                return true;
            }

            switch (value)
            {
                case int typedValue:
                    valueType = StoredValueType.Int;
                    intValue = typedValue;
                    return true;

                case long typedValue:
                    valueType = StoredValueType.Long;
                    longValue = typedValue;
                    return true;

                case float typedValue:
                    valueType = StoredValueType.Float;
                    floatValue = typedValue;
                    return true;

                case double typedValue:
                    valueType = StoredValueType.Double;
                    doubleValue = typedValue;
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

        public object GetStoredValue(Type propertyType)
        {
            switch (valueType)
            {
                case StoredValueType.Int:
                    return intValue;

                case StoredValueType.Long:
                    return longValue;

                case StoredValueType.Float:
                    return floatValue;

                case StoredValueType.Double:
                    return doubleValue;

                case StoredValueType.Bool:
                    return boolValue;

                case StoredValueType.String:
                    return stringValue;

                case StoredValueType.Vector2:
                    return vector2Value;

                case StoredValueType.Vector3:
                    return vector3Value;

                case StoredValueType.Color:
                    return colorValue;

                case StoredValueType.Enum:
                    if (propertyType == null || !propertyType.IsEnum)
                    {
                        throw new InvalidOperationException(
                            $"Stored baseline is an enum, but property type " +
                            $"'{propertyType?.Name ?? "null"}' is not an enum."
                        );
                    }

                    return Enum.Parse(
                        propertyType,
                        enumValueName
                    );

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}