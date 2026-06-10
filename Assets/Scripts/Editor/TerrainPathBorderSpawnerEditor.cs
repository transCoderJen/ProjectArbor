using UnityEditor;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable.Editor
{
    [CustomEditor(typeof(TerrainPathBorderSpawner))]
    public class TerrainPathBorderSpawnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);

            TerrainPathBorderSpawner spawner =
                (TerrainPathBorderSpawner)target;

            GUI.backgroundColor = Color.green;

            if (GUILayout.Button("Generate Path Borders", GUILayout.Height(35)))
            {
                spawner.GeneratePathBorders();

                EditorUtility.SetDirty(spawner);
            }

            GUI.backgroundColor = Color.red;

            if (GUILayout.Button("Clear Spawned Prefabs", GUILayout.Height(35)))
            {
                spawner.ClearSpawnedPrefabs();

                EditorUtility.SetDirty(spawner);
            }

            GUI.backgroundColor = Color.white;
        }
    }
}