using UnityEngine;

[ExecuteAlways]
public class TerrainPathBorderSpawner : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField] private Terrain Terrain;
    [SerializeField] private int DirtTextureIndex = 1;
    [SerializeField] private float PathThreshold = 0.5f;

    [Header("Spawning")]
    [SerializeField] private GameObject[] BorderPrefabs;
    [SerializeField] private Transform Parent;
    [SerializeField] private float Spacing = 2f;
    [SerializeField] private float RandomOffset = 0.4f;
    [SerializeField] private float SpawnChance = 0.6f;

    [Header("Billboard")]
    [SerializeField] private bool FaceCamera = true;
    [SerializeField] private Camera TargetCamera;
    [SerializeField] private float MinXRotation = -0f;
    [SerializeField] private float MaxXRotation = 10f;
    [SerializeField] private float MinYRotation = -10f;
    [SerializeField] private float MaxYRotation = 10f;
    [SerializeField] private float MinZRotation = -30f;
    [SerializeField] private float MaxZRotation = 30f;

    [Header("Random Size")]
    [SerializeField] private float MinScale = 0.8f;
    [SerializeField] private float MaxScale = 1.2f;

    [Header("Cleanup")]
    [SerializeField] private bool ClearBeforeGenerate = true;

    public void GeneratePathBorders()
    {
        if (Terrain == null || BorderPrefabs == null || BorderPrefabs.Length == 0)
            return;

        if (Parent == null)
            Parent = transform;

        if (ClearBeforeGenerate)
            ClearSpawnedPrefabs();

        TerrainData data = Terrain.terrainData;

        int width = data.alphamapWidth;
        int height = data.alphamapHeight;

        float[,,] maps = data.GetAlphamaps(0, 0, width, height);

        int stepX = Mathf.Max(1, Mathf.RoundToInt(Spacing / data.size.x * width));
        int stepY = Mathf.Max(1, Mathf.RoundToInt(Spacing / data.size.z * height));

        for (int y = 1; y < height - 1; y += stepY)
        {
            for (int x = 1; x < width - 1; x += stepX)
            {
                if (!IsPath(maps, x, y))
                    continue;

                if (!IsBorder(maps, x, y))
                    continue;

                if (Random.value > SpawnChance)
                    continue;

                SpawnAt(data, x, y);
            }
        }
    }

    public void ClearSpawnedPrefabs()
    {
        if (Parent == null)
            Parent = transform;

        for (int i = Parent.childCount - 1; i >= 0; i--)
        {
    #if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(Parent.GetChild(i).gameObject);
            else
                Destroy(Parent.GetChild(i).gameObject);
    #else
            Destroy(Parent.GetChild(i).gameObject);
    #endif
        }
    }

    private bool IsPath(float[,,] maps, int x, int y)
    {
        return maps[y, x, DirtTextureIndex] >= PathThreshold;
    }

    private bool IsBorder(float[,,] maps, int x, int y)
    {
        return !IsPath(maps, x + 1, y) ||
               !IsPath(maps, x - 1, y) ||
               !IsPath(maps, x, y + 1) ||
               !IsPath(maps, x, y - 1);
    }

    private void SpawnAt(TerrainData data, int x, int y)
    {
        float normalizedX = x / (float)data.alphamapWidth;
        float normalizedZ = y / (float)data.alphamapHeight;

        Vector3 terrainPosition = Terrain.transform.position;

        Vector3 worldPosition = new Vector3(
            terrainPosition.x + normalizedX * data.size.x,
            0f,
            terrainPosition.z + normalizedZ * data.size.z);

        worldPosition.x += Random.Range(-RandomOffset, RandomOffset);
        worldPosition.z += Random.Range(-RandomOffset, RandomOffset);
        worldPosition.y = Terrain.SampleHeight(worldPosition) + terrainPosition.y;

        GameObject prefab = BorderPrefabs[Random.Range(0, BorderPrefabs.Length)];

// #if UNITY_EDITOR
//         if (!Application.isPlaying)
//             UnityEditor.PrefabUtility.InstantiatePrefab(prefab, Parent);
// #endif

        GameObject obj = Instantiate(prefab, worldPosition, Quaternion.identity, Parent);

        if (FaceCamera)
        {
            Camera cam = TargetCamera != null ? TargetCamera : Camera.main;

            if (cam != null)
            {
                obj.transform.forward = cam.transform.forward;
            }
        }

        
        float randomX = Random.Range(MinXRotation, MaxXRotation);
        float randomY = Random.Range(MinYRotation, MaxYRotation);
        float randomZ = Random.Range(MinZRotation, MaxZRotation);
        obj.transform.Rotate(randomX, randomY, randomZ, Space.Self);

        float randomScale = Random.Range(MinScale, MaxScale);
        obj.transform.localScale *= randomScale;
    }

    private void ClearChildren()
    {
        for (int i = Parent.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(Parent.GetChild(i).gameObject);
            else
                DestroyImmediate(Parent.GetChild(i).gameObject);
        }
    }
}