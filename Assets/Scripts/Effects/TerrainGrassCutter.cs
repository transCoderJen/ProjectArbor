using System;
using UnityEngine;

namespace ShiftedSignal.Garden.Effects
{
    public enum CutShape
    {
        Box,
        Sphere
    }

    /// <summary>
    /// Result data for a terrain grass cut operation.
    /// </summary>
    [Serializable]
    public struct GrassCutResult
    {
        public bool GrassRemoved;
        public Vector3 WorldPosition;
    }

    /// <summary>
    /// Cuts Unity Terrain detail grass around a calculated world position.
    /// Supports circle and box cutting modes.
    /// </summary>
    public class TerrainGrassCutter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Terrain targetTerrain;

        private Terrain TerrainRef
        {
            get
            {
                if (targetTerrain != null)
                    return targetTerrain;

                return Terrain.activeTerrain;
            }
        }

        [Header("Cut Settings")]
        [SerializeField] private float cutRadius = 1.5f;
        [SerializeField] private bool cutAllDetailLayers = true;
        [SerializeField] private int detailLayerIndex = 0;
        [SerializeField] private CutShape defaultCutShape = CutShape.Sphere;

        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmo = true;

        /// <summary>
        /// Cuts grass using the default cut settings.
        /// </summary>
        /// <param name="facingDir">The player's facing direction in XZ space.</param>
        public GrassCutResult CutGrass(Vector2 facingDir)
        {
            switch (defaultCutShape)
            {
                case CutShape.Box:
                {
                    float boxSize = cutRadius * 2f;
                    Vector3 cutCenter = GetCutCenterOnTerrain(facingDir, cutRadius);
                    return CutGrassBoxAtPosition(cutCenter, boxSize, boxSize);
                }

                case CutShape.Sphere:
                default:
                {
                    Vector3 cutCenter = GetCutCenterOnTerrain(facingDir, cutRadius);
                    return CutGrassCircleAtPosition(cutCenter, cutRadius);
                }
            }
        }

        /// <summary>
        /// Cuts grass using a specific shape and size.
        /// For Box, size is the full box width/depth.
        /// For Sphere, size is the full diameter.
        /// </summary>
        /// <param name="facingDir">The player's facing direction in XZ space.</param>
        /// <param name="size">Full size. Box = width/depth, Sphere = diameter.</param>
        /// <param name="shape">Cut shape to use.</param>
        public GrassCutResult CutGrass(Vector2 facingDir, float size, CutShape shape)
        {
            switch (shape)
            {
                case CutShape.Box:
                {
                    float halfExtent = size * 0.5f;
                    Vector3 cutCenter = GetCutCenterOnTerrain(facingDir, halfExtent);
                    return CutGrassBoxAtPosition(cutCenter, size, size);
                }

                case CutShape.Sphere:
                default:
                {
                    float radius = size * 0.5f;
                    Vector3 cutCenter = GetCutCenterOnTerrain(facingDir, radius);
                    return CutGrassCircleAtPosition(cutCenter, radius);
                }
            }
        }

        /// <summary>
        /// Cuts grass using a rectangular box.
        /// </summary>
        /// <param name="facingDir">The player's facing direction in XZ space.</param>
        /// <param name="sizeX">Full width of the box.</param>
        /// <param name="sizeZ">Full depth of the box.</param>
        public GrassCutResult CutGrassBox(Vector2 facingDir, float sizeX, float sizeZ)
        {
            float forwardDistance = Mathf.Max(sizeX, sizeZ) * 0.5f;
            Vector3 cutCenter = GetCutCenterOnTerrain(facingDir, forwardDistance);
            return CutGrassBoxAtPosition(cutCenter, sizeX, sizeZ);
        }

        /// <summary>
        /// Cuts grass using a circular area.
        /// </summary>
        /// <param name="facingDir">The player's facing direction in XZ space.</param>
        /// <param name="radius">Radius of the cut.</param>
        public GrassCutResult CutGrassCircle(Vector2 facingDir, float radius)
        {
            Vector3 cutCenter = GetCutCenterOnTerrain(facingDir, radius);
            return CutGrassCircleAtPosition(cutCenter, radius);
        }

