using UnityEngine;

[CreateAssetMenu(menuName = "AI/Navigation Config")]
public class NavigationConfigSO : ScriptableObject
{
    public float SampleRadius = 2f;

    public float MinVelocityBeforeStopping = 0.15f;

    public float StuckDuration = 1.5f;
}