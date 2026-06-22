using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Stats;
using ShiftedSignal.Garden.QuestSystem;
using ShiftedSignal.Garden.UserInterface.Components;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;

namespace ShiftedSignal.Garden.UserInterface.Managers
{
    public class UI_InGame : MonoBehaviour
    {
        [Header("Time UI")]
        [SerializeField] private TextMeshProUGUI TimeText;
        [SerializeField] private TextMeshProUGUI DayText;

        [Header("Quest UI")]
        [SerializeField] private TextMeshProUGUI CurrentQuestStep;

        [Header("Day Period Icons")]
        [SerializeField] private Image DayPeriodIcon;
        [SerializeField] private Sprite DawnSprite;
        [SerializeField] private Sprite MorningSprite;
        [SerializeField] private Sprite AfternoonSprite;
        [SerializeField] private Sprite EveningSprite;
        [SerializeField] private Sprite NightSprite;

        [Header("Heart UI")]
        [SerializeField] private Transform HeartParent;
        [SerializeField] private GameObject HeartPrefab;

        [Header("Currency UI")]
        [SerializeField] private TextMeshProUGUI CurrencyText;

        [Header("Number Scroll Settings")]
        [SerializeField] private float NumberScrollSpeed = 75f;

        private readonly List<UI_HeartSlot> heartSlots = new();

        private CharacterHealth playerHealth;
        private bool subscribedToHealth;

        private Quest trackedQuest;

        private int lastDisplayedHearts = -1;
        private int lastMaxHearts = -1;

        private float displayedCurrency;
        private float targetCurrency;

        private float displayedDay;
        private float targetDay;

        private void Start()
        {
            CachePlayerHealth();
            CacheTrackedQuest();
            UpdateAllUIImmediate();
        }

        private void OnEnable()
        {
            AddEventHandlers();

            playerHealth = null;
            subscribedToHealth = false;

            CachePlayerHealth();
            CacheTrackedQuest();
            UpdateAllUIImmediate();
        }

        private void OnDisable()
        {
            RemoveEventHandlers();
            UnsubscribeFromHealth();
        }

        private void Update()
        {
            ScrollCurrencyUI();
            ScrollDayUI();
        }

        private void CachePlayerHealth()
        {
            if (Player.Instance == null)
                return;

            CharacterHealth foundHealth =
                Player.Instance.GetComponent<CharacterHealth>();

            if (foundHealth == null)
                return;

            if (playerHealth != null &&
                playerHealth != foundHealth)
            {
                UnsubscribeFromHealth();
            }

            playerHealth = foundHealth;

            if (!subscribedToHealth)
            {
                playerHealth.OnHealthChanged += UpdateHeartUI;
                subscribedToHealth = true;
            }
        }

        private void UnsubscribeFromHealth()
        {
            if (playerHealth != null && subscribedToHealth)
                playerHealth.OnHealthChanged -= UpdateHeartUI;

            subscribedToHealth = false;
        }

        private void CacheTrackedQuest()
        {
            if (QuestManager.Instance == null)
            {
                RefreshTrackedQuestUI();
                return;
            }

            trackedQuest = QuestManager.Instance.TrackedQuest;
            RefreshTrackedQuestUI();
        }

        private void AddEventHandlers()
        {
            Bus<TimeChangedEvent>.OnEvent += UpdateTimeUI;
            Bus<DayStartedEvent>.OnEvent += UpdateDayUI;
            Bus<DayChangedEvent>.OnEvent += UpdateDayUI;
            Bus<DayPeriodChangedEvent>.OnEvent += UpdateDayPeriodUI;
            Bus<CurrencyUpdatedEvent>.OnEvent += HandleCurrencyUpdate;

            Bus<QuestStepStateChangedEvent>.OnEvent += UpdateQuestStepUI;
            Bus<TrackedQuestChangedEvent>.OnEvent += HandleTrackedQuestChanged;
            Bus<QuestStepAdvancedEvent>.OnEvent += UpdateQuestStepUI;
        }

        private void RemoveEventHandlers()
        {
            Bus<TimeChangedEvent>.OnEvent -= UpdateTimeUI;
            Bus<DayStartedEvent>.OnEvent -= UpdateDayUI;
            Bus<DayChangedEvent>.OnEvent -= UpdateDayUI;
            Bus<DayPeriodChangedEvent>.OnEvent -= UpdateDayPeriodUI;
            Bus<CurrencyUpdatedEvent>.OnEvent -= HandleCurrencyUpdate;

            Bus<QuestStepStateChangedEvent>.OnEvent -= UpdateQuestStepUI;
            Bus<TrackedQuestChangedEvent>.OnEvent -= HandleTrackedQuestChanged;
            Bus<QuestStepAdvancedEvent>.OnEvent -= UpdateQuestStepUI;
        }

        private void UpdateAllUIImmediate()
        {
            UpdateTimeUI();

            if (TimeManger.Instance != null)
            {
                SetDayImmediate(TimeManger.Instance.CurrentDay);
                UpdateDayPeriodUI(new DayPeriodChangedEvent(TimeManger.Instance.CurrentDayPeriod));
            }

            if (PlayerManager.Instance != null)
                SetCurrencyImmediate(PlayerManager.Instance.Currency);

            RebuildHeartUI();
            UpdateHeartUIImmediate();

            RefreshTrackedQuestUI();
        }

        #region Heart UI

