using UnityEditor;
using UnityEngine;

public static class FR5MaterialShaderRepairTool
{
    [MenuItem("FR5 Tools/Repair Missing Material Shaders To Standard")]
    public static void RepairMissingShadersToStandard()
    {
        Shader standardShader = Shader.Find("Standard");

        if (standardShader == null)
        {
            Debug.LogError("[FR5MaterialShaderRepairTool] Standard shader was not found.");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material");

        int checkedCount = 0;
        int repairedCount = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                continue;
            }

            checkedCount++;

            if (!IsBrokenShader(material))
            {
                continue;
            }

            Undo.RecordObject(material, "Repair Missing Material Shader");

            material.shader = standardShader;

            if (material.HasProperty("_Color"))
            {
                material.color = Color.gray;
            }

            EditorUtility.SetDirty(material);
            repairedCount++;

            Debug.Log($"[FR5MaterialShaderRepairTool] Repaired asset material: {path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FR5MaterialShaderRepairTool] Asset repair done. Checked={checkedCount}, Repaired={repairedCount}");
    }

    [MenuItem("FR5 Tools/Repair Scene Renderer Materials To Standard")]
    public static void RepairSceneRendererMaterialsToStandard()
    {
        Shader standardShader = Shader.Find("Standard");

        if (standardShader == null)
        {
            Debug.LogError("[FR5MaterialShaderRepairTool] Standard shader was not found.");
            return;
        }

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        int rendererCount = 0;
        int materialSlotCount = 0;
        int repairedCount = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            rendererCount++;

            Material[] materials = renderer.sharedMaterials;
            bool rendererChanged = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                materialSlotCount++;

                if (material == null)
                {
                    Material newMaterial = new Material(standardShader);
                    newMaterial.name = $"{renderer.gameObject.name}_AutoStandard";
                    newMaterial.color = Color.gray;

                    materials[i] = newMaterial;
                    rendererChanged = true;
                    repairedCount++;

                    Debug.Log($"[FR5MaterialShaderRepairTool] Created material for empty slot: {GetPath(renderer.gameObject)} / slot {i}");
                    continue;
                }

                if (!IsBrokenShader(material))
                {
                    continue;
                }

                Undo.RecordObject(material, "Repair Scene Renderer Material Shader");

                material.shader = standardShader;

                if (material.HasProperty("_Color"))
                {
                    material.color = Color.gray;
                }

                EditorUtility.SetDirty(material);
                rendererChanged = true;
                repairedCount++;

                Debug.Log($"[FR5MaterialShaderRepairTool] Repaired scene material: {GetPath(renderer.gameObject)} / slot {i} / material={material.name}");
            }

            if (rendererChanged)
            {
                Undo.RecordObject(renderer, "Repair Renderer Materials");
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        Debug.Log($"[FR5MaterialShaderRepairTool] Scene renderer repair done. Renderers={rendererCount}, MaterialSlots={materialSlotCount}, Repaired={repairedCount}");
    }

    private static bool IsBrokenShader(Material material)
    {
        if (material == null)
        {
            return true;
        }

        if (material.shader == null)
        {
            return true;
        }

        string shaderName = material.shader.name;

        return shaderName == "Hidden/InternalErrorShader" ||
               shaderName.ToLower().Contains("error");
    }

    private static string GetPath(GameObject obj)
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