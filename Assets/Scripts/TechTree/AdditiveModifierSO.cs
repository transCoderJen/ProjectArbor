using System;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.TechTree
{
    [CreateAssetMenu(
        fileName = "Additive Modifier",
        menuName = "Tech Tree/Modifiers/Additive Modifier",
        order = 160)]
    public class AdditiveModifierSO : UpgradeSO
    {
        [field: SerializeField]
        public AbstractUnitSO Target { get; private set; }

        [field: SerializeField]
        public float Amount { get; private set; }

        public override ScriptableObject TargetObject => Target;

        public override void Apply()
        {
            ResolvedProperty property = ResolveProperty();

            if (property.PropertyType == typeof(int))
            {
                int currentValue = property.GetValue<int>();
                int amount = Mathf.RoundToInt(Amount);
                int updatedValue = currentValue + amount;

                property.SetValue(updatedValue);

                LogChange(currentValue, updatedValue);
                return;
            }

            if (property.PropertyType == typeof(float))
            {
                float currentValue = property.GetValue<float>();
                float updatedValue = currentValue + Amount;

                property.SetValue(updatedValue);

                LogChange(currentValue, updatedValue);
                return;
            }

            if (property.PropertyType == typeof(double))
            {
                double currentValue = property.GetValue<double>();
                double updatedValue = currentValue + Amount;

                property.SetValue(updatedValue);

                LogChange(currentValue, updatedValue);
                return;
            }

            if (property.PropertyType == typeof(long))
            {
                long currentValue = property.GetValue<long>();
                long amount = Mathf.RoundToInt(Amount);
                long updatedValue = currentValue + amount;

                property.SetValue(updatedValue);

                LogChange(currentValue, updatedValue);
                return;
            }

            throw new InvalidOperationException(
                $"Additive upgrade '{name}' cannot modify " +
                $"'{PropertyPath}' because its type is " +
                $"{property.PropertyType.FullName}."
            );
        }

        private void LogChange(object previousValue, object updatedValue)
        {
            Debug.Log(
                $"{name} changed {Target.name}.{PropertyPath} " +
                $"from {previousValue} to {updatedValue}."
            );
        }
    }
}