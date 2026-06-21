using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptFinder
{
    [MenuItem("Tools/Project/Find Missing Scripts/Prefabs")]
    public static void FindMissingScriptsInPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        int missingCount = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                continue;

            missingCount += CheckGameObject(prefab, path, prefab);
        }

        Debug.Log($"Prefab missing script scan complete. Found {missingCount} missing scripts.");
    }

    [MenuItem("Tools/Project/Find Missing Scripts/Open Scene")]
    public static void FindMissingScriptsInOpenScene()
    {
        int missingCount = 0;

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            missingCount += CheckGameObject(root, activeScene.path, root);
        }

        Debug.Log($"Open scene missing script scan complete. Found {missingCount} missing scripts.");
    }

    [MenuItem("Tools/Project/Find Missing Scripts/All Scenes")]
    public static void FindMissingScriptsInAllScenes()
    {
        string currentScenePath = SceneManager.GetActiveScene().path;

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");

            int missingCount = 0;

            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                GameObject[] roots = scene.GetRootGameObjects();

                foreach (GameObject root in roots)
                {
                    missingCount += CheckGameObject(root, scenePath, root);
                }
            }

            if (!string.IsNullOrEmpty(currentScenePath))
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);

            Debug.Log($"All scenes missing script scan complete. Found {missingCount} missing scripts.");
        }
    }

    private static int CheckGameObject(GameObject root, string assetOrScenePath, Object context)
    {
        int missingCount = 0;

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform transform in transforms)
        {
            Component[] components = transform.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                    continue;

                missingCount++;

                Debug.LogError(
                    $"Missing script found!\nLocation: {assetOrScenePath}\nObject: {GetHierarchyPath(transform)}",
                    context);
            }
        }

        return missingCount;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;

        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}