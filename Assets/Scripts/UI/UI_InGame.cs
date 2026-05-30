using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Stats;

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

        [Header("Number Scroll Settings")]
        [SerializeField] private float NumberScrollSpeed = 75f;

        private CharacterStats playerStats;

        private float displayedHealth;
        private float targetHealth;

        private float displayedMp;
        private float targetMp;

        private float displayedCurrency;
        private float targetCurrency;

        private float displayedDay;
        private float targetDay;

        private float displayedLevel;
        private float targetLevel;

        private void Start()
        {
            CachePlayerStats();
            UpdateAllUIImmediate();
        }

        private void OnEnable()
        {
            AddEventHandlers();
            CachePlayerStats();
        }

        private void OnDisable()
        {
            RemoveEventHandlers();

            if (playerStats != null)
            {
                playerStats.OnHealthChanged -= UpdateHealthUI;
                playerStats.OnMagicChanged -= UpdateMpUI;
            }
        }

        private void Update()
        {
            ScrollHealthUI();
            ScrollMpUI();
            ScrollCurrencyUI();
            ScrollDayUI();
            ScrollLevelUI();
        }

        private void CachePlayerStats()
        {
            if (playerStats != null)
                return;

            if (PlayerManager.Instance == null || PlayerManager.Instance.Player == null)
                return;

            playerStats = PlayerManager.Instance.Player.Stats;

            playerStats.OnHealthChanged += UpdateHealthUI;
            playerStats.OnMagicChanged += UpdateMpUI;
        }

        private void AddEventHandlers()
        {
            Bus<TimeChangedEvent>.OnEvent += UpdateTimeUI;
            Bus<DayStartedEvent>.OnEvent += UpdateDayUI;
            Bus<DayChangedEvent>.OnEvent += UpdateDayUI;
            Bus<DayPeriodChangedEvent>.OnEvent += UpdateDayPeriodUI;
            Bus<CurrencyUpdatedEvent>.OnEvent += HandleCurrencyUpdate;
            Bus<PlayerLevelUpEvent>.OnEvent += UpdateLevelUI;
        }

        private void RemoveEventHandlers()
        {
            Bus<TimeChangedEvent>.OnEvent -= UpdateTimeUI;
            Bus<DayStartedEvent>.OnEvent -= UpdateDayUI;
            Bus<DayChangedEvent>.OnEvent -= UpdateDayUI;
            Bus<DayPeriodChangedEvent>.OnEvent -= UpdateDayPeriodUI;
            Bus<CurrencyUpdatedEvent>.OnEvent -= HandleCurrencyUpdate;
            Bus<PlayerLevelUpEvent>.OnEvent -= UpdateLevelUI;
        }

        private void UpdateAllUIImmediate()
        {
            UpdateTimeUI();

            if (TimeManger.Instance != null)
            {
                SetDayImmediate(TimeManger.Instance.CurrentDay);
                UpdateDayPeriodUI(new DayPeriodChangedEvent(TimeManger.Instance.CurrentDayPeriod));
            }

            if (playerStats != null)
            {
                SetHealthImmediate(playerStats.CurrentHealth);
                SetMpImmediate(playerStats.CurrentMP);
            }

            if (PlayerManager.Instance != null)
                SetCurrencyImmediate(PlayerManager.Instance.Currency);

            if (playerStats is PlayerStats pStats)
                SetLevelImmediate(pStats.Level);
        }

        private void UpdateDayPeriodUI(DayPeriodChangedEvent evt)
        {
            if (DayPeriodIcon == null)
                return;

            switch (evt.DayPeriod)
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
            if (TimeManger.Instance != null)
                targetDay = TimeManger.Instance.CurrentDay;
        }

        private void UpdateDayUI(DayChangedEvent evt)
        {
            targetDay = evt.Day;
        }

        private void UpdateTimeUI(TimeChangedEvent evt)
        {
            UpdateTimeUI();
        }

        private void UpdateTimeUI()
        {
            if (TimeText != null && TimeManger.Instance != null)
                TimeText.text = TimeManger.Instance.FormattedTime;
        }

        private void UpdateHealthUI()
        {
            if (playerStats != null)
                targetHealth = playerStats.CurrentHealth;
        }

        private void UpdateMpUI()
        {
            if (playerStats != null)
                targetMp = playerStats.CurrentMP;
        }

        private void HandleCurrencyUpdate(CurrencyUpdatedEvent evt)
        {
            if (PlayerManager.Instance != null)
                targetCurrency = PlayerManager.Instance.Currency;
        }

        private void UpdateLevelUI(PlayerLevelUpEvent evt)
        {
            targetLevel = evt.Level;
        }

        private void ScrollHealthUI()
        {
            if (HealthText == null || playerStats == null)
                return;

            displayedHealth = ScrollNumber(displayedHealth, targetHealth);

            HealthText.text =
                $"{Mathf.RoundToInt(displayedHealth)} / {playerStats.GetMaxHealthValue()}";
        }

        private void ScrollMpUI()
        {
            if (MpText == null || playerStats == null)
                return;

            displayedMp = ScrollNumber(displayedMp, targetMp);

            MpText.text =
                $"{Mathf.RoundToInt(displayedMp)} / {playerStats.MaxMP.GetValue()}";
        }

        private void ScrollCurrencyUI()
        {
            if (CurrencyText == null)
                return;

            displayedCurrency = ScrollNumber(displayedCurrency, targetCurrency);
            CurrencyText.text = Mathf.RoundToInt(displayedCurrency).ToString();
        }

        private void ScrollDayUI()
        {
            if (DayText == null)
                return;

            displayedDay = ScrollNumber(displayedDay, targetDay);
            DayText.text = "Day " + Mathf.RoundToInt(displayedDay);
        }

        private void ScrollLevelUI()
        {
            if (LevelText == null)
                return;

            displayedLevel = ScrollNumber(displayedLevel, targetLevel);
            LevelText.text = "Lvl " + Mathf.RoundToInt(displayedLevel);
        }

        private float ScrollNumber(float current, float target)
        {
            if (Mathf.Approximately(current, target))
                return target;

            return Mathf.MoveTowards(
                current,
                target,
                NumberScrollSpeed * Time.unscaledDeltaTime);
        }

        private void SetHealthImmediate(int value)
        {
            displayedHealth = value;
            targetHealth = value;

            if (HealthText != null && playerStats != null)
                HealthText.text = $"{value} / {playerStats.GetMaxHealthValue()}";
        }

        private void SetMpImmediate(int value)
        {
            displayedMp = value;
            targetMp = value;

            if (MpText != null && playerStats != null)
                MpText.text = $"{value} / {playerStats.MaxMP.GetValue()}";
        }

        private void SetCurrencyImmediate(int value)
        {
            displayedCurrency = value;
            targetCurrency = value;

            if (CurrencyText != null)
                CurrencyText.text = value.ToString();
        }

        private void SetDayImmediate(int value)
        {
            displayedDay = value;
            targetDay = value;

            if (DayText != null)
                DayText.text = "Day " + value;
        }

        private void SetLevelImmediate(int value)
        {
            displayedLevel = value;
            targetLevel = value;

            if (LevelText != null)
                LevelText.text = "Lvl " + value;
        }
    }
}