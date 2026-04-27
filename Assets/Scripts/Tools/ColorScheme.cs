using System;
using Unity.VisualScripting;
using UnityEngine;

namespace ShiftedSignal.Garden.Tools
{
    
    [CreateAssetMenu(fileName = "NewColorScheme", menuName = "Colors/Color Scheme")]
    public class ColorScheme : ScriptableObject
    {
        [Serializable]
        public struct TerrainLayerColorEntry
        {
            public TerrainLayerName LayerName;

            [ColorUsage(false, true)]
            public Color Color;
        }

        [Serializable]
        public struct TreeColorEntry
        {
            public Material TreeMaterial;
            public Color LeavesColor;
            public Color LeavesBorderColor;
            public Color TrunkColor;
            public Color TrunkBorderColor;
        }

        [ColorUsage(false, true)]
        public Color SelectionBoxBorder = Color.white;

        [Header("Terrain")]
        public TerrainLayerColorEntry[] TerrainLayerColorEntries;
        [ColorUsage(false,true)]
        public Color TerrainTintCompensation;

        [Header("Trees")]
        public TreeColorEntry[] TreeColorEntries;
    }

    public enum TerrainLayerName
    {
        Ground,
        Path
    }
}