using System;
using ShiftedSignal.Garden.Misc;
using UnityEngine;

namespace ShiftedSignal.Garden.Managers
{
    public enum DayPeriod
    {
        Dawn,
        Morning,
        Afternoon,
        Evening,
        Night
    }

    public class TimeManger : Singleton<TimeManger>
    {
        [Header("Current Time")]
        [SerializeField] private float currentTime = 8f;
        [SerializeField] private int currentDay = 1;

        [Header("Day/Night Times")]
        [SerializeField] private float dayStartHour = 8f;
        [SerializeField] private float nightStartHour = 20f;

        [Header("Time Speeds")]
        [SerializeField] private float daySecondsPerHour = 90f;
        [SerializeField] private float nightSecondsPerHour = 22.5f;

        private int lastHour = -1;
        private int lastMinute = -1;

        private bool wasDay;


    #region Actions
        public event Action<int> OnHourChanged;
        public event Action OnTimeChanged;
        public event Action OnDayStarted;
        public event Action OnNightStarted;
        public event Action<int> OnDayChanged;
        public event Action<DayPeriod> OnDayPeriodChanged;
    #endregion

    #region Getters
        public DayPeriod CurrentDayPeriod { get; private set; }
        public float CurrentTime => currentTime;
        public int CurrentDay => currentDay;
        public float DayStartHour => dayStartHour;
        public float NightStartHour => nightStartHour;
        public int CurrentHour => Mathf.FloorToInt(currentTime);
        public int CurrentMinute
        {
            get
            {
                float fractionalHour = currentTime - Mathf.Floor(currentTime);
                return Mathf.FloorToInt(fractionalHour * 60f);
            }
        }

        public bool IsDay => currentTime >= dayStartHour && currentTime < nightStartHour;

        public bool IsNight => !IsDay;

        public string FormattedTime => GetFormattedTime();

        #endregion

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            lastHour = CurrentHour;
            lastMinute = CurrentMinute;
            wasDay = IsDay;
            CurrentDayPeriod = GetDayPeriod();
        }

        private void Update()
        {
            AdvanceTime();
            CheckTimeEvents();
            // Debug.Log(CurrentDay);
        }

        public void CheckTimeEvents()
        {
            int currentHourInt = CurrentHour;
            int currentMinuteInt = CurrentMinute;

            if (currentMinuteInt != lastMinute)
            {
                int lastFive = lastMinute / 5;
                int currentFive = CurrentMinute / 5;

                if (currentFive != lastFive)
                {
                    OnTimeChanged?.Invoke();
                }

                lastMinute = currentMinuteInt;
            }

            if (currentHourInt != lastHour)
            {
                lastHour = currentHourInt;
                OnHourChanged?.Invoke(currentHourInt);
            }

            DayPeriod newDayPeriod = GetDayPeriod();
            if (CurrentDayPeriod != newDayPeriod)
            {
                CurrentDayPeriod = newDayPeriod;
                OnDayPeriodChanged?.Invoke(CurrentDayPeriod);
            }

            bool isCurrentlyDay = IsDay;
            if (isCurrentlyDay != wasDay)
            {
                if (isCurrentlyDay)
                {
                    OnDayStarted?.Invoke();
                }
                else
                {
                    OnNightStarted?.Invoke();
                }

                wasDay = isCurrentlyDay;
            }
        }

        private void AdvanceTime()
        {
            float secondsPerHour = IsDay ? daySecondsPerHour : nightSecondsPerHour;
            currentTime += Time.deltaTime / secondsPerHour;

            if (currentTime >= 24f)
            {
                currentTime -=24f;
                currentDay++;
                OnDayChanged?.Invoke(currentDay);
            }
        }

        public void SetTime(float newTime)
        {
            currentTime = Mathf.Repeat(newTime, 24f);
            lastHour = CurrentHour;
            lastMinute = CurrentMinute;
            wasDay = IsDay;
            CurrentDayPeriod = GetDayPeriod();
            OnTimeChanged?.Invoke();
        }

        [ContextMenu("Sleep")]
        public void Sleep()
        {
            currentDay++;
            SetTime(8);
        }

        public void AddHours(float hours)
        {
            currentTime = Mathf.Repeat(currentTime + hours, 24f);
        }

        public void AddMinutes(float minutes)
        {
            AddHours(minutes / 60f);
        }

        public float GetSecondPerHour()
        {
            return IsDay ? daySecondsPerHour : nightSecondsPerHour;
        }

        public float GetNormalizedTime()
        {
            return currentTime / 24f;
        }

        public float GetTimeUntilHour(float targetHour)
        {
            float difference = targetHour -  currentTime;

            if (difference < 0f)
            {
                difference += 24f;
            }

            return difference;
        }

        private DayPeriod GetDayPeriod()
        {
            if (IsWithinTimeRange(5f, 8f))
                return DayPeriod.Dawn;

            if (IsWithinTimeRange(8f, 12f))
                return DayPeriod.Morning;
                
            if (IsWithinTimeRange(12f, 17f))
                return DayPeriod.Afternoon;
            
            if (IsWithinTimeRange(17f, 20f))
                return DayPeriod.Evening;
            
            return DayPeriod.Night;


        }

        private bool IsWithinTimeRange(float startHour, float endHour)
        {
            if (startHour <= endHour)
            {
                return currentTime >= startHour && currentTime < endHour;
            }

            return currentTime >= startHour || currentTime < endHour;
        }

        private string GetFormattedTime()
        {
            int hour24 = CurrentHour;
            int minute = CurrentMinute;

            string amPm = hour24 >= 12 ? "PM" : "AM";

            int hour12 = hour24 % 12;
            if (hour12 == 0)
            {
                hour12 = 12;
            }

            return $"{hour12}:{minute:00} {amPm}";
        }
    }
}
