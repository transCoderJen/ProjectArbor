using System;
using ShiftedSignal.Garden.Misc;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Header("Fill Light")]
        [SerializeField] private Light fillLight;
        [SerializeField] private Color fillLightColor = new Color(0.45f, 0.55f, 0.75f);
        [SerializeField] private float fillLightIntensity = 0.15f;

        [Header("Sun Light")]
        [SerializeField] private Light sun;
        [SerializeField] private Color dayColor = Color.white;
        [SerializeField] private Color dawnColor = new Color(1f, 0.6f, 0.4f);
        [SerializeField] private Color nightColor = new Color(0.2f, 0.35f, 0.6f);

        [Header("Moon Light")]
        [SerializeField] private Light moon;
        [SerializeField] private Color moonColor = new Color(0.35f, 0.45f, 0.75f);
        [SerializeField] private float moonIntensity = 0.2f;

        [Header("Color Temperature Kelvin")]
        [SerializeField] private float dayTemperature = 5500f;
        [SerializeField] private float dawnTemperature = 3000f;
        [SerializeField] private float nightTemperature = 2000f;

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

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindLights();

            UpdateLighting();
        }

        private void FindLights()
        {
            if (sun == null)
            {
                GameObject sunObject = GameObject.FindWithTag("Sun");

                if (sunObject != null)
                {
                    sun = sunObject.GetComponent<Light>();
                }
            }

            if (moon == null)
            {
                GameObject moonObject = GameObject.FindWithTag("Moon");

                if (moonObject != null)
                {
                    moon = moonObject.GetComponent<Light>();
                }
            }

            if (fillLight == null)
            {
                GameObject fillObject = GameObject.FindWithTag("FillLight");

                if (fillObject != null)
                {
                    fillLight = fillObject.GetComponent<Light>();
                }
            }
        }

        private void Start()
        {
            lastHour = CurrentHour;
            lastMinute = CurrentMinute;
            wasDay = IsDay;
            CurrentDayPeriod = GetDayPeriod();

            UpdateLighting();
        }

        private void Update()
        {
            AdvanceTime();
            CheckTimeEvents();
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

            UpdateLighting();

            if (currentTime >= 24f)
            {
                currentTime -= 24f;
                currentDay++;
                OnDayChanged?.Invoke(currentDay);
            }
        }

        private void UpdateLighting()
        {
            float sunIntensity;
            float currentMoonIntensity;

            Color targetColor = dayColor;
            float targetTemp = dayTemperature;

            if (IsWithinTimeRange(5f, 8f)) // Dawn
            {
                float t = (currentTime - 5f) / 3f;

                sunIntensity = Mathf.Lerp(0.15f, 1f, t);
                currentMoonIntensity = Mathf.Lerp(moonIntensity, 0f, t);

                targetColor = Color.Lerp(dawnColor, dayColor, t);
                targetTemp = Mathf.Lerp(dawnTemperature, dayTemperature, t);
            }
            else if (IsWithinTimeRange(8f, 20f)) // Day
            {
                sunIntensity = 1f;
                currentMoonIntensity = 0f;

                targetColor = dayColor;
                targetTemp = dayTemperature;
            }
            else if (IsWithinTimeRange(20f, 23f)) // Evening
            {
                float t = (currentTime - 20f) / 3f;

                sunIntensity = Mathf.Lerp(1f, 0f, t);
                currentMoonIntensity = Mathf.Lerp(0f, moonIntensity, t);

                targetColor = Color.Lerp(dayColor, dawnColor, t);
                targetTemp = Mathf.Lerp(dayTemperature, dawnTemperature, t);
            }
            else // Night
            {
                sunIntensity = 0f;
                currentMoonIntensity = moonIntensity;

                targetColor = nightColor;
                targetTemp = nightTemperature;
            }

            // Prevent total darkness
            sunIntensity = Mathf.Max(sunIntensity, 0.02f);
            currentMoonIntensity = Mathf.Max(currentMoonIntensity, 0.02f);

            UpdateCelestialRotation();

            // Sun
            if (sun != null)
            {
                sun.intensity = sunIntensity;
                sun.color = targetColor;

        #if UNITY_2019_1_OR_NEWER
                sun.colorTemperature = targetTemp;
                sun.useColorTemperature = true;
        #endif
            }

            // Moon
            if (moon != null)
            {
                moon.intensity = currentMoonIntensity;
                moon.color = moonColor;
            }

            // Fill Light
            if (fillLight != null)
            {
                fillLight.intensity = fillLightIntensity;
                fillLight.color = fillLightColor;

                fillLight.shadows = LightShadows.None;

                // Always point toward camera/front
                fillLight.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            }

            // Ambient Lighting
            if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat)
            {
                float ambientLerp = Mathf.InverseLerp(0f, 1f, sunIntensity);

                RenderSettings.ambientLight = Color.Lerp(
                    new Color(0.08f, 0.1f, 0.15f),
                    Color.white,
                    ambientLerp
                );
            }
        }

        private void UpdateCelestialRotation()
        {
            float normalizedTime = currentTime / 24f;

            float sunAngle = normalizedTime * 360f - 90f;
            float moonAngle = sunAngle + 180f;

            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
            }

            if (moon != null)
            {
                moon.transform.rotation = Quaternion.Euler(moonAngle, 170f, 0f);
            }
        }

        public void SetTime(float newTime)
        {
            currentTime = Mathf.Repeat(newTime, 24f);
            lastHour = CurrentHour;
            lastMinute = CurrentMinute;
            wasDay = IsDay;
            CurrentDayPeriod = GetDayPeriod();

            UpdateLighting();
            OnTimeChanged?.Invoke();
        }

        [ContextMenu("Sleep")]
        public void Sleep()
        {
            currentDay++;
            SetTime(8f);
        }

        public void AddHours(float hours)
        {
            currentTime = Mathf.Repeat(currentTime + hours, 24f);
            UpdateLighting();
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
            float difference = targetHour - currentTime;

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