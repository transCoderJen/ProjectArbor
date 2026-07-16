using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftedSignal.Garden.Stats
{
    [Serializable]
    public class Stat
    {
        [SerializeField] private float baseValue;

        private readonly List<float> modifiers = new();

        public float BaseValue => baseValue;

        public float CurrentValue => baseValue + GetModifiersValue();

        public Stat(float baseValue)
        {
            this.baseValue = baseValue;
        }
        
        public float GetValue()
        {
            return CurrentValue;
        }

        public float GetModifiersValue()
        {
            float total = 0f;

            foreach (float modifier in modifiers)
            {
                total += modifier;
            }

            return total;
        }

        public void SetDefaultValue(float value)
        {
            baseValue = value;
        }

        public void AddModifier(float modifier)
        {
            modifiers.Add(modifier);
        }

        public void RemoveModifier(float modifier)
        {
            modifiers.Remove(modifier);
        }

        public void ClearModifiers()
        {
            modifiers.Clear();
        }

        public override string ToString()
        {
            return $"{CurrentValue} (Base: {BaseValue}, Modifiers: {GetModifiersValue()})";
        }
    }
}