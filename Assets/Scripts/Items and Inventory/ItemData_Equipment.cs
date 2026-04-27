using System.Collections.Generic;
using ShiftedSignal.Garden.Effects;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Stats;
using UnityEngine;

namespace ShiftedSignal.Garden.ItemsAndInventory
{
    public enum EquipmentType
    {
        Tool,
        Weapon,
        Armor,
        Amulet,
        Flask
    }

    [CreateAssetMenu(fileName = "New Item Data", menuName = "Data/Equipment")]
    public class ItemData_Equipment : ItemData
    {
        [Header("Equipment")]
        public EquipmentType EquipmentType;
        public GameObject Weapon;

        [Header("Use / Effects")]
        public float ItemCooldown;
        public PooledObjectList SlashFX;
        public PooledObjectList HitFX;
        public ItemEffect[] ItemEffects;

        [Header("Stat Modifiers (match PlayerStats.cs)")]
        public int HP;          // Flat HP bonus
        public int MP;           // Flat MP bonus
        public int Power;             // All damage scaling
        public int Vitality;          // Health scaling
        public int Defense;           // Damage reduction
        public int Speed;             // Movement speed

        [Header("Combat Modifiers (match PlayerStats.cs)")]
        public int CritChance;        // %
        public int CritPower;         // % (e.g., +15 means +15% crit power)
        public int Evasion;           // %
        public int MagicResistance;   // Flat or %, depending on your system

        [Header("Optional / Not in PlayerStats.cs yet")]
        public int AttackSpeed;       // Keep if you want gear to affect attack cadence

        [Header("Craft Requirements")]
        public List<InventoryItem> craftingMaterials;

        private int descriptionLength;

        public void Effect(Transform _spawnPosition)
        {
            if (ItemEffects == null) return;

            foreach (var item in ItemEffects)
            {
                if (item == null) continue;
                item.ExecuteEffect(_spawnPosition);
            }
        }

        public void AddModifiers()
        {
            PlayerStats playerStats = PlayerManager.Instance.Player.GetComponent<PlayerStats>();

            playerStats.MaxHP.AddModifier(HP);
            playerStats.MaxMP.AddModifier(MP);
            playerStats.Power.AddModifier(Power);
            playerStats.Vitality.AddModifier(Vitality);
            playerStats.Defense.AddModifier(Defense);
            playerStats.Speed.AddModifier(Speed);
            playerStats.CritChance.AddModifier(CritChance);
            playerStats.CritPower.AddModifier(CritPower);
            playerStats.Evasion.AddModifier(Evasion);
            playerStats.MagicResistance.AddModifier(MagicResistance);
        }

        public void RemoveModifiers()
        {
            PlayerStats playerStats = PlayerManager.Instance.Player.GetComponent<PlayerStats>();

            playerStats.MaxHP.RemoveModifier(HP);
            playerStats.MaxMP.RemoveModifier(MP);
            playerStats.Power.RemoveModifier(Power);
            playerStats.Vitality.RemoveModifier(Vitality);
            playerStats.Defense.RemoveModifier(Defense);
            playerStats.Speed.RemoveModifier(Speed);
            playerStats.CritChance.RemoveModifier(CritChance);
            playerStats.CritPower.RemoveModifier(CritPower);
            playerStats.Evasion.RemoveModifier(Evasion);
            playerStats.MagicResistance.RemoveModifier(MagicResistance);
        }

        public override string GetDescription()
        {
            sb.Length = 0;
            descriptionLength = 0;

            // Core stats
            AddItemDescription(HP, "Health");
            AddItemDescription(MP, "Magic");
            AddItemDescription(Power, "Power");
            AddItemDescription(Vitality, "Vitality");
            AddItemDescription(Defense, "Defense");
            AddItemDescription(Speed, "Speed");

            // Combat stats
            AddItemDescription(CritChance, "Crit Chance", percent: true);

            // CritPower is usually shown like “+15% Crit Power” (on top of base 150%)
            AddItemDescription(CritPower, "Crit Power", percent: true);

            AddItemDescription(Evasion, "Evasion", percent: true);

            // If MagicResistance is percent in your system, flip this to percent:true
            AddItemDescription(MagicResistance, "Magic Resistance");

            // Optional
            AddItemDescription(AttackSpeed, "Attack Speed", percent: true);

            // Base ItemData description text (keeps your existing pattern)
            AddItemDescription(1000000, Description);

            // Effects text
            if (ItemEffects != null)
            {
                for (int i = 0; i < ItemEffects.Length; i++)
                {
                    if (ItemEffects[i] == null) continue;

                    // Assuming ItemEffect has effectDescription
                    if (!string.IsNullOrEmpty(ItemEffects[i].EffectDescription))
                    {
                        sb.AppendLine();
                        sb.Append(ItemEffects[i].EffectDescription);

                        int lines = ItemEffects[i].EffectDescription.Split('\n').Length;
                        descriptionLength += lines;
                    }
                }
            }

            // Cooldown line
            if (ItemCooldown > 0)
            {
                sb.AppendLine();
                sb.Append("Cooldown: " + ItemCooldown + " seconds");
                descriptionLength++;
            }

            // Pad so UI boxes look consistent
            if (descriptionLength < 5)
            {
                for (int i = 0; i < 5 - descriptionLength; i++)
                    sb.AppendLine();
            }

            return sb.ToString();
        }

        private void AddItemDescription(int _value, string _name, bool percent = false)
        {
            if (_value == 0) return;

            sb.AppendLine();

            if (_value == 1000000)
            {
                if (!string.IsNullOrEmpty(_name))
                {
                    sb.Append(_name);
                    int lines = _name.Split('\n').Length;
                    descriptionLength += lines;
                }
                return;
            }

            if (_value > 0)
            {
                if (percent)
                    sb.Append("+ " + _value + "% " + _name);
                else
                    sb.Append("+ " + _value + " " + _name);

                descriptionLength++;
            }
        }
    }
}