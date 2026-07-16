using System;
using System.Reflection;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.TechTree
{
    [CreateAssetMenu(
        fileName = "Additive Int Modifier",
        menuName = "Tech Tree/Modifiers/Additive Int Modifier",
        order = 160)]
    public class AdditiveIntModifierSO : UpgradeSO
    {
        [field: SerializeField]
        public AbstractUnitSO Target { get; private set; }

        [field: SerializeField]
        public int Amount { get; private set; }

        public override ScriptableObject TargetObject => Target;

        public override void Apply()
        {
            if (Target == null)
            {
                Debug.LogError(
                    $"{name} cannot apply because its target is null."
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(PropertyPath))
            {
                Debug.LogError(
                    $"{name} cannot apply because PropertyPath is empty."
                );

                return;
            }

            string[] attributes = PropertyPath.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries
            );

            Type type = Target.GetType();
            object targetObject = Target;

            for (int i = 0; i < attributes.Length - 1; i++)
            {
                PropertyInfo property = type.GetProperty(attributes[i]);

                if (property == null)
                {
                    Debug.LogError(
                        $"Unable to apply {Name} to '{PropertyPath}'. " +
                        $"Property '{attributes[i]}' does not exist on " +
                        $"{type.Name}."
                    );

                    return;
                }

                targetObject = property.GetValue(targetObject);

                if (targetObject == null)
                {
                    Debug.LogError(
                        $"Unable to apply {Name} to '{PropertyPath}'. " +
                        $"Property '{attributes[i]}' returned null."
                    );

                    return;
                }

                type = targetObject.GetType();
            }

            string finalAttribute = attributes[^1];

            PropertyInfo attributeProperty =
                type.GetProperty(finalAttribute);

            if (attributeProperty == null)
            {
                Debug.LogError(
                    $"Unable to apply {Name} to '{PropertyPath}'. " +
                    $"Property '{finalAttribute}' does not exist on " +
                    $"{type.Name}."
                );

                return;
            }

            if (attributeProperty.PropertyType != typeof(int))
            {
                Debug.LogError(
                    $"Expected '{PropertyPath}' to be an int, but it is " +
                    $"{attributeProperty.PropertyType.Name}."
                );

                return;
            }

            int currentValue =
                (int)attributeProperty.GetValue(targetObject);

            int updatedValue = currentValue + Amount;

            attributeProperty.SetValue(targetObject, updatedValue);

            Debug.Log(
                $"{name} changed {Target.name}.{PropertyPath} " +
                $"from {currentValue} to {updatedValue}."
            );
        }
    }
}