        /// <summary>
        /// Calculates the actual cut center using facing direction, half-size forward offset, and terrain height.
        /// </summary>
        private Vector3 GetCutCenterOnTerrain(Vector2 facingDir, float forwardDistance)
        {
            if (TerrainRef == null)
            {
                return transform.position;
            }

            Vector3 direction = new Vector3(facingDir.x, 0f, facingDir.y);

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = transform.forward;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();

            TerrainData terrainData = TerrainRef.terrainData;
            Vector3 terrainPosition = TerrainRef.transform.position;
            Vector3 terrainSize = terrainData.size;

            Vector3 cutCenter = transform.position + (direction * forwardDistance);

            cutCenter.x = Mathf.Clamp(cutCenter.x, terrainPosition.x, terrainPosition.x + terrainSize.x);
            cutCenter.z = Mathf.Clamp(cutCenter.z, terrainPosition.z, terrainPosition.z + terrainSize.z);

            float terrainHeight = TerrainRef.SampleHeight(cutCenter) + terrainPosition.y;
            cutCenter.y = terrainHeight;

            return cutCenter;
        }

        /// <summary>
        /// Performs a circle cut at a world position.
        /// </summary>
        private GrassCutResult CutGrassCircleAtPosition(Vector3 worldPosition, float radius)
        {
            if (!TryGetTerrainContext(worldPosition, out TerrainData terrainData, out Vector3 localPosition, out Vector3 terrainSize))
            {
                return default;
            }

            int detailWidth = terrainData.detailWidth;
            int detailHeight = terrainData.detailHeight;

            int centerX = WorldToDetailX(localPosition.x, terrainSize.x, detailWidth);
            int centerY = WorldToDetailY(localPosition.z, terrainSize.z, detailHeight);

            int radiusInCellsX = Mathf.CeilToInt((radius / terrainSize.x) * detailWidth);
            int radiusInCellsY = Mathf.CeilToInt((radius / terrainSize.z) * detailHeight);

            int startX = Mathf.Clamp(centerX - radiusInCellsX, 0, detailWidth - 1);
            int startY = Mathf.Clamp(centerY - radiusInCellsY, 0, detailHeight - 1);
            int endX = Mathf.Clamp(centerX + radiusInCellsX, 0, detailWidth - 1);
            int endY = Mathf.Clamp(centerY + radiusInCellsY, 0, detailHeight - 1);

            int width = endX - startX + 1;
            int height = endY - startY + 1;

            if (width <= 0 || height <= 0)
            {
                return default;
            }

            bool anyGrassRemoved = false;
            Vector2 removedDetailSum = Vector2.zero;
            int removedDetailCount = 0;

            ApplyCutToLayers(
                terrainData,
                layerIndex => CutLayerCircle(
                    terrainData,
                    layerIndex,
                    startX,
                    startY,
                    width,
                    height,
                    centerX,
                    centerY,
                    radiusInCellsX,
                    radiusInCellsY,
                    ref removedDetailSum,
                    ref removedDetailCount),
                ref anyGrassRemoved);

            return BuildResult(terrainData, removedDetailSum, removedDetailCount, worldPosition, anyGrassRemoved);
        }

