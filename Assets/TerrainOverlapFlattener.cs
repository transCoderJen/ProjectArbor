using UnityEngine;

namespace ShiftedSignal.Garden.TerrainGeneration
{
    public class TerrainOverlapFlattener : MonoBehaviour
    {
        [Header("Terrains")]
        [SerializeField] private Terrain FirstTerrain;
        [SerializeField] private Terrain SecondTerrain;

        [Header("Flattening")]
        [SerializeField, Range(0f, 1f)] private float TargetNormalizedHeight = 0f;

        [Tooltip("Extra world-unit padding around the overlap area.")]
        [SerializeField] private float Padding = 0f;

        [ContextMenu("Flatten Overlap")]
        public void FlattenOverlap()
        {
            if (FirstTerrain == null || SecondTerrain == null)
            {
                Debug.LogWarning($"{nameof(TerrainOverlapFlattener)} needs both terrains assigned.");
                return;
            }

            if (!TryGetOverlapBounds(FirstTerrain, SecondTerrain, out Bounds overlapBounds))
            {
                Debug.LogWarning("The terrains do not overlap.");
                return;
            }

            overlapBounds.Expand(new Vector3(Padding * 2f, 0f, Padding * 2f));

            FlattenTerrainArea(FirstTerrain, overlapBounds);
            FlattenTerrainArea(SecondTerrain, overlapBounds);

            FirstTerrain.Flush();
            SecondTerrain.Flush();

            Debug.Log("Terrain overlap flattened.");
        }

        private void FlattenTerrainArea(Terrain terrain, Bounds worldBounds)
        {
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPosition = terrain.transform.position;

            int resolution = terrainData.heightmapResolution;

            float normalizedMinX = Mathf.InverseLerp(
                terrainPosition.x,
                terrainPosition.x + terrainData.size.x,
                worldBounds.min.x
            );

            float normalizedMaxX = Mathf.InverseLerp(
                terrainPosition.x,
                terrainPosition.x + terrainData.size.x,
                worldBounds.max.x
            );

            float normalizedMinZ = Mathf.InverseLerp(
                terrainPosition.z,
                terrainPosition.z + terrainData.size.z,
                worldBounds.min.z
            );

            float normalizedMaxZ = Mathf.InverseLerp(
                terrainPosition.z,
                terrainPosition.z + terrainData.size.z,
                worldBounds.max.z
            );

            int startX = Mathf.Clamp(Mathf.FloorToInt(normalizedMinX * (resolution - 1)), 0, resolution - 1);
            int endX = Mathf.Clamp(Mathf.CeilToInt(normalizedMaxX * (resolution - 1)), 0, resolution - 1);

            int startZ = Mathf.Clamp(Mathf.FloorToInt(normalizedMinZ * (resolution - 1)), 0, resolution - 1);
            int endZ = Mathf.Clamp(Mathf.CeilToInt(normalizedMaxZ * (resolution - 1)), 0, resolution - 1);

            int width = endX - startX + 1;
            int height = endZ - startZ + 1;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            float[,] heights = terrainData.GetHeights(startX, startZ, width, height);

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    heights[z, x] = TargetNormalizedHeight;
                }
            }

            terrainData.SetHeights(startX, startZ, heights);
        }

        private bool TryGetOverlapBounds(Terrain a, Terrain b, out Bounds overlapBounds)
        {
            Bounds boundsA = GetTerrainWorldBounds(a);
            Bounds boundsB = GetTerrainWorldBounds(b);

            float minX = Mathf.Max(boundsA.min.x, boundsB.min.x);
            float maxX = Mathf.Min(boundsA.max.x, boundsB.max.x);

            float minZ = Mathf.Max(boundsA.min.z, boundsB.min.z);
            float maxZ = Mathf.Min(boundsA.max.z, boundsB.max.z);

            if (minX >= maxX || minZ >= maxZ)
            {
                overlapBounds = default;
                return false;
            }

            Vector3 center = new Vector3(
                (minX + maxX) * 0.5f,
                0f,
                (minZ + maxZ) * 0.5f
            );

            Vector3 size = new Vector3(
                maxX - minX,
                0f,
                maxZ - minZ
            );

            overlapBounds = new Bounds(center, size);
            return true;
        }

        private Bounds GetTerrainWorldBounds(Terrain terrain)
        {
            TerrainData terrainData = terrain.terrainData;
            Vector3 position = terrain.transform.position;

            Vector3 center = position + new Vector3(
                terrainData.size.x * 0.5f,
                terrainData.size.y * 0.5f,
                terrainData.size.z * 0.5f
            );

            return new Bounds(center, terrainData.size);
        }

        private void OnValidate()
        {
            TargetNormalizedHeight = Mathf.Clamp01(TargetNormalizedHeight);
            Padding = Mathf.Max(0f, Padding);
        }
    }
}