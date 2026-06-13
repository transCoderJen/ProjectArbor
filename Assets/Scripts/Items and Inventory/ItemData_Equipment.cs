using System.Collections.Generic;
using ShiftedSignal.Garden.Effects;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using UnityEngine;

namespace ShiftedSignal.Garden.ItemsAndInventory
{
    [CreateAssetMenu(fileName = "New Usable Item Data", menuName = "Data/Usable Item")]
    public class ItemData_Equipment : ItemData
    {
        [Header("Use / Effects")]
        public float ItemCooldown;
        public PooledObjectList SlashFX;
        public PooledObjectList HitFX;
        public ItemEffect[] ItemEffects;

        [Header("Craft Requirements")]
        public List<InventoryItem> craftingMaterials;

        private int descriptionLength;

        public void Effect(Transform spawnPosition)
        {
            if (ItemEffects == null)
                return;

            foreach (ItemEffect item in ItemEffects)
            {
                if (item == null)
                    continue;

                item.ExecuteEffect(spawnPosition);
            }
        }

        public override string GetDescription()
        {
            sb.Length = 0;
            descriptionLength = 0;

            AddDescriptionText(Description);

            AddEffectDescriptions();

            AddCooldownDescription();

            PadDescription();

            return sb.ToString();
        }

        private void AddDescriptionText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            sb.Append(text);
            descriptionLength += text.Split('\n').Length;
        }

        private void AddEffectDescriptions()
        {
            if (ItemEffects == null)
                return;

            foreach (ItemEffect effect in ItemEffects)
            {
                if (effect == null)
                    continue;

                if (string.IsNullOrEmpty(effect.EffectDescription))
                    continue;

                if (sb.Length > 0)
                    sb.AppendLine();

                sb.Append(effect.EffectDescription);

                descriptionLength += effect.EffectDescription.Split('\n').Length;
            }
        }

        private void AddCooldownDescription()
        {
            if (ItemCooldown <= 0)
                return;

            if (sb.Length > 0)
                sb.AppendLine();

            sb.Append("Cooldown: " + ItemCooldown + " seconds");
            descriptionLength++;
        }

        private void PadDescription()
        {
            if (descriptionLength >= 5)
                return;

            for (int i = 0; i < 5 - descriptionLength; i++)
                sb.AppendLine();
        }
    }
}