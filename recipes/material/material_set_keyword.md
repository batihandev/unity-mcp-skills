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

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

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
}
```
