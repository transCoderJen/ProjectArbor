using System.Collections.Generic;
using UnityEngine;

namespace ShiftedSignal.Garden.Stats
{
    [System.Serializable]
    public class Stat
    {
        [SerializeField] private int baseValue;

        private readonly List<int> modifiers = new();

        public int BaseValue => baseValue;

        public int GetValue()
        {
            return baseValue + GetModifiersValue();
        }

        public int GetModifiersValue()
        {
            int total = 0;

            foreach (int modifier in modifiers)
            {
                total += modifier;
            }

            return total;
        }

        public void SetDefaultValue(int value)
        {
            baseValue = value;
        }

        public void AddModifier(int modifier)
        {
            modifiers.Add(modifier);
        }

        public void RemoveModifier(int modifier)
        {
            modifiers.Remove(modifier);
        }

        public void ClearModifiers()
        {
            modifiers.Clear();
        }
    }
}