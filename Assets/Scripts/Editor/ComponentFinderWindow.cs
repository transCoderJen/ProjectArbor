using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ShiftedSignal.Garden.EditorTools
{
    public class ComponentFinderWindow : EditorWindow
    {
        private string componentName = "AudioListener";
        private bool includeInactive = true;

        [MenuItem("Tools/Project/Find Components/Search")]
        public static void Open()
        {
            GetWindow<ComponentFinderWindow>("Component Finder");
        }

        private void OnGUI()
        {
            GUILayout.Label("Find GameObjects With Component", EditorStyles.boldLabel);

            componentName = EditorGUILayout.TextField("Component Name", componentName);
            includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

            if (GUILayout.Button("Find"))
                FindComponentsByName(componentName, includeInactive);
        }

        private static void FindComponentsByName(string searchName, bool includeInactive)
        {
            if (string.IsNullOrWhiteSpace(searchName))
            {
                Debug.LogWarning("Enter a component type name first.");
                return;
            }

            Type componentType = FindComponentType(searchName);

            if (componentType == null)
            {
                Debug.LogError($"Could not find component type: {searchName}");
                return;
            }

            Component[] components = UnityEngine.Object.FindObjectsByType(
                componentType,
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None) as Component[];

            if (components == null || components.Length == 0)
            {
                Debug.Log($"No {componentType.Name} found in open scene.");
                return;
            }

            Debug.Log($"Found {components.Length} {componentType.Name}(s):");

            foreach (Component component in components)
            {
                Debug.Log(
                    $"{componentType.Name}: {GetHierarchyPath(component.gameObject)}",
                    component.gameObject);
            }
        }

        private static Type FindComponentType(string typeName)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .FirstOrDefault(type =>
                    typeof(Component).IsAssignableFrom(type) &&
                    (type.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                     type.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase)));
        }

        private static string GetHierarchyPath(GameObject obj)
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
}