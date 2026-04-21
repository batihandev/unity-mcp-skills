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
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name         = "Floor";   // target GameObject name
        int    instanceId   = 0;
        string path         = null;      // or material asset path
        string propertyName = null;      // null → auto-detect main texture property
        float  x            = 4f;       // tile 4x horizontally
        float  y            = 4f;       // tile 4x vertically

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        if (string.IsNullOrEmpty(propertyName))
            propertyName = ProjectSkills.GetMainTexturePropertyName();

        WorkflowManager.SnapshotObject(material);
        Undo.RecordObject(material, "Set Texture Scale");
        material.SetTextureScale(propertyName, new Vector2(x, y));

        if (go == null) EditorUtility.SetDirty(material);

        { result.SetResult(new { success = true, target = go != null ? go.name : path, property = propertyName, scale = new { x, y } }); return; }
    }
}
```
