using ShiftedSignal.Garden.Managers;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ColorManager))]
public class ColorManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Color Scheme Tools", EditorStyles.boldLabel);

        ColorManager colorManager = (ColorManager)target;

        GUI.enabled = !Application.isPlaying;

        if (GUILayout.Button("Overwrite Existing Color Scheme"))
        {
            colorManager.OverwriteExistingColorScheme();
            EditorUtility.SetDirty(colorManager);
        }

        if (GUILayout.Button("Save New Color Scheme"))
        {
            colorManager.SaveNewColorScheme();
            EditorUtility.SetDirty(colorManager);
        }


        GUI.enabled = true;
    }
}