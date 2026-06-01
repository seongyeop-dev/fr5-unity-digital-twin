using UnityEditor;
using UnityEngine;

/// <summary>
/// Finds GameObjects in the currently opened scenes that contain missing MonoBehaviour scripts.
/// This tool does not modify or remove anything.
/// </summary>
public static class FR5MissingScriptFinderTool
{
    [MenuItem("FR5 Tools/Find Missing Scripts In Scene")]
    public static void FindMissingScriptsInScene()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        int objectCount = 0;
        int missingObjectCount = 0;
        int missingScriptCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj == null)
            {
                continue;
            }

            objectCount++;

            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);

            if (count <= 0)
            {
                continue;
            }

            missingObjectCount++;
            missingScriptCount += count;

            Debug.LogWarning(
                $"[FR5MissingScriptFinderTool] Missing Script Count={count} | Path={GetHierarchyPath(obj)}",
                obj
            );
        }

        Debug.Log(
            $"[FR5MissingScriptFinderTool] Done. CheckedObjects={objectCount}, MissingObjects={missingObjectCount}, MissingScripts={missingScriptCount}"
        );
    }

    private static string GetHierarchyPath(GameObject obj)
    {
        if (obj == null)
        {
            return "(null)";
        }

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