using System.Collections.Generic;
using UnityEngine;

namespace ShiftedSignal.Garden.Effects
{
    public class WaveManager : MonoBehaviour
    {
        private static readonly List<Wave> activeWaves = new();

        private void Update()
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
            if (wave == null)
                return;

            if (activeWaves.Contains(wave))
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