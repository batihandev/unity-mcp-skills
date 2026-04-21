# material_set_texture

Set a texture on a material (auto-detects property name for the active render pipeline).

**Signature:** `MaterialSetTexture(string name = null, int instanceId = 0, string path = null, string texturePath = null, string propertyName = null)`

**Returns:** `{ success, target, texture, propertyUsed }`

## Notes

- `texturePath` is required — must be a valid texture asset path (e.g. `Assets/Textures/Wood.png`).
- `propertyName` auto-detects the main texture property for the active pipeline if omitted (`_BaseMap` for URP, `_BaseColorMap` for HDRP, `_MainTex` for Standard).
- Target is resolved as a material asset path (if `path` ends in `.mat`) or via a GameObject renderer.

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
        string name         = "Cube";                        // target GameObject name
        int    instanceId   = 0;
        string path         = null;                          // or material asset path
        string texturePath  = "Assets/Textures/Wood.png";   // required
        string propertyName = null;                          // null → auto-detect

        if (Validate.Required(texturePath, "texturePath") is object err) { result.SetResult(err); return; }

        // Auto-detect texture property name if not specified
        if (string.IsNullOrEmpty(propertyName))
        {
            propertyName = ProjectSkills.GetMainTexturePropertyName();
        }

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
        if (texture == null)
            { result.SetResult(new { error = $"Texture not found: {texturePath}" }); return; }

        WorkflowManager.SnapshotObject(material);
        Undo.RecordObject(material, "Set Texture");
        material.SetTexture(propertyName, texture);

        if (go == null) EditorUtility.SetDirty(material);

        { result.SetResult(new { 
            success = true, 
            target = go != null ? go.name : path, 
            texture = texturePath,
            propertyUsed = propertyName
        }); return; }
    }
}
```