        /// <summary>
        /// Performs a box cut at a world position.
        /// </summary>
        private GrassCutResult CutGrassBoxAtPosition(Vector3 worldPosition, float boxSizeX, float boxSizeZ)
        {
            if (!TryGetTerrainContext(worldPosition, out TerrainData terrainData, out Vector3 localPosition, out Vector3 terrainSize))
            {
                return default;
            }

            int detailWidth = terrainData.detailWidth;
            int detailHeight = terrainData.detailHeight;

            int centerX = WorldToDetailX(localPosition.x, terrainSize.x, detailWidth);
            int centerY = WorldToDetailY(localPosition.z, terrainSize.z, detailHeight);

            int halfBoxInCellsX = Mathf.CeilToInt(((boxSizeX * 0.5f) / terrainSize.x) * detailWidth);
            int halfBoxInCellsY = Mathf.CeilToInt(((boxSizeZ * 0.5f) / terrainSize.z) * detailHeight);

            int startX = Mathf.Clamp(centerX - halfBoxInCellsX, 0, detailWidth - 1);
            int startY = Mathf.Clamp(centerY - halfBoxInCellsY, 0, detailHeight - 1);
            int endX = Mathf.Clamp(centerX + halfBoxInCellsX, 0, detailWidth - 1);
            int endY = Mathf.Clamp(centerY + halfBoxInCellsY, 0, detailHeight - 1);

            int width = endX - startX + 1;
            int height = endY - startY + 1;

            if (width <= 0 || height <= 0)
            {
                return default;
            }

            bool anyGrassRemoved = false;
            Vector2 removedDetailSum = Vector2.zero;
            int removedDetailCount = 0;

            ApplyCutToLayers(
                terrainData,
                layerIndex => CutLayerBox(
                    terrainData,
                    layerIndex,
                    startX,
                    startY,
                    width,
                    height,
                    ref removedDetailSum,
                    ref removedDetailCount),
                ref anyGrassRemoved);

            return BuildResult(terrainData, removedDetailSum, removedDetailCount, worldPosition, anyGrassRemoved);
        }

        /// <summary>
        /// Applies the cut operation to the selected terrain detail layers.
        /// </summary>
        private void ApplyCutToLayers(
            TerrainData terrainData,
            Func<int, bool> cutAction,
            ref bool anyGrassRemoved)
        {
            if (cutAllDetailLayers)
            {
                int layerCount = terrainData.detailPrototypes.Length;

                for (int layer = 0; layer < layerCount; layer++)
                {
                    if (cutAction(layer))
                    {
                        anyGrassRemoved = true;
                    }
                }
            }
            else
            {
                if (detailLayerIndex < 0 || detailLayerIndex >= terrainData.detailPrototypes.Length)
                {
                    Debug.LogWarning($"Invalid detail layer index {detailLayerIndex} on {name}.");
                    return;
                }

                if (cutAction(detailLayerIndex))
                {
                    anyGrassRemoved = true;
                }
            }
        }

