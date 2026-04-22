# material_set_keyword

Enable or disable a shader keyword on a material.

**Signature:** `MaterialSetKeyword(string name = null, int instanceId = 0, string path = null, string keyword = null, bool enable = true)`

**Returns:** `{ success, target, keyword, enabled, allKeywords }`

## Notes

- `keyword` is required.
- `enable` defaults to `true`; pass `false` to disable the keyword.
- `allKeywords` in the response lists every currently-enabled keyword on the material after the change.

## Common Keywords

| Keyword | Purpose |
|---------|---------|
| `_EMISSION` | Enable emission (required alongside `_EmissionColor`) |
| `_NORMALMAP` | Enable normal map sampling |
| `_METALLICGLOSSMAP` | Enable metallic/gloss texture |
| `_ALPHATEST_ON` | Enable alpha clipping |
| `_ALPHABLEND_ON` | Enable alpha blending |
| `_ALPHAPREMULTIPLY_ON` | Enable premultiplied alpha |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name       = "Cube";       // target GameObject name
        int    instanceId = 0;
        string path       = null;         // or material asset path
        string keyword    = "_EMISSION";  // required
        bool   enable     = true;         // false to disable

        if (Validate.Required(keyword, "keyword") is object err) { result.SetResult(err); return; }

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        WorkflowManager.SnapshotObject(material);
        Undo.RecordObject(material, "Set Material Keyword");

        if (enable)
            material.EnableKeyword(keyword);
        else
            material.DisableKeyword(keyword);

        if (go == null) EditorUtility.SetDirty(material);

        { result.SetResult(new { 
            success = true, 
            target = go != null ? go.name : path, 
            keyword, 
            enabled = enable,
            allKeywords = material.shaderKeywords
        }); return; }
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
