# material_set_texture_scale

Set the texture scale (UV tiling) for a texture property.

**Signature:** `MaterialSetTextureScale(string name = null, int instanceId = 0, string path = null, string propertyName = null, float x = 1, float y = 1)`

**Returns:** `{ success, target, property, scale: {x,y} }`

## Notes

- `propertyName` auto-detects the main texture property for the active pipeline if omitted (`_BaseMap` for URP, `_BaseColorMap` for HDRP, `_MainTex` for Standard).
- Scale defaults are `x = 1, y = 1` (one tile). A value of `2` tiles the texture twice.
- Use `material_set_texture_offset` to control the starting UV position independently.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name         = "Floor";
        int    instanceId   = 0;
        string path         = null;
        string propertyName = null;
        float  x            = 4f;
        float  y            = 4f;

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        if (string.IsNullOrEmpty(propertyName)) propertyName = GetMainTexProp();

        WorkflowManager.SnapshotObject(material);
        Undo.RecordObject(material, "Set Texture Scale");
        material.SetTextureScale(propertyName, new Vector2(x, y));

        if (go == null) EditorUtility.SetDirty(material);

        result.SetResult(new { success = true, target = go != null ? go.name : path, property = propertyName, scale = new { x, y } });
    }

    private static string GetMainTexProp()
    {
        var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (rp != null)
        {
            var t = rp.GetType().FullName ?? "";
            if (t.Contains("Universal")) return "_BaseMap";
            if (t.Contains("HDRP") || t.Contains("HDRenderPipeline")) return "_BaseColorMap";
        }
        return "_MainTex";
    }

    private static (Material mat, GameObject go, object error) FindMaterial(string name, int instanceId, string path)
    {
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".mat"))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) return (null, null, new { error = "Material asset not found: " + path });
            return (m, null, null);
        }
        var (go, err) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (err != null) return (null, null, err);
        var rdr = go.GetComponent<Renderer>();
        if (rdr == null) return (null, go, new { error = "No Renderer on " + go.name });
        var mat = rdr.sharedMaterial;
        if (mat == null) return (null, go, new { error = "No material on " + go.name });
        return (mat, go, null);
    }
}
```
