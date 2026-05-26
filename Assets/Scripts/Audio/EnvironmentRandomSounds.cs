using System;
using System.Collections;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.SaveAndLoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ShiftedSignal.Garden.Audio
{
    [Serializable]
    public class RandomClip
    {
        public AudioClip Clip;
        public float MinPlayTime = 3f;
        public float MaxPlayTime = 8f;
        public float Volume = 1f;
    }

    [RequireComponent(typeof(Collider))]
    public class EnvironmentRandomSounds : MonoBehaviour, ISaveManager
    {
        [Header("Random Anytime SFX")]
        [SerializeField] private AudioClip[] ClipsForPopulatingAnytime;
        [SerializeField] private RandomClip[] RandomAnyTimeSFX;

        [Header("Random Day SFX")]
        [SerializeField] private AudioClip[] ClipsForPopulatingDay;
        [SerializeField] private RandomClip[] RandomDaySFX;

        [Header("Random Night SFX")]
        [SerializeField] private AudioClip[] ClipsForPopulatingNight;
        [SerializeField] private RandomClip[] RandomNightSFX;

        [Header("3D Audio")]
        [SerializeField] private float Min3DDistance = 2f;
        [SerializeField] private float Max3DDistance = 25f;
        [SerializeField] private AudioRolloffMode RolloffMode = AudioRolloffMode.Linear;

        [Header("Debug")]
        [SerializeField] private bool DrawDebugPoint = true;

        private Collider boundsCollider;
        private bool playerInside;
        private Vector3 lastRandomPoint;

        private void Awake()
        {
            boundsCollider = GetComponent<Collider>();
            boundsCollider.isTrigger = true;
        }

        private void Start()
        {
            for (int i = 0; i < RandomAnyTimeSFX.Length; i++)
            {
                if (RandomAnyTimeSFX[i] == null || RandomAnyTimeSFX[i].Clip == null)
                    continue;

                StartCoroutine(PlayRandomSoundLoop(RandomAnyTimeSFX[i]));
            }
            for (int i = 0; i < RandomDaySFX.Length; i++)
            {
                if (RandomDaySFX[i] == null || RandomDaySFX[i].Clip == null)
                    continue;

                StartCoroutine(PlayRandomDaySoundLoop(RandomDaySFX[i]));
            }
            for (int i = 0; i < RandomNightSFX.Length; i++)
            {
                if (RandomNightSFX[i] == null || RandomNightSFX[i].Clip == null)
                    continue;

                StartCoroutine(PlayRandomNightSoundLoop(RandomNightSFX[i]));
            }
        }

        void OnEnable()
        {
            
        }

        void OnDisable()
        {
            
        }

        [ContextMenu("Populate Random SFX From Clips")]
        private void PopulateRandomSFXFromClips()
        {
            RandomAnyTimeSFX = PopulateRandomClipArray(ClipsForPopulatingAnytime, 24f, 70f, 0.6f);
            RandomDaySFX = PopulateRandomClipArray(ClipsForPopulatingDay, 24f, 70f, 0.6f);
            RandomNightSFX = PopulateRandomClipArray(ClipsForPopulatingNight, 24f, 70f, 0.6f);

            Debug.Log(
                $"Populated Anytime: {RandomAnyTimeSFX.Length}, " +
                $"Day: {RandomDaySFX.Length}, " +
                $"Night: {RandomNightSFX.Length} Random SFX entries.");
        }

        private RandomClip[] PopulateRandomClipArray(
            AudioClip[] clips,
            float minPlayTime,
            float maxPlayTime,
            float volume)
        {
            if (clips == null || clips.Length == 0)
                return Array.Empty<RandomClip>();

            RandomClip[] randomClips = new RandomClip[clips.Length];

            for (int i = 0; i < clips.Length; i++)
            {
                randomClips[i] = new RandomClip
                {
                    Clip = clips[i],
                    MinPlayTime = minPlayTime,
                    MaxPlayTime = maxPlayTime,
                    Volume = volume
                };
            }

            return randomClips;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Player>() != null)
            {
                playerInside = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<Player>() != null)
            {
                playerInside = false;
            }
        }

        private IEnumerator PlayRandomSoundLoop(RandomClip randomClip)
        {
            while (true)
            {
                float waitTime = Random.Range(
                    randomClip.MinPlayTime,
                    randomClip.MaxPlayTime);

                yield return new WaitForSeconds(waitTime);

                if (!playerInside)
                    continue;

                lastRandomPoint = GetRandomPointOnBoundsEdge();

                AudioManager.Instance.PlaySFX3D(
                    randomClip.Clip,
                    lastRandomPoint,
                    randomClip.Volume,
                    Min3DDistance,
                    Max3DDistance,
                    RolloffMode);
            }
        }

        private IEnumerator PlayRandomNightSoundLoop(RandomClip randomClip)
        {
            while (true)
            {
                float waitTime = Random.Range(
                    randomClip.MinPlayTime,
                    randomClip.MaxPlayTime);

                yield return new WaitForSeconds(waitTime);

                if (!playerInside)
                    continue;

                lastRandomPoint = GetRandomPointOnBoundsEdge();

                AudioManager.Instance.PlaySFX3D(
                    randomClip.Clip,
                    lastRandomPoint,
                    TimeManger.Instance.IsNight ? randomClip.Volume : 0,
                    Min3DDistance,
                    Max3DDistance,
                    RolloffMode);
            }
        }
        
        private IEnumerator PlayRandomDaySoundLoop(RandomClip randomClip)
        {
            while (true)
            {
                float waitTime = Random.Range(
                    randomClip.MinPlayTime,
                    randomClip.MaxPlayTime);

                yield return new WaitForSeconds(waitTime);

                if (!playerInside)
                    continue;

                lastRandomPoint = GetRandomPointOnBoundsEdge();

                AudioManager.Instance.PlaySFX3D(
                    randomClip.Clip,
                    lastRandomPoint,
                    TimeManger.Instance.IsDay ? randomClip.Volume : 0,
                    Min3DDistance,
                    Max3DDistance,
                    RolloffMode);
            }
        }

        private Vector3 GetRandomPointOnBoundsEdge()
        {
            if (boundsCollider == null)
                return transform.position;

            Bounds bounds = boundsCollider.bounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            float t = Random.value;
            int edgeIndex = Random.Range(0, 8);

            switch (edgeIndex)
            {
                case 0:
                    return Vector3.Lerp(new Vector3(min.x, min.y, min.z), new Vector3(min.x, max.y, min.z), t);
                case 1:
                    return Vector3.Lerp(new Vector3(min.x, min.y, max.z), new Vector3(min.x, max.y, max.z), t);
                case 2:
                    return Vector3.Lerp(new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), t);
                case 3:
                    return Vector3.Lerp(new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z), t);
                case 4:
                    return Vector3.Lerp(new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z), t);
                case 5:
                    return Vector3.Lerp(new Vector3(min.x, max.y, max.z), new Vector3(max.x, max.y, max.z), t);
                case 6:
                    return Vector3.Lerp(new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z), t);
                default:
                    return Vector3.Lerp(new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z), t);
            }
        }

        public void LoadData(GameData data)
        {
            if (boundsCollider == null) return;

            // Find out exactly where the player will be spawning.
            // If it's a new game (Vector3.zero), we fall back to the Player's default scene position.
            Vector3 spawnPos = data.playerPosition != Vector3.zero 
                ? data.playerPosition 
                : PlayerManager.Instance.Player.transform.position;

            // The ClosestPoint method returns the exact position you pass into it ONLY IF 
            // that position is already inside the collider. This is a perfect, lightweight bounds check!
            if (boundsCollider.ClosestPoint(spawnPos) == spawnPos)
            {
                playerInside = true;
            }
        }

        public void SaveData(ref GameData data)
        {
            // We leave this completely blank! 
            // We don't need to save anything; we are just using the interface 
            // so we can read the player's position during the LoadData phase.
        }
    }
}