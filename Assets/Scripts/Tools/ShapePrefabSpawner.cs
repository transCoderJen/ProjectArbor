using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


    /// <summary>
    /// Spawns random prefabs across the top surface of a collider using editor buttons.
    /// Useful for grass clumps, rocks, trees, mushrooms, and similar scatter objects.
    /// </summary>
    [ExecuteAlways]
    public class ShapePrefabSpawner : MonoBehaviour
    {
        #region Inspector Fields
        [Header("References")]
        [SerializeField] private Collider TargetCollider;
        [SerializeField] private Transform SpawnParent;

        [Header("Prefabs")]
        [SerializeField] private List<GameObject> PrefabsToSpawn = new List<GameObject>();

        [Header("Spawn Settings")]
        [Tooltip("Objects per square unit. Final count is based on collider X/Z footprint.")]
        [SerializeField] private float Density = 0.25f;

        [Tooltip("Extra padding inward from the collider bounds edge.")]
        [SerializeField] private float EdgePadding = 0.1f;

        [Tooltip("How many failed attempts are allowed while searching for valid spawn points.")]
        [SerializeField] private int MaxPlacementAttempts = 5000;

        [Header("Scale")]
        [SerializeField] private bool UseHeightRange = false;
        [SerializeField] private float FixedHeight = 1f;
        [SerializeField] private Vector2 HeightRange = new Vector2(0.75f, 1.5f);

        [Header("Rotation")]
        [SerializeField] private bool RandomYRotation = true;
        [SerializeField] private Vector2 YRotationRange = new Vector2(0f, 360f);

        [Header("Placement Rules")]
        [Tooltip("If enabled, spawned objects align their up direction to the hit normal.")]
        [SerializeField] private bool AlignToSurfaceNormal = true;

        [Tooltip("Optional mask for blocking placement if another collider is already in the way.")]
        [SerializeField] private LayerMask OverlapBlockers = ~0;

        [Tooltip("Radius used to prevent overlapping placements.")]
        [SerializeField] private float CollisionCheckRadius = 0.25f;

        [Tooltip("Small vertical offset after placement to prevent clipping into the surface.")]
        [SerializeField] private float SurfaceOffset = 0.01f;
        #endregion

        #region Public Methods
        /// <summary>
        /// Spawns prefabs across the collider surface based on the configured density.
        /// </summary>
        [ContextMenu("Spawn Prefabs")]
        public void SpawnPrefabs()
        {
            if (!ValidateSetup())
            {
                return;
            }

            EnsureSpawnParent();

            Bounds bounds = TargetCollider.bounds;
            float area = Mathf.Max(0f, (bounds.size.x - EdgePadding * 2f) * (bounds.size.z - EdgePadding * 2f));
            int spawnCount = Mathf.RoundToInt(area * Density);

            if (spawnCount <= 0)
            {
                Debug.LogWarning($"[{nameof(ShapePrefabSpawner)}] Spawn count is 0. Increase Density or use a larger collider.", this);
                return;
            }

            int placedCount = 0;
            int attempts = 0;

            while (placedCount < spawnCount && attempts < MaxPlacementAttempts)
            {
                attempts++;

                if (!TryGetSpawnPoint(out RaycastHit hit))
                {
                    continue;
                }

                float targetHeight = GetRandomHeight();
                GameObject prefab = GetRandomPrefab();

                if (prefab == null)
                {
                    continue;
                }

                Vector3 spawnPosition = hit.point + hit.normal * SurfaceOffset;
                Quaternion spawnRotation = GetSpawnRotation(hit.normal);
                Vector3 spawnScale = GetScaledSize(prefab.transform.localScale, targetHeight);

                if (IsBlocked(spawnPosition, spawnRotation, spawnScale))
                {
                    continue;
                }

                CreateInstance(prefab, spawnPosition, spawnRotation, spawnScale);
                placedCount++;
            }

            Debug.Log($"[{nameof(ShapePrefabSpawner)}] Spawned {placedCount}/{spawnCount} prefabs after {attempts} attempts.", this);
        }

        /// <summary>
        /// Removes all spawned children under the spawn parent.
        /// </summary>
        [ContextMenu("Clear Spawned Prefabs")]
        public void ClearSpawnedPrefabs()
        {
            if (SpawnParent == null)
            {
                return;
            }

#if UNITY_EDITOR
            for (int i = SpawnParent.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(SpawnParent.GetChild(i).gameObject);
            }
#else
            for (int i = SpawnParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(SpawnParent.GetChild(i).gameObject);
            }
#endif
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Validates that all required references and values are usable.
        /// </summary>
        private bool ValidateSetup()
        {
            if (TargetCollider == null)
            {
                TargetCollider = GetComponent<Collider>();
            }

            if (TargetCollider == null)
            {
                Debug.LogError($"[{nameof(ShapePrefabSpawner)}] No Collider found. Assign a TargetCollider or place this on an object with a Collider.", this);
                return false;
            }

            if (PrefabsToSpawn == null || PrefabsToSpawn.Count == 0)
            {
                Debug.LogError($"[{nameof(ShapePrefabSpawner)}] No prefabs assigned.", this);
                return false;
            }

            if (FixedHeight <= 0f)
            {
                FixedHeight = 1f;
            }

            if (HeightRange.x <= 0f)
            {
                HeightRange.x = 0.1f;
            }

            if (HeightRange.y < HeightRange.x)
            {
                HeightRange.y = HeightRange.x;
            }

            return true;
        }

        /// <summary>
        /// Ensures a valid parent exists for spawned instances.
        /// </summary>
        private void EnsureSpawnParent()
        {
            if (SpawnParent != null)
            {
                return;
            }

            Transform existing = transform.Find("Spawned Prefabs");
            if (existing != null)
            {
                SpawnParent = existing;
                return;
            }

            GameObject parent = new GameObject("Spawned Prefabs");
            parent.transform.SetParent(transform);
            parent.transform.localPosition = Vector3.zero;
            parent.transform.localRotation = Quaternion.identity;
            parent.transform.localScale = Vector3.one;
            SpawnParent = parent.transform;
        }

        /// <summary>
        /// Attempts to find a valid point on the top surface of the target collider.
        /// </summary>
        private bool TryGetSpawnPoint(out RaycastHit hit)
        {
            Bounds bounds = TargetCollider.bounds;

            float randomX = Random.Range(bounds.min.x + EdgePadding, bounds.max.x - EdgePadding);
            float randomZ = Random.Range(bounds.min.z + EdgePadding, bounds.max.z - EdgePadding);

            Vector3 rayOrigin = new Vector3(randomX, bounds.max.y + 5f, randomZ);
            Ray ray = new Ray(rayOrigin, Vector3.down);

            if (TargetCollider.Raycast(ray, out hit, bounds.size.y + 10f))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a random prefab from the configured list.
        /// </summary>
        private GameObject GetRandomPrefab()
        {
            if (PrefabsToSpawn == null || PrefabsToSpawn.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, PrefabsToSpawn.Count);
            return PrefabsToSpawn[index];
        }

        /// <summary>
        /// Gets the target spawned height based on the current scaling mode.
        /// </summary>
        private float GetRandomHeight()
        {
            return UseHeightRange
                ? Random.Range(HeightRange.x, HeightRange.y)
                : FixedHeight;
        }

        /// <summary>
        /// Builds the spawn rotation, optionally aligning to the surface normal.
        /// </summary>
        private Quaternion GetSpawnRotation(Vector3 surfaceNormal)
        {
            Quaternion baseRotation = AlignToSurfaceNormal
                ? Quaternion.FromToRotation(Vector3.up, surfaceNormal)
                : Quaternion.identity;

            if (!RandomYRotation)
            {
                return baseRotation;
            }

            float yRotation = Random.Range(YRotationRange.x, YRotationRange.y);
            Quaternion randomYaw = Quaternion.AngleAxis(yRotation, Vector3.up);

            return randomYaw * baseRotation;
        }

        /// <summary>
        /// Scales uniformly so the resulting local Y matches the requested height.
        /// Width and depth are preserved proportionally.
        /// </summary>
        private Vector3 GetScaledSize(Vector3 originalScale, float targetHeight)
        {
            if (Mathf.Approximately(originalScale.y, 0f))
            {
                return Vector3.one * targetHeight;
            }

            float multiplier = targetHeight / originalScale.y;
            return originalScale * multiplier;
        }

        /// <summary>
        /// Checks whether placement should be blocked due to overlap.
        /// </summary>
        private bool IsBlocked(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (CollisionCheckRadius <= 0f)
            {
                return false;
            }

            Collider[] hits = Physics.OverlapSphere(
                position,
                CollisionCheckRadius * Mathf.Max(scale.x, scale.z),
                OverlapBlockers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];

                if (hit == null)
                {
                    continue;
                }

                if (hit == TargetCollider)
                {
                    continue;
                }

                if (SpawnParent != null && hit.transform.IsChildOf(SpawnParent))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates the prefab instance with editor undo support.
        /// </summary>
        private void CreateInstance(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
        {
#if UNITY_EDITOR
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SpawnParent);
            if (instance == null)
            {
                instance = Instantiate(prefab, SpawnParent);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Prefabs");
#else
            GameObject instance = Instantiate(prefab, SpawnParent);
#endif

            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = scale;
        }

        private void OnDrawGizmosSelected()
        {
            if (TargetCollider == null)
            {
                TargetCollider = GetComponent<Collider>();
            }

            if (TargetCollider == null)
            {
                return;
            }

            Gizmos.color = Color.green;
            Bounds bounds = TargetCollider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        #endregion
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom inspector for quick spawn and clear buttons.
    /// </summary>
    [CustomEditor(typeof(ShapePrefabSpawner))]
    public class ShapePrefabSpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10f);

            ShapePrefabSpawner spawner = (ShapePrefabSpawner)target;

            if (GUILayout.Button("Spawn Prefabs"))
            {
                spawner.SpawnPrefabs();
                EditorUtility.SetDirty(spawner);
            }

            if (GUILayout.Button("Clear Spawned Prefabs"))
            {
                spawner.ClearSpawnedPrefabs();
                EditorUtility.SetDirty(spawner);
            }
        }
    }
#endif
