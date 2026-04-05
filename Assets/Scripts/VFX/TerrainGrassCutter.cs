using System.ComponentModel;
using UnityEngine;

    /// <summary>
    /// Cuts terrain detail grass around a world position by modifying Terrain detail layers.
    /// Works with Unity Terrain grass/details painted through the Terrain system.
    /// </summary>
    public class TerrainGrassCutter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Terrain TargetTerrain;

        [Header("Cut Settings")]
        [SerializeField] private float CutRadius = 1.5f;
        [SerializeField] private bool CutAllDetailLayers = true;
        [SerializeField] private int DetailLayerIndex = 0;

        [Header("Particle System")]
        [SerializeField] private ParticleSystem GrassParticleSystem;

        [Header("Debug")]
        [SerializeField] private bool DrawDebugGizmo = true;


        /// <summary>
        /// Cuts grass at this component's current world position.
        /// </summary>
        [ContextMenu("Cut Grass At Current Position")]
        public void CutGrassAtCurrentPosition()
        {
            CutGrass(transform.position);
        }

        /// <summary>
        /// Cuts grass around the provided world position.
        /// </summary>
        /// <param name="worldPosition">World position to cut around.</param>
        public void CutGrass(Vector3 worldPosition)
        {
            if (TargetTerrain == null)
            {
                Debug.LogWarning($"{nameof(TerrainGrassCutter)} on {name} has no target terrain assigned.");
                return;
            }

            TerrainData terrainData = TargetTerrain.terrainData;
            Vector3 terrainPosition = TargetTerrain.transform.position;
            Vector3 localPosition = worldPosition - terrainPosition;
            Vector3 terrainSize = terrainData.size;

            if (localPosition.x < 0f || localPosition.z < 0f ||
                localPosition.x > terrainSize.x || localPosition.z > terrainSize.z)
            {
                return;
            }

            int detailWidth = terrainData.detailWidth;
            int detailHeight = terrainData.detailHeight;

            float normalizedX = localPosition.x / terrainSize.x;
            float normalizedZ = localPosition.z / terrainSize.z;

            int centerX = Mathf.RoundToInt(normalizedX * detailWidth);
            int centerY = Mathf.RoundToInt(normalizedZ * detailHeight);

            int radiusInCellsX = Mathf.CeilToInt((CutRadius / terrainSize.x) * detailWidth);
            int radiusInCellsY = Mathf.CeilToInt((CutRadius / terrainSize.z) * detailHeight);

            int startX = Mathf.Clamp(centerX - radiusInCellsX, 0, detailWidth - 1);
            int startY = Mathf.Clamp(centerY - radiusInCellsY, 0, detailHeight - 1);
            int endX = Mathf.Clamp(centerX + radiusInCellsX, 0, detailWidth - 1);
            int endY = Mathf.Clamp(centerY + radiusInCellsY, 0, detailHeight - 1);

            int width = endX - startX + 1;
            int height = endY - startY + 1;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            if (CutAllDetailLayers)
            {
                int layerCount = terrainData.detailPrototypes.Length;

                for (int layer = 0; layer < layerCount; layer++)
                {
                    CutLayer(terrainData, layer, startX, startY, width, height, centerX, centerY, radiusInCellsX, radiusInCellsY);
                }
            }
            else
            {
                if (DetailLayerIndex < 0 || DetailLayerIndex >= terrainData.detailPrototypes.Length)
                {
                    Debug.LogWarning($"Invalid detail layer index {DetailLayerIndex} on {name}.");
                    return;
                }

                CutLayer(terrainData, DetailLayerIndex, startX, startY, width, height, centerX, centerY, radiusInCellsX, radiusInCellsY);
            }

            GrassParticleSystem.Play();
        }

        private void CutLayer(
            TerrainData terrainData,
            int layerIndex,
            int startX,
            int startY,
            int width,
            int height,
            int centerX,
            int centerY,
            int radiusInCellsX,
            int radiusInCellsY)
        {
            int[,] details = terrainData.GetDetailLayer(startX, startY, width, height, layerIndex);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int mapX = startX + x;
                    int mapY = startY + y;

                    float normalizedDistanceX = radiusInCellsX > 0 ? (mapX - centerX) / (float)radiusInCellsX : 0f;
                    float normalizedDistanceY = radiusInCellsY > 0 ? (mapY - centerY) / (float)radiusInCellsY : 0f;

                    float sqrDistance = (normalizedDistanceX * normalizedDistanceX) + (normalizedDistanceY * normalizedDistanceY);

                    if (sqrDistance <= 1f)
                    {
                        details[x, y] = 0;
                    }
                }
            }

            terrainData.SetDetailLayer(startX, startY, layerIndex, details);
        }

        private void OnDrawGizmosSelected()
        {
            if (!DrawDebugGizmo)
            {
                return;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, CutRadius);
        }
    }
