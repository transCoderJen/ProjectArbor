using UnityEngine;

namespace ShiftedSignal.Garden.TerrainGeneration
{
    public class PerlinTerrainGenerator : MonoBehaviour
    {
        [Header("Terrain")]
        [SerializeField] private Terrain Terrain;

        [Header("Mountain Ranges")]
        [SerializeField] private float MountainScale = 220f;
        [SerializeField] private float MountainDetailScale = 45f;
        [SerializeField] private float MountainHeight = 0.3f;

        [SerializeField, Range(0f, 1f)]
        private float MountainStart = 0.58f;

        [SerializeField] private float MountainSharpness = 2.5f;

        [Header("Border Flattening")]
        [SerializeField] private bool FlattenBorders = true;

        [Tooltip("How far inward from the terrain edge the height blends from 0 to full height, in world units.")]
        [SerializeField] private float BorderBlendDistance = 20f;

        [Header("Noise Detail")]
        [SerializeField] private int Octaves = 4;
        [SerializeField] private float Persistence = 0.5f;
        [SerializeField] private float Lacunarity = 2f;

        [Header("Offset")]
        [SerializeField] private Vector2 Offset;

        [ContextMenu("Generate Terrain")]
        public void GenerateTerrain()
        {
            if (Terrain == null)
            {
                Debug.LogWarning($"{nameof(PerlinTerrainGenerator)} needs a Terrain assigned.");
                return;
            }

            TerrainData terrainData = Terrain.terrainData;
            int resolution = terrainData.heightmapResolution;

            float[,] heights = new float[resolution, resolution];

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float worldX = Terrain.transform.position.x +
                                   ((float)x / (resolution - 1)) * terrainData.size.x;

                    float worldZ = Terrain.transform.position.z +
                                   ((float)z / (resolution - 1)) * terrainData.size.z;

                    float mountainMask = Mathf.PerlinNoise(
                        (worldX + Offset.x) / MountainScale,
                        (worldZ + Offset.y) / MountainScale
                    );

                    mountainMask = Mathf.InverseLerp(MountainStart, 1f, mountainMask);
                    mountainMask = Mathf.Clamp01(mountainMask);
                    mountainMask = Mathf.Pow(mountainMask, MountainSharpness);

                    float mountainDetail = FractalNoise(worldX, worldZ, MountainDetailScale);

                    float finalHeight = mountainMask * mountainDetail * MountainHeight;

                    if (FlattenBorders)
                    {
                        float borderMask = GetBorderMask(x, z, resolution, terrainData.size);
                        finalHeight *= borderMask;
                    }

                    heights[z, x] = finalHeight;
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        [ContextMenu("Clear Terrain")]
        public void ClearTerrain()
        {
            if (Terrain == null)
            {
                Debug.LogWarning($"{nameof(PerlinTerrainGenerator)} needs a Terrain assigned.");
                return;
            }

            TerrainData terrainData = Terrain.terrainData;
            int resolution = terrainData.heightmapResolution;

            float[,] heights = new float[resolution, resolution];
            terrainData.SetHeights(0, 0, heights);
        }

        private float GetBorderMask(int x, int z, int resolution, Vector3 terrainSize)
        {
            if (BorderBlendDistance <= 0f)
            {
                bool isBorder =
                    x == 0 ||
                    z == 0 ||
                    x == resolution - 1 ||
                    z == resolution - 1;

                return isBorder ? 0f : 1f;
            }

            float normalizedX = (float)x / (resolution - 1);
            float normalizedZ = (float)z / (resolution - 1);

            float worldX = normalizedX * terrainSize.x;
            float worldZ = normalizedZ * terrainSize.z;

            float distanceFromLeft = worldX;
            float distanceFromRight = terrainSize.x - worldX;
            float distanceFromBottom = worldZ;
            float distanceFromTop = terrainSize.z - worldZ;

            float closestEdgeDistance = Mathf.Min(
                distanceFromLeft,
                distanceFromRight,
                distanceFromBottom,
                distanceFromTop
            );

            float mask = Mathf.InverseLerp(0f, BorderBlendDistance, closestEdgeDistance);

            return Mathf.SmoothStep(0f, 1f, mask);
        }

        private float FractalNoise(float worldX, float worldZ, float scale)
        {
            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0f;
            float maxValue = 0f;

            for (int i = 0; i < Octaves; i++)
            {
                float sampleX = (worldX + Offset.x) / scale * frequency;
                float sampleZ = (worldZ + Offset.y) / scale * frequency;

                float perlin = Mathf.PerlinNoise(sampleX, sampleZ);

                noiseHeight += perlin * amplitude;
                maxValue += amplitude;

                amplitude *= Persistence;
                frequency *= Lacunarity;
            }

            return noiseHeight / maxValue;
        }

        private void OnValidate()
        {
            MountainScale = Mathf.Max(0.01f, MountainScale);
            MountainDetailScale = Mathf.Max(0.01f, MountainDetailScale);
            MountainHeight = Mathf.Max(0f, MountainHeight);

            MountainSharpness = Mathf.Max(0.01f, MountainSharpness);

            Octaves = Mathf.Max(1, Octaves);
            Persistence = Mathf.Clamp01(Persistence);
            Lacunarity = Mathf.Max(0.01f, Lacunarity);

            BorderBlendDistance = Mathf.Max(0f, BorderBlendDistance);
        }
    }
}