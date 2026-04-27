using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShiftedSignal.Garden.UserInterface
{
    
    public class UI_StatSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string StatName;
        [SerializeField] private StatType StatType;
        [SerializeField] private TextMeshProUGUI StatValueText;
        [SerializeField] private TextMeshProUGUI StatNameText;

        private UI Ui;

        private void OnValidate()
        {
            gameObject.name = "Stat - " + StatName;

            if (StatNameText != null)
                StatNameText.text = StatName;
        }

        void Start()
        {
            UpdateStatValueUI();
            Ui = GetComponentInParent<UI>();
        }

        public void UpdateStatValueUI()
        {
            CharacterStats playerStats = PlayerManager.Instance.Player.GetComponent<CharacterStats>();

            if (playerStats == null)
            {
                Debug.Log("PlayerStats is null");
                return;
            }

            switch (StatType)
            {
                case StatType.MaxHP:
                    StatValueText.text = playerStats.GetMaxHealthValue().ToString();
                    break;

                case StatType.MaxMP:
                    StatValueText.text = playerStats.GetStat(StatType).GetValue().ToString();
                    break;

                case StatType.Power:
                    StatValueText.text = playerStats.GetTotalDamage().ToString();
                    break;

                case StatType.Vitality:
                case StatType.Defense:
                case StatType.Speed:
                    StatValueText.text = playerStats.GetStat(StatType).GetValue().ToString();
                    break;

                case StatType.CritChance:
                    StatValueText.text = playerStats.GetStat(StatType).GetValue() + "%";
                    break;

                case StatType.CritPower:
                    StatValueText.text = playerStats.GetStat(StatType).GetValue() + "%";
                    break;

                case StatType.Evasion:
                    StatValueText.text = playerStats.GetStat(StatType).GetValue() + "%";
                    break;

                case StatType.MagicResistance:
                    StatValueText.text = playerStats.GetStat(StatType).GetValue().ToString();
                    break;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            //TODO OnPointerEnter Show Tooltip
            // Ui.statTooltip.ShowToolTip(StatType);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //TODO OnPointerExit HideTooltip
            // Ui.statTooltip.HideToolTip();
        }
    }
}