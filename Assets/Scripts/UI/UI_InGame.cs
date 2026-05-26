using UnityEngine;
using UnityEngine.UI; // Required for Image components
using TMPro;
using System;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Stats; // Required to access CharacterStats

namespace ShiftedSignal.Garden.UserInterface
{
    public class UI_InGame : MonoBehaviour
    {
        [Header("Time UI")]
        [SerializeField] private TextMeshProUGUI TimeText;
        [SerializeField] private TextMeshProUGUI DayText; 
        
        [Header("Day Period Icons")]
        [SerializeField] private Image DayPeriodIcon;
        [SerializeField] private Sprite DawnSprite;
        [SerializeField] private Sprite MorningSprite;
        [SerializeField] private Sprite AfternoonSprite;
        [SerializeField] private Sprite EveningSprite;
        [SerializeField] private Sprite NightSprite;

        [Header("Player Stats UI")]
        [SerializeField] private TextMeshProUGUI HealthText;
        [SerializeField] private TextMeshProUGUI MpText;
        [SerializeField] private TextMeshProUGUI CurrencyText;
        [SerializeField] private TextMeshProUGUI LevelText; 

        private CharacterStats playerStats;

        void Start()
        {
            // Grab reference to player stats
            if (PlayerManager.Instance != null && PlayerManager.Instance.Player != null)
            {
                playerStats = PlayerManager.Instance.Player.Stats;
                
                // Subscribe directly to the actions in CharacterStats
                playerStats.OnHealthChanged += UpdateHealthUI;
                playerStats.OnMagicChanged += UpdateMpUI; 
            }

            AddEventHandlers();
            
            // Force an initial update of all UI elements to reflect current state
            UpdateAllUI();
        }

        private void OnEnable()
        {
            AddEventHandlers();
        }

        private void OnDisable()
        {
            RemoveEventHandlers();
            
            // Unsubscribe from stat events to prevent memory leaks
            if (playerStats != null)
            {
                playerStats.OnHealthChanged -= UpdateHealthUI;
                playerStats.OnMagicChanged -= UpdateMpUI;
            }
        }

        private void AddEventHandlers()
        {
            Bus<TimeChangedEvent>.OnEvent += UpdateTimeUI;
            Bus<DayStartedEvent>.OnEvent += UpdateDayUI;
            
            // Add this line to listen for day changes (Midnight & Save Loads)
            Bus<DayChangedEvent>.OnEvent += UpdateDayUI; 
            
            Bus<DayPeriodChangedEvent>.OnEvent += UpdateDayPeriodUI;
            Bus<CurrencyUpdatedEvent>.OnEvent += HandleCurrencyUpdate;
            Bus<PlayerLevelUpEvent>.OnEvent += UpdateLevelUI;
        }

        private void RemoveEventHandlers()
        {
            Bus<TimeChangedEvent>.OnEvent -= UpdateTimeUI;
            Bus<DayStartedEvent>.OnEvent -= UpdateDayUI;
            
            // Add this line to unsubscribe cleanly
            Bus<DayChangedEvent>.OnEvent -= UpdateDayUI; 
            
            Bus<DayPeriodChangedEvent>.OnEvent -= UpdateDayPeriodUI;
            Bus<CurrencyUpdatedEvent>.OnEvent -= HandleCurrencyUpdate;
            Bus<PlayerLevelUpEvent>.OnEvent -= UpdateLevelUI;
        }

        private void UpdateAllUI()
        {
            UpdateTimeUI();
            UpdateDayUI(new DayStartedEvent());
            UpdateDayPeriodUI(new DayPeriodChangedEvent(TimeManger.Instance.CurrentDayPeriod));
            UpdateHealthUI();
            UpdateMpUI();
            UpdateCurrencyUI();
            
            if (playerStats != null && playerStats is PlayerStats pStats)
                UpdateLevelUI(new PlayerLevelUpEvent(pStats.Level));
        }

        // --- Time & Day Methods ---

        private void UpdateDayPeriodUI(DayPeriodChangedEvent evt)
        {
            if (DayPeriodIcon == null) return;

            switch(evt.DayPeriod)
            {
                case DayPeriod.Dawn:
                    DayPeriodIcon.sprite = DawnSprite;
                    break;
                case DayPeriod.Morning:
                    DayPeriodIcon.sprite = MorningSprite;
                    break;
                case DayPeriod.Afternoon:
                    DayPeriodIcon.sprite = AfternoonSprite;
                    break;
                case DayPeriod.Evening:
                    DayPeriodIcon.sprite = EveningSprite;
                    break;
                case DayPeriod.Night:
                    DayPeriodIcon.sprite = NightSprite;
                    break;
            }
        }

        private void UpdateDayUI(DayStartedEvent args)
        {
            if (DayText != null)
                DayText.text = "Day " + TimeManger.Instance.CurrentDay;
        }

        private void UpdateDayUI(DayChangedEvent evt)
        {
            if (DayText != null)
                DayText.text = "Day " + evt.Day;
        }

        private void UpdateTimeUI(TimeChangedEvent evt)
        {
            UpdateTimeUI();
        }

        private void UpdateTimeUI()
        {
            if (TimeText != null)
                TimeText.text = TimeManger.Instance.FormattedTime;
        }

        // --- Player Stat Methods ---

        private void UpdateHealthUI()
        {
            if (HealthText != null && playerStats != null)
            {
                HealthText.text = $"{playerStats.CurrentHealth} / {playerStats.GetMaxHealthValue()}";
            }
        }

        private void UpdateMpUI()
        {
            if (MpText != null && playerStats != null)
            {
                MpText.text = $"{playerStats.CurrentMP} / {playerStats.MaxMP.GetValue()}";
            }
        }

        // We trigger an update when the CurrencyUpdatedEvent fires. 
        // Note: evt.Coins only contains the *amount added*, so we read the total directly from the PlayerManager.
        private void HandleCurrencyUpdate(CurrencyUpdatedEvent evt)
        {
            UpdateCurrencyUI();
        }

        private void UpdateCurrencyUI()
        {
            if (CurrencyText != null && PlayerManager.Instance != null)
            {
                CurrencyText.text = PlayerManager.Instance.Currency.ToString();
            }
        }

        private void UpdateLevelUI(PlayerLevelUpEvent evt)
        {
            if (LevelText != null)
            {
                LevelText.text = "Lvl " + evt.Level;
            }
        }
    }
}