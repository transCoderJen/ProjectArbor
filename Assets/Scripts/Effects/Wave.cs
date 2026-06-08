using UnityEngine;

namespace ShiftedSignal.Garden.Effects
{
    public class Wave : MonoBehaviour
    {
        [Header("Wave Settings")]
        [SerializeField] private float WaveSpeed = 1f;
        [SerializeField] private float WaveAmount = 5f;

        private Quaternion startRotation;
        private float waveOffset;
        private bool isVisible;

        public bool IsVisible => isVisible;
        public float Speed => WaveSpeed;
        public float Amount => WaveAmount;
        public float Offset => waveOffset;

        private void Awake()
        {
            startRotation = transform.rotation;
            waveOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            WaveManager.Register(this);
        }

        private void OnDisable()
        {
            WaveManager.Unregister(this);
        }

        private void OnBecameVisible()
        {
            isVisible = true;
        }

        private void OnBecameInvisible()
        {
            isVisible = false;
        }

        public void ApplyWave(float time)
        {
            float angle = Mathf.Sin(time * WaveSpeed + waveOffset) * WaveAmount;
            transform.rotation = startRotation * Quaternion.Euler(0f, 0f, angle);
        }

        public void ResetWave()
        {
            transform.rotation = startRotation;
        }
    }
}