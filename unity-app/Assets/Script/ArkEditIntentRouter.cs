using System;
using UnityEngine;

[Serializable]
public class EditUnityIntent
{
    public string type;
    public string intent;
    public string mode;
    public string target;
    public string color;
    public string material;
    public string asset_name;
}

public class ArkEditIntentRouter : MonoBehaviour
{
    [Header("Edit Materials (Overrides)")]
    public Material metallicGrayMaterial;
    public Material dragonStickerMaterial;

    // This is called by the WebSocket Client, passing the parsed intent and the currently spawned model
    // This is called by the WebSocket Client, passing the parsed intent and the currently spawned model
    public void ApplyEditIntent(EditUnityIntent intent, GameObject currentModel)
    {
        if (currentModel == null || intent == null || string.IsNullOrWhiteSpace(intent.target))
        {
            Debug.LogWarning("[ARK Edit] Ignored: No model loaded or target is empty.");
            return;
        }

        Transform targetPart = FindChildRecursive(currentModel.transform, intent.target);
        if (targetPart == null)
        {
            Debug.LogWarning($"[ARK Edit] Could not find target part containing: '{intent.target}'. Check your model's hierarchy names!");
            return;
        }

        AttachMeshColliderIfMissing(targetPart.gameObject);

        // FIX 1: Look for the renderer on the object OR its children (in case OBJ loader wrapped it in an empty folder)
        var renderer = targetPart.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"[ARK Edit] Found target '{targetPart.name}', but it doesn't have a MeshRenderer to color!");
            return;
        }

        // 1. Asset Swap Logic
        if (intent.mode == "asset_swap" && dragonStickerMaterial != null)
        {
            renderer.material = dragonStickerMaterial;
            return;
        }

        // 2. Full Material Swap Logic
        if (intent.mode == "material" && metallicGrayMaterial != null)
        {
            renderer.material = metallicGrayMaterial;
            return;
        }

        // 3. Simple Color Change Logic
        if (!string.IsNullOrWhiteSpace(intent.color))
        {
            if (ColorUtility.TryParseHtmlString(intent.color, out Color parsed) || TryParseNamedColor(intent.color, out parsed))
            {
                renderer.material = new Material(renderer.material);
                renderer.material.color = parsed;
            }
        }
        
        Debug.Log($"[ARK Edit] Successfully applied edit to: {targetPart.name}");
    }

    private Transform FindChildRecursive(Transform root, string targetName)
    {
        // FIX 2: Make the search "Fuzzy". If the part is named "body_mesh_1", it will now successfully match "body"!
        if (root.name.ToLowerInvariant().Contains(targetName.ToLowerInvariant()))
            return root;

        foreach (Transform child in root)
        {
            var found = FindChildRecursive(child, targetName);
            if (found != null)
                return found;
        }
        return null;
    }

    private void AttachMeshColliderIfMissing(GameObject go)
    {
        var filter = go.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return;
        var mc = go.GetComponent<MeshCollider>();
        if (mc == null) mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = filter.sharedMesh;
        mc.convex = false;
    }

    private bool TryParseNamedColor(string name, out Color color)
    {
        switch (name.Trim().ToLowerInvariant())
        {
            case "red": color = Color.red; return true;
            case "green": color = Color.green; return true;
            case "blue": color = Color.blue; return true;
            case "yellow": color = Color.yellow; return true;
            case "black": color = Color.black; return true;
            case "white": color = Color.white; return true;
            case "gray": color = Color.gray; return true;
            case "cyan": color = Color.cyan; return true;
            case "magenta": color = Color.magenta; return true;
            default: color = Color.white; return false;
        }
    }
}