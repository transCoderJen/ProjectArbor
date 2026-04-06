using UnityEngine;

public class Wave : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private float WaveSpeed = 1f;
    [SerializeField] private float WaveAmount = 5f;

    private Quaternion startRotation;
    private float waveOffset;

    private void Start()
    {
        startRotation = transform.rotation;

        // Randomize the wave start point
        waveOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        WaveMotion();
    }

    private void WaveMotion()
    {
        float angle = Mathf.Sin(Time.time * WaveSpeed + waveOffset) * WaveAmount;
        transform.rotation = startRotation * Quaternion.Euler(0f, 0f, angle);
    }
}