        private void RebuildHeartUI()
        {
            if (HeartParent == null || HeartPrefab == null || playerHealth == null)
                return;

            ClearHeartUI();

            for (int i = 0; i < playerHealth.MaxHearts; i++)
            {
                GameObject heartObject = Instantiate(HeartPrefab, HeartParent);

                UI_HeartSlot heartSlot = heartObject.GetComponent<UI_HeartSlot>();

                if (heartSlot == null)
                {
                    Debug.LogWarning("Heart prefab is missing UI_HeartSlot.", heartObject);
                    continue;
                }

                heartSlots.Add(heartSlot);
            }

            lastMaxHearts = playerHealth.MaxHearts;
            lastDisplayedHearts = -1;
        }

        private void UpdateHeartUIImmediate()
        {
            if (playerHealth == null)
                return;

            if (heartSlots.Count != playerHealth.MaxHearts ||
                lastMaxHearts != playerHealth.MaxHearts)
            {
                RebuildHeartUI();
            }

            for (int i = 0; i < heartSlots.Count; i++)
            {
                if (heartSlots[i] == null)
                    continue;

                bool shouldBeFilled = i < playerHealth.CurrentHearts;

                if (shouldBeFilled)
                    heartSlots[i].Show(false);
                else
                    heartSlots[i].Hide(false);
            }

            lastDisplayedHearts = playerHealth.CurrentHearts;
            lastMaxHearts = playerHealth.MaxHearts;
        }

        private void UpdateHeartUI()
        {
            if (playerHealth == null)
                return;

            if (heartSlots.Count != playerHealth.MaxHearts ||
                lastMaxHearts != playerHealth.MaxHearts)
            {
                RebuildHeartUI();
                UpdateHeartUIImmediate();
                return;
            }

            bool lostHealth =
                lastDisplayedHearts >= 0 &&
                playerHealth.CurrentHearts < lastDisplayedHearts;

            bool gainedHealth =
                lastDisplayedHearts >= 0 &&
                playerHealth.CurrentHearts > lastDisplayedHearts;

            for (int i = 0; i < heartSlots.Count; i++)
            {
                if (heartSlots[i] == null)
                    continue;

                bool shouldBeFilled = i < playerHealth.CurrentHearts;

                bool wasFilled =
                    lastDisplayedHearts < 0 ||
                    i < lastDisplayedHearts;

                if (shouldBeFilled)
                {
                    bool animateIn = gainedHealth && !wasFilled;
                    heartSlots[i].Show(animateIn);
                }
                else
                {
                    bool animateOut =
                        lostHealth &&
                        wasFilled &&
                        i >= playerHealth.CurrentHearts;

                    heartSlots[i].Hide(animateOut);
                }
            }

            lastDisplayedHearts = playerHealth.CurrentHearts;
            lastMaxHearts = playerHealth.MaxHearts;
        }

        private void ClearHeartUI()
        {
            heartSlots.Clear();

            if (HeartParent == null)
                return;

            for (int i = HeartParent.childCount - 1; i >= 0; i--)
            {
                GameObject child = HeartParent.GetChild(i).gameObject;

                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        #endregion

        #region Quest UI

        private void HandleTrackedQuestChanged(TrackedQuestChangedEvent evt)
        {
            trackedQuest = evt.Quest;
            RefreshTrackedQuestUI();
        }

        private void UpdateQuestStepUI(QuestStepAdvancedEvent evt)
        {
            if (trackedQuest == null)
                return;

            if (evt.Quest != trackedQuest)
                return;

            RefreshTrackedQuestUI();
        }

        private void UpdateQuestStepUI(QuestStepStateChangedEvent evt)
        {
            if (trackedQuest == null)
                return;

            if (evt.ID != trackedQuest.Info.ID)
                return;

            RefreshTrackedQuestUI();
        }

        private void RefreshTrackedQuestUI()
        {
            if (CurrentQuestStep == null)
                return;

            if (trackedQuest == null)
            {
                CurrentQuestStep.text = "";
                return;
            }

            if (trackedQuest.State == QuestState.CAN_FINISH)
            {
                CurrentQuestStep.text =
                    $"{trackedQuest.Info.DisplayName}\nReturn to quest giver";
                return;
            }

            if (trackedQuest.State != QuestState.IN_PROGRESS)
            {
                CurrentQuestStep.text = "";
                return;
            }

            CurrentQuestStep.text =
                $"{trackedQuest.Info.DisplayName}\n" +
                trackedQuest.GetCurrentStepStatusText();
        }

        #endregion

        #region Time UI

        private void UpdateTimeUI(TimeChangedEvent evt)
        {
            UpdateTimeUI();
        }

        private void UpdateTimeUI()
        {
            if (TimeText != null && TimeManger.Instance != null)
                TimeText.text = TimeManger.Instance.FormattedTime;
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

        #endregion

        #region Currency UI

        private void HandleCurrencyUpdate(CurrencyUpdatedEvent evt)
        {
            if (PlayerManager.Instance != null)
                targetCurrency = PlayerManager.Instance.Currency;
        }

        private void ScrollCurrencyUI()
        {
            if (CurrencyText == null)
                return;

            displayedCurrency = ScrollNumber(displayedCurrency, targetCurrency);
            CurrencyText.text = Mathf.RoundToInt(displayedCurrency).ToString();
        }

        #endregion

        #region Day UI

        private void ScrollDayUI()
        {
            if (DayText == null)
                return;

            displayedDay = ScrollNumber(displayedDay, targetDay);
            DayText.text = "Day " + Mathf.RoundToInt(displayedDay);
        }

        #endregion

        #region Helpers

        private float ScrollNumber(float current, float target)
        {
            if (Mathf.Approximately(current, target))
                return target;

            return Mathf.MoveTowards(
                current,
                target,
                NumberScrollSpeed * Time.unscaledDeltaTime);
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

        #endregion
    }
}