using ShiftedSignal.Garden.Misc;
using UnityEngine;

public class WindManager : Singleton<WindManager>
{
    [SerializeField] private Terrain Terrain;

    [Range(0f, 2f)]
    [SerializeField] private float Strength = 0.4f;

    [Range(0f, 5f)]
    [SerializeField] private float Speed = 1f;

    public void SetWind(float strength, float speed)
    {
        Terrain.terrainData.wavingGrassStrength = strength;
        Terrain.terrainData.wavingGrassSpeed = speed;
    }
}