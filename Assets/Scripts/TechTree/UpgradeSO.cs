using System;
using System.Collections.Generic;
using System.Reflection;
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

        [field: Header("Targets")]
        [field: SerializeField]
        public List<UnitSO> TargetObjects { get; private set; } = new(1);

        public string UpgradeID => upgradeID;

        public virtual IEnumerable<string> ModifiedPropertyPaths
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PropertyPath))
                {
                    yield return PropertyPath;
                }
            }
        }

        /// <summary>
        /// Applies this upgrade to one target object.
        /// UpgradeManager should call this once for every object in TargetObjects.
        /// </summary>
        public abstract void Apply(ScriptableObject targetObject);

        /// <summary>
        /// Resolves PropertyPath against a specific target object.
        /// </summary>
        protected ResolvedProperty ResolveProperty(
            ScriptableObject targetObject)
        {
            if (targetObject == null)
            {
                throw new InvalidPropertyPathException(
                    PropertyPath,
                    $"Upgrade '{name}' was given a null target object."
                );
            }

            if (string.IsNullOrWhiteSpace(PropertyPath))
            {
                throw new InvalidPropertyPathException(
                    PropertyPath,
                    $"Upgrade '{name}' has an empty property path."
                );
            }

            string[] attributes = PropertyPath.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries
            );

            for (int i = 0; i < attributes.Length; i++)
            {
                attributes[i] = attributes[i].Trim();
            }

            if (attributes.Length == 0)
            {
                throw new InvalidPropertyPathException(
                    PropertyPath,
                    "The path did not contain any valid property names."
                );
            }

            object currentOwner = targetObject;

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            for (int i = 0; i < attributes.Length; i++)
            {
                string propertyName = attributes[i];

                if (currentOwner == null)
                {
                    throw new InvalidPropertyPathException(
                        PropertyPath,
                        $"The object before '{propertyName}' was null."
                    );
                }

                Type currentType = currentOwner.GetType();

                PropertyInfo property = currentType.GetProperty(
                    propertyName,
                    flags
                );

                if (property == null)
                {
                    throw new InvalidPropertyPathException(
                        PropertyPath,
                        propertyName,
                        currentType
                    );
                }

                bool isFinalProperty =
                    i == attributes.Length - 1;

                if (isFinalProperty)
                {
                    if (!property.CanRead)
                    {
                        throw new InvalidPropertyPathException(
                            PropertyPath,
                            $"Final property '{propertyName}' cannot be read."
                        );
                    }

                    return new ResolvedProperty(
                        currentOwner,
                        property,
                        PropertyPath
                    );
                }

                if (!property.CanRead)
                {
                    throw new InvalidPropertyPathException(
                        PropertyPath,
                        $"Intermediate property '{propertyName}' " +
                        $"cannot be read."
                    );
                }

                try
                {
                    currentOwner = property.GetValue(currentOwner);
                }
                catch (TargetInvocationException exception)
                {
                    throw new InvalidPropertyPathException(
                        PropertyPath,
                        $"The getter for intermediate property " +
                        $"'{propertyName}' threw an exception: " +
                        $"{exception.InnerException?.Message ?? exception.Message}"
                    );
                }
                catch (Exception exception)
                {
                    throw new InvalidPropertyPathException(
                        PropertyPath,
                        $"Failed to read intermediate property " +
                        $"'{propertyName}': {exception.Message}"
                    );
                }
            }

            throw new InvalidPropertyPathException(
                PropertyPath,
                "The final property could not be resolved."
            );
        }

        protected readonly struct ResolvedProperty
        {
            public object Owner { get; }

            public PropertyInfo Property { get; }

            public string Path { get; }

            public Type PropertyType => Property.PropertyType;

            public bool CanRead => Property.CanRead;

            public bool CanWrite =>
                Property.GetSetMethod(true) != null;

            public ResolvedProperty(
                object owner,
                PropertyInfo property,
                string path)
            {
                Owner = owner;
                Property = property;
                Path = path;
            }

            public object GetValue()
            {
                if (!CanRead)
                {
                    throw new InvalidOperationException(
                        $"Property '{Path}' cannot be read."
                    );
                }

                try
                {
                    return Property.GetValue(Owner);
                }
                catch (TargetInvocationException exception)
                {
                    throw new InvalidOperationException(
                        $"The getter for '{Path}' threw an exception.",
                        exception.InnerException ?? exception
                    );
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Failed to read property '{Path}'.",
                        exception
                    );
                }
            }

            public T GetValue<T>()
            {
                object value = GetValue();

                if (value == null)
                {
                    Type requestedType = typeof(T);

                    bool acceptsNull =
                        !requestedType.IsValueType ||
                        Nullable.GetUnderlyingType(requestedType) != null;

                    if (!acceptsNull)
                    {
                        throw new InvalidCastException(
                            $"Property '{Path}' returned null, but " +
                            $"{requestedType.FullName} cannot contain null."
                        );
                    }

                    return default;
                }

                if (!(value is T typedValue))
                {
                    throw new InvalidCastException(
                        $"Property '{Path}' is type " +
                        $"{value.GetType().FullName}, but " +
                        $"{typeof(T).FullName} was requested."
                    );
                }

                return typedValue;
            }

            public void SetValue(object value)
            {
                if (!CanWrite)
                {
                    throw new InvalidOperationException(
                        $"Property '{Path}' does not have a setter."
                    );
                }

                Type propertyType = PropertyType;

                if (value == null)
                {
                    bool acceptsNull =
                        !propertyType.IsValueType ||
                        Nullable.GetUnderlyingType(propertyType) != null;

                    if (!acceptsNull)
                    {
                        throw new InvalidCastException(
                            $"Cannot assign null to non-nullable property " +
                            $"'{Path}' of type {propertyType.FullName}."
                        );
                    }
                }
                else if (!propertyType.IsInstanceOfType(value))
                {
                    throw new InvalidCastException(
                        $"Cannot assign a value of type " +
                        $"{value.GetType().FullName} to property " +
                        $"'{Path}' of type {propertyType.FullName}."
                    );
                }

                try
                {
                    Property.SetValue(Owner, value);
                }
                catch (TargetInvocationException exception)
                {
                    throw new InvalidOperationException(
                        $"The setter for '{Path}' threw an exception.",
                        exception.InnerException ?? exception
                    );
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Failed to set property '{Path}'.",
                        exception
                    );
                }
            }

            public void SetValue<T>(T value)
            {
                SetValue((object)value);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (string.IsNullOrWhiteSpace(upgradeID))
            {
                upgradeID = Guid.NewGuid().ToString();

                UnityEditor.EditorUtility.SetDirty(this);
            }

            for (int i = TargetObjects.Count - 1; i >= 0; i--)
            {
                if (TargetObjects[i] == null)
                {
                    TargetObjects.RemoveAt(i);
                }
            }
        }
#endif
    }
}