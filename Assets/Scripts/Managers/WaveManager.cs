using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Effects
{
    public class WaveManager : MonoBehaviour
    {
        [SerializeField, Min(0.01f)]
        private float tickInterval = 0.1f;

        private static readonly List<Wave> activeWaves = new();

        private Coroutine tickCoroutine;
        private WaitForSeconds tickDelay;

        private void Awake()
        {
            tickDelay = new WaitForSeconds(tickInterval);
        }

        private void OnEnable()
        {
            Bus<NightStartedEvent>.OnEvent += HandleNightStarted;
            Bus<DayStartedEvent>.OnEvent += HandleDayStarted;
        }

        private void OnDisable()
        {
            Bus<NightStartedEvent>.OnEvent -= HandleNightStarted;
            Bus<DayStartedEvent>.OnEvent -= HandleDayStarted;

            StopTicking();
        }

        private void Start()
        {
            if (TimeManger.Instance.IsNight)
                StartTicking();
        }

        private void HandleNightStarted(NightStartedEvent nightStartedEvent)
        {
            StartTicking();
        }

        private void HandleDayStarted(DayStartedEvent dayStartedEvent)
        {
            StopTicking();
        }

        private void StartTicking()
        {
            if (tickCoroutine != null)
                return;

            tickCoroutine = StartCoroutine(TickWaves());
        }

        private void StopTicking()
        {
            if (tickCoroutine == null)
                return;

            StopCoroutine(tickCoroutine);
            tickCoroutine = null;
        }

        private IEnumerator TickWaves()
        {
            while (true)
            {
                ApplyWaves();
                yield return tickDelay;
            }
        }

        private static void ApplyWaves()
        {
            float time = Time.time;

            for (int i = activeWaves.Count - 1; i >= 0; i--)
            {
                Wave wave = activeWaves[i];

                if (wave == null)
                {
                    activeWaves.RemoveAt(i);
                    continue;
                }

                if (!wave.IsVisible)
                    continue;

                wave.ApplyWave(time);
            }
        }

        public static void Register(Wave wave)
        {
            if (wave == null || activeWaves.Contains(wave))
                return;

            activeWaves.Add(wave);
        }

        public static void Unregister(Wave wave)
        {
            if (wave == null)
                return;

            activeWaves.Remove(wave);
        }
    }
}