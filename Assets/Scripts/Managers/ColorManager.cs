using UnityEngine;
using System;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.Tools;



#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace ShiftedSignal.Garden.Managers
{ 
    [ExecuteAlways]
    public class ColorManager : Singleton<ColorManager>
    {
        [Header("Current Scheme")]
        [SerializeField] private ColorScheme currentColorScheme;

        [Header("Editable Inspector Values")]
        [ColorUsage(false, true)]
        [SerializeField] private Color selectionBoxBorder = Color.white;

        [Header("Terrain")]
        [SerializeField] private ColorScheme.TerrainLayerColorEntry[] terrainLayerColorEntries;

        [ColorUsage(false, true)]
        [SerializeField] private Color terrainTintCompensation = Color.white;

        [Header("Trees")]
        [SerializeField] private ColorScheme.TreeColorEntry[] treeColorEntries;

    #if UNITY_EDITOR
        [Header("Editor Save Settings")]
        [SerializeField] private string colorSchemeSaveFolder = "Assets/Data/ColorSchemes";
        [SerializeField] private string newColorSchemeName = "NewColorScheme";
    #endif

        [SerializeField, HideInInspector] private ColorScheme lastLoadedColorScheme;

        private bool pendingApply;

        public Color SelectionBoxBorder
        {
            get => selectionBoxBorder;
            set
            {
                if (selectionBoxBorder == value)
                    return;

                selectionBoxBorder = value;
                QueueApply();
            }
        }

        public ColorScheme CurrentColorScheme => currentColorScheme;

        private void OnEnable()
        {
            QueueApply();
        }

        private void Start()
        {
            if (currentColorScheme != null && lastLoadedColorScheme != currentColorScheme)
                LoadColorScheme(currentColorScheme);
            else
                QueueApply();
        }

        private void OnValidate()
        {
            bool colorSchemeChanged = currentColorScheme != lastLoadedColorScheme;

            if (colorSchemeChanged && currentColorScheme != null)
            {
                LoadColorSchemeValuesIntoInspector(currentColorScheme);
                lastLoadedColorScheme = currentColorScheme;
            }
            else if (currentColorScheme == null && lastLoadedColorScheme != null)
            {
                lastLoadedColorScheme = null;
            }

            QueueApply();
        }

        private void Update()
        {
            if (!pendingApply)
                return;

            pendingApply = false;
            ApplyAllColors();
        }

        public void LoadColorScheme(ColorScheme colorScheme)
        {
            if (colorScheme == null)
                return;

            currentColorScheme = colorScheme;
            LoadColorSchemeValuesIntoInspector(colorScheme);
            lastLoadedColorScheme = colorScheme;

            QueueApply();
        }

        private void LoadColorSchemeValuesIntoInspector(ColorScheme colorScheme)
        {
            if (colorScheme == null)
                return;

            selectionBoxBorder = colorScheme.SelectionBoxBorder;
            terrainLayerColorEntries = CloneEntries(colorScheme.TerrainLayerColorEntries);
            terrainTintCompensation = colorScheme.TerrainTintCompensation;
            treeColorEntries = CloneEntries(colorScheme.TreeColorEntries);
        }

        private void CopyInspectorValuesToColorScheme(ColorScheme colorScheme)
        {
            if (colorScheme == null)
                return;

            colorScheme.SelectionBoxBorder = selectionBoxBorder;
            colorScheme.TerrainLayerColorEntries = CloneEntries(terrainLayerColorEntries);
            colorScheme.TerrainTintCompensation = terrainTintCompensation;
            colorScheme.TreeColorEntries = CloneEntries(treeColorEntries);
        }

        public Color GetTerrainLayerColor(TerrainLayerName layerName)
        {
            if (terrainLayerColorEntries == null)
                return Color.white;

            for (int i = 0; i < terrainLayerColorEntries.Length; i++)
            {
                if (terrainLayerColorEntries[i].LayerName == layerName)
                    return terrainLayerColorEntries[i].Color;
            }

            return Color.white;
        }

    #if UNITY_EDITOR
        [ContextMenu("Load Current Color Scheme Into Inspector")]
        public void LoadCurrentColorSchemeIntoInspector()
        {
            if (currentColorScheme == null)
            {
                Debug.LogWarning("No ColorScheme is assigned to load from.", this);
                return;
            }

            LoadColorScheme(currentColorScheme);
            EditorUtility.SetDirty(this);

            Debug.Log($"Loaded ColorScheme '{currentColorScheme.name}' into inspector values.", this);
        }

        [ContextMenu("Overwrite Existing Color Scheme")]
        public void OverwriteExistingColorScheme()
        {
            if (currentColorScheme == null)
            {
                Debug.LogWarning("No ColorScheme is assigned to overwrite.", this);
                return;
            }

            CopyInspectorValuesToColorScheme(currentColorScheme);
            lastLoadedColorScheme = currentColorScheme;

            EditorUtility.SetDirty(currentColorScheme);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Overwrote ColorScheme '{currentColorScheme.name}' with current inspector values.", currentColorScheme);
        }

        [ContextMenu("Save New Color Scheme")]
        public void SaveNewColorScheme()
        {
            string folderPath = SanitizeFolderPath(colorSchemeSaveFolder);
            EnsureFolderExists(folderPath);

            ColorScheme newScheme = ScriptableObject.CreateInstance<ColorScheme>();
            CopyInspectorValuesToColorScheme(newScheme);

            string baseFileName = string.IsNullOrWhiteSpace(newColorSchemeName)
                ? "NewColorScheme"
                : newColorSchemeName.Trim();

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(folderPath, $"{baseFileName}.asset").Replace("\\", "/")
            );

            AssetDatabase.CreateAsset(newScheme, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            currentColorScheme = newScheme;
            lastLoadedColorScheme = newScheme;

            EditorUtility.SetDirty(newScheme);
            EditorUtility.SetDirty(this);

            Selection.activeObject = newScheme;

            Debug.Log($"Created new ColorScheme at: {assetPath}", newScheme);
        }

        private static string SanitizeFolderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return "Assets";

            folderPath = folderPath.Replace("\\", "/").Trim();

            if (!folderPath.StartsWith("Assets"))
                folderPath = Path.Combine("Assets", folderPath).Replace("\\", "/");

            return folderPath;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, parts[i]);

                currentPath = nextPath;
            }
        }
    #endif

        private void QueueApply()
        {
            pendingApply = true;
        }

        private void ApplyAllColors()
        {
            UpdateMaterialColors();
            ApplyTerrainTints();
        }

        private void UpdateMaterialColors()
        {
            if (GridManager.Instance != null)
                GridManager.Instance.UpdateSelectionBoxColors();

            ApplyTreeMaterials();
        }

        private void ApplyTreeMaterials()
        {
            if (treeColorEntries == null || treeColorEntries.Length == 0)
                return;

            for (int i = 0; i < treeColorEntries.Length; i++)
            {
                string tag = $"Tree{i + 1}";
                ApplyTreeMaterialToTag(tag, treeColorEntries[i]);
            }
        }

        private void ApplyTreeMaterialToTag(string tag, ColorScheme.TreeColorEntry entry)
        {
            GameObject[] trees = GameObject.FindGameObjectsWithTag(tag);

            foreach (GameObject tree in trees)
            {
                Renderer renderer = tree.GetComponent<Renderer>();
                if (renderer == null)
                    continue;

                ApplyColors(renderer, entry);
            }
        }

        private void ApplyColors(Renderer renderer, ColorScheme.TreeColorEntry entry)
        {
            renderer.sharedMaterial.SetColor("_LeavesColor", entry.LeavesColor);
            renderer.sharedMaterial.SetColor("_TrunkColor", entry.TrunkColor);
            renderer.sharedMaterial.SetColor("_LeavesBorderColor", entry.LeavesBorderColor);
            renderer.sharedMaterial.SetColor("_TrunkBorderColor", entry.TrunkBorderColor);
        }

        private void ApplyTerrainTints()
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null || terrain.terrainData == null)
                return;

            TerrainLayer[] layers = terrain.terrainData.terrainLayers;
            if (layers == null || layers.Length == 0)
                return;

            if (terrainLayerColorEntries == null || terrainLayerColorEntries.Length == 0)
                return;

            for (int i = 0; i < terrainLayerColorEntries.Length; i++)
            {
                int index = (int)terrainLayerColorEntries[i].LayerName;

                if (index < 0 || index >= layers.Length)
                    continue;

                if (layers[index] == null)
                    continue;

                Color baseColor = terrainLayerColorEntries[i].Color;
                Color correctedColor = new Color(
                    baseColor.r * terrainTintCompensation.r,
                    baseColor.g * terrainTintCompensation.g,
                    baseColor.b * terrainTintCompensation.b,
                    baseColor.a
                );

                layers[index].diffuseRemapMax = correctedColor;
            }

            terrain.terrainData.terrainLayers = layers;
        }

        private static ColorScheme.TerrainLayerColorEntry[] CloneEntries(ColorScheme.TerrainLayerColorEntry[] source)
        {
            if (source == null)
                return null;

            ColorScheme.TerrainLayerColorEntry[] clone = new ColorScheme.TerrainLayerColorEntry[source.Length];

            for (int i = 0; i < source.Length; i++)
                clone[i] = source[i];

            return clone;
        }

        private static ColorScheme.TreeColorEntry[] CloneEntries(ColorScheme.TreeColorEntry[] source)
        {
            if (source == null)
                return null;

            ColorScheme.TreeColorEntry[] clone = new ColorScheme.TreeColorEntry[source.Length];

            for (int i = 0; i < source.Length; i++)
                clone[i] = source[i];

            return clone;
        }
    }
}