        /// <summary>
        /// Cuts a circular region from a single detail layer.
        /// </summary>
        private bool CutLayerCircle(
            TerrainData terrainData,
            int layerIndex,
            int startX,
            int startY,
            int width,
            int height,
            int centerX,
            int centerY,
            int radiusInCellsX,
            int radiusInCellsY,
            ref Vector2 removedDetailSum,
            ref int removedDetailCount)
        {
            int[,] details = terrainData.GetDetailLayer(startX, startY, width, height, layerIndex);
            bool grassRemoved = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int mapX = startX + x;
                    int mapY = startY + y;

                    float normalizedDistanceX = radiusInCellsX > 0 ? (mapX - centerX) / (float)radiusInCellsX : 0f;
                    float normalizedDistanceY = radiusInCellsY > 0 ? (mapY - centerY) / (float)radiusInCellsY : 0f;
                    float sqrDistance = (normalizedDistanceX * normalizedDistanceX) + (normalizedDistanceY * normalizedDistanceY);

                    if (sqrDistance <= 1f && details[y, x] > 0)
                    {
                        details[y, x] = 0;
                        grassRemoved = true;
                        removedDetailSum += new Vector2(mapX, mapY);
                        removedDetailCount++;
                    }
                }
            }

            if (grassRemoved)
            {
                terrainData.SetDetailLayer(startX, startY, layerIndex, details);
            }

            return grassRemoved;
        }

        /// <summary>
        /// Cuts a rectangular region from a single detail layer.
        /// </summary>
        private bool CutLayerBox(
            TerrainData terrainData,
            int layerIndex,
            int startX,
            int startY,
            int width,
            int height,
            ref Vector2 removedDetailSum,
            ref int removedDetailCount)
        {
            int[,] details = terrainData.GetDetailLayer(startX, startY, width, height, layerIndex);
            bool grassRemoved = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (details[y, x] > 0)
                    {
                        details[y, x] = 0;
                        grassRemoved = true;
                        removedDetailSum += new Vector2(startX + x, startY + y);
                        removedDetailCount++;
                    }
                }
            }

            if (grassRemoved)
            {
                terrainData.SetDetailLayer(startX, startY, layerIndex, details);
            }

            return grassRemoved;
        }

        /// <summary>
        /// Builds the final result for the cut operation.
        /// </summary>
        private GrassCutResult BuildResult(
            TerrainData terrainData,
            Vector2 removedDetailSum,
            int removedDetailCount,
            Vector3 fallbackWorldPosition,
            bool grassRemoved)
        {
            if (!grassRemoved || removedDetailCount <= 0 || TerrainRef == null)
            {
                return new GrassCutResult
                {
                    GrassRemoved = false,
                    WorldPosition = fallbackWorldPosition
                };
            }

            float averageDetailX = removedDetailSum.x / removedDetailCount;
            float averageDetailY = removedDetailSum.y / removedDetailCount;

            float normalizedX = averageDetailX / Mathf.Max(terrainData.detailWidth - 1, 1);
            float normalizedZ = averageDetailY / Mathf.Max(terrainData.detailHeight - 1, 1);

            Vector3 terrainPosition = TerrainRef.transform.position;
            Vector3 terrainSize = terrainData.size;

            float worldX = terrainPosition.x + (normalizedX * terrainSize.x);
            float worldZ = terrainPosition.z + (normalizedZ * terrainSize.z);
            float worldY = TerrainRef.SampleHeight(new Vector3(worldX, terrainPosition.y, worldZ)) + terrainPosition.y;

            return new GrassCutResult
            {
                GrassRemoved = true,
                WorldPosition = new Vector3(worldX, worldY, worldZ)
            };
        }

        /// <summary>
        /// Validates that the world position is inside the terrain and provides terrain context.
        /// </summary>
        private bool TryGetTerrainContext(
            Vector3 worldPosition,
            out TerrainData terrainData,
            out Vector3 localPosition,
            out Vector3 terrainSize)
        {
            terrainData = null;
            localPosition = Vector3.zero;
            terrainSize = Vector3.zero;

            if (TerrainRef == null)
            {
                Debug.LogWarning($"{nameof(TerrainGrassCutter)} on {name} has no target terrain assigned.");
                return false;
            }

            terrainData = TerrainRef.terrainData;
            Vector3 terrainPosition = TerrainRef.transform.position;
            localPosition = worldPosition - terrainPosition;
            terrainSize = terrainData.size;

            if (localPosition.x < 0f || localPosition.z < 0f ||
                localPosition.x > terrainSize.x || localPosition.z > terrainSize.z)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Converts local terrain X position to detail map X coordinate.
        /// </summary>
        private int WorldToDetailX(float localX, float terrainWidth, int detailWidth)
        {
            float normalizedX = localX / terrainWidth;
            return Mathf.RoundToInt(normalizedX * (detailWidth - 1));
        }

        /// <summary>
        /// Converts local terrain Z position to detail map Y coordinate.
        /// </summary>
        private int WorldToDetailY(float localZ, float terrainLength, int detailHeight)
        {
            float normalizedZ = localZ / terrainLength;
            return Mathf.RoundToInt(normalizedZ * (detailHeight - 1));
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmo)
            {
                return;
            }

            if (TerrainRef == null)
            {
                return;
            }

            Gizmos.color = Color.green;

            Vector3 cutCenter = GetCutCenterOnTerrain(Vector2.up, cutRadius);

            switch (defaultCutShape)
            {
                case CutShape.Box:
                    Gizmos.DrawWireCube(cutCenter, new Vector3(cutRadius * 2f, 1f, cutRadius * 2f));
                    break;

                case CutShape.Sphere:
                default:
                    Gizmos.DrawWireSphere(cutCenter, cutRadius);
                    break;
            }
        }
    }
}