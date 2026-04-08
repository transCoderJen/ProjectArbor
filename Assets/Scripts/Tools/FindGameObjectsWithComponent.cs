using UnityEngine;

public class FindGameObjectsWithComponent : MonoBehaviour
{
    [ContextMenu("Find AudioListeners")]
    public void FindAudioListeners()
    {
        FindComponents<AudioListener>();
    }

    public void FindComponents<T>() where T : Component
    {
        T[] components = FindObjectsByType<T>(FindObjectsSortMode.None);

        if (components.Length == 0)
        {
            Debug.Log($"No {typeof(T).Name} found in scene.");
            return;
        }

        Debug.Log($"Found {components.Length} {typeof(T).Name}(s):");

        foreach (T comp in components)
        {
            string hierarchy = GetHierarchyPath(comp.gameObject);
            Debug.Log(hierarchy);
        }
    }

    private string GetHierarchyPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}