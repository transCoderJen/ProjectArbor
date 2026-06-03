using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShiftedSignal.Garden.Tools
{
    /// <summary>
    /// Replaces every TextMeshPro font in the current scene with the selected font asset.
    /// </summary>
    public class TMPFontReplacer : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset fontAsset;

        public void ReplaceFontsInScene()
        {
            if (fontAsset == null)
            {
                Debug.LogWarning("No TMP font asset assigned.");
                return;
            }

            TMP_Text[] textObjects = FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (TMP_Text textObject in textObjects)
            {
                textObject.font = fontAsset;

#if UNITY_EDITOR
                EditorUtility.SetDirty(textObject);
#endif
            }

            Debug.Log($"Updated {textObjects.Length} TextMeshPro text objects to use {fontAsset.name}.");
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TMPFontReplacer))]
    public class TMPFontReplacerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TMPFontReplacer replacer = (TMPFontReplacer)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Replace TMP Fonts In Scene"))
            {
                replacer.ReplaceFontsInScene();
            }
        }
    }
#endif
}