using System;
using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Misc;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ShiftedSignal.Garden.Managers
{
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("BGM")]
        [SerializeField] private AudioSource[] bgm;
        [SerializeField] private float bgmCrossFadeDuration = 2f;
        [SerializeField] private bool playBGM = true;

        [Tooltip("If enabled, music continues while the game window is unfocused.")]
        [SerializeField] private bool continueMusicWhenWindowInactive = true;

        [Header("SFX Pool")]
        [SerializeField] private int sfxPoolSize = 20;
        [SerializeField] private Transform sfxPoolParent;

        [Header("Default SFX Settings")]
        [SerializeField] private float defaultSfxVolume = 1f;
        [SerializeField] private Vector2 pitchRange = new Vector2(.85f, 1.1f);

        [Header("Default 3D Settings")]
        [SerializeField] private float min3DDistance = 2f;
        [SerializeField] private float max3DDistance = 25f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

        [Header("Audio Clips")]
        [SerializeField] AudioClip[] RoosterClips;

        private readonly List<AudioSource> sfxPool = new();

        private int bgmIndex;

        private bool bgmPausedByFocus;
        private Coroutine bgmFadeRoutine;

        #region === Unity ===

        protected override void Awake()
        {
            base.Awake();

            Application.runInBackground = continueMusicWhenWindowInactive;

            CreateSFXPool();

            if (playBGM && bgm.Length > 0)
                PlayBGM(0);
        }

        private void OnEnable()
        {
            Bus<NightStartedEvent>.OnEvent += StartNightMusic;
            Bus<DayStartedEvent>.OnEvent += StartDayMusic;
        }

        private void OnDisable()
        {
            Bus<NightStartedEvent>.OnEvent -= StartNightMusic;
            Bus<DayStartedEvent>.OnEvent -= StartDayMusic;
        }

        // day music indexes: 
        // 0
        // 1
        private void StartDayMusic(DayStartedEvent evt)
        {
            int randomDayBGMIndex = Random.Range(0, 2);
            CrossFadeBGM(randomDayBGMIndex, 5); 
            PlaySFX2D(RoosterClips[Random.Range(0, RoosterClips.Length)], .7f);
        }  

        // night music indexes
        // 2
        private void StartNightMusic(NightStartedEvent evt)
        {
            Debug.Log("Crossfading into night");
            CrossFadeBGM(2, 5);
        }

        private void Update()
        {
            if (!playBGM)
            {
                StopAllBGM();
                return;
            }

            if (bgmPausedByFocus)
                return;

            if (bgm == null || bgm.Length == 0)
                return;

            if (!bgm[bgmIndex].isPlaying)
                PlayRandomBGM();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            HandleApplicationFocusState(!pauseStatus);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            HandleApplicationFocusState(hasFocus);
        }

        private void HandleApplicationFocusState(bool hasFocus)
        {
            if (continueMusicWhenWindowInactive)
            {
                Application.runInBackground = true;
                return;
            }

            if (!playBGM || bgm == null || bgm.Length == 0)
                return;

            AudioSource currentBgm = bgm[bgmIndex];

            if (!hasFocus)
            {
                if (currentBgm.isPlaying)
                {
                    currentBgm.Pause();
                    bgmPausedByFocus = true;
                }
            }
            else
            {
                if (bgmPausedByFocus)
                {
                    currentBgm.UnPause();
                    bgmPausedByFocus = false;
                }
            }
        }

        #endregion

        #region === SFX ===

        public void PlaySFX2D(AudioClip clip, float volume = -1f)
        {
            if (clip == null)
                return;

            AudioSource source = GetAvailableSFXSource();

            SetupSFXSource(source, clip, volume, 0f);

            source.transform.position = transform.position;
            source.Play();

            StartCoroutine(ReturnSFXSourceAfterPlaying(source));
        }

        public void PlaySFX3D(
            AudioClip clip,
            Vector3 position,
            float volume = -1f,
            float minDistance = -1f,
            float maxDistance = -1f,
            AudioRolloffMode? customRolloffMode = null)
        {
            if (clip == null)
                return;

            AudioSource source = GetAvailableSFXSource();

            SetupSFXSource(source, clip, volume, 1f);

            source.transform.position = position;
            source.minDistance = minDistance > 0f ? minDistance : min3DDistance;
            source.maxDistance = maxDistance > 0f ? maxDistance : max3DDistance;
            source.rolloffMode = customRolloffMode ?? rolloffMode;

            source.Play();

            StartCoroutine(ReturnSFXSourceAfterPlaying(source));
        }

        public void PlaySFX3D(
            AudioClip clip,
            Transform sourceTransform,
            float volume = -1f,
            float minDistance = -1f,
            float maxDistance = -1f,
            AudioRolloffMode? customRolloffMode = null)
        {
            if (sourceTransform == null)
                return;

            PlaySFX3D(
                clip,
                sourceTransform.position,
                volume,
                minDistance,
                maxDistance,
                customRolloffMode);
        }

        private void CreateSFXPool()
        {
            if (sfxPoolParent == null)
                sfxPoolParent = transform;

            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject sourceObject = new GameObject($"SFX Source {i}");

                sourceObject.transform.SetParent(sfxPoolParent);

                AudioSource source = sourceObject.AddComponent<AudioSource>();

                source.playOnAwake = false;

                sfxPool.Add(source);
            }
        }

        private AudioSource GetAvailableSFXSource()
        {
            for (int i = 0; i < sfxPool.Count; i++)
            {
                if (!sfxPool[i].isPlaying)
                    return sfxPool[i];
            }

            GameObject sourceObject = new GameObject($"SFX Source {sfxPool.Count}");

            sourceObject.transform.SetParent(sfxPoolParent);

            AudioSource source = sourceObject.AddComponent<AudioSource>();

            source.playOnAwake = false;

            sfxPool.Add(source);

            return source;
        }

        private void SetupSFXSource(AudioSource source, AudioClip clip, float volume, float spatialBlend)
        {
            source.clip = clip;

            source.volume = volume < 0f
                ? defaultSfxVolume
                : volume;

            source.pitch = Random.Range(pitchRange.x, pitchRange.y);

            source.spatialBlend = spatialBlend;

            source.loop = false;
        }

        private IEnumerator ReturnSFXSourceAfterPlaying(AudioSource source)
        {
            if (source == null || source.clip == null)
                yield break;

            yield return new WaitForSeconds(source.clip.length / Mathf.Abs(source.pitch));

            source.Stop();

            source.clip = null;
            source.pitch = 1f;
            source.spatialBlend = 0f;
        }

        #endregion

        #region === BGM ===

        public void PlayBGM(int index)
        {
            if (bgm == null || bgm.Length == 0)
                return;

            if (index < 0 || index >= bgm.Length)
                return;

            bgmIndex = index;

            StopAllBGM();

            bgm[bgmIndex].Play();
        }

        public void PlayNextBGM()
        {
            if (bgm == null || bgm.Length == 0)
                return;

            bgmIndex++;

            if (bgmIndex >= bgm.Length)
                bgmIndex = 0;

            PlayBGM(bgmIndex);
        }

        public void PlayRandomBGM()
        {
            if (bgm == null || bgm.Length == 0)
                return;

            bgmIndex = Random.Range(0, bgm.Length);

            PlayBGM(bgmIndex);
        }

        public void CrossFadeBGM(int newBgmIndex, float duration = -1f)
        {
            if (bgm == null || bgm.Length == 0)
                return;

            if (newBgmIndex < 0 || newBgmIndex >= bgm.Length)
                return;

            if (newBgmIndex == bgmIndex && bgm[newBgmIndex].isPlaying)
                return;

            if (duration <= 0f)
                duration = bgmCrossFadeDuration;

            if (bgmFadeRoutine != null)
                StopCoroutine(bgmFadeRoutine);

            bgmFadeRoutine = StartCoroutine(
                CrossFadeBGMRoutine(newBgmIndex, duration));
        }

        private IEnumerator CrossFadeBGMRoutine(int newBgmIndex, float duration)
        {
            AudioSource oldBgm = bgm[bgmIndex];
            AudioSource newBgm = bgm[newBgmIndex];

            float oldStartVolume = oldBgm.volume;
            float newTargetVolume = newBgm.volume;

            bgmIndex = newBgmIndex;

            newBgm.volume = 0f;
            newBgm.Play();

            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;

                float t = timer / duration;

                if (oldBgm != null)
                    oldBgm.volume = Mathf.Lerp(oldStartVolume, 0f, t);

                if (newBgm != null)
                    newBgm.volume = Mathf.Lerp(0f, newTargetVolume, t);

                yield return null;
            }

            if (oldBgm != null)
            {
                oldBgm.Stop();
                oldBgm.volume = oldStartVolume;
            }

            if (newBgm != null)
                newBgm.volume = newTargetVolume;

            bgmFadeRoutine = null;
        }

        public void StopAllBGM()
        {
            if (bgm == null)
                return;

            for (int i = 0; i < bgm.Length; i++)
            {
                if (bgm[i] == null)
                    continue;

                bgm[i].Stop();
            }
        }

        #endregion
    }
}