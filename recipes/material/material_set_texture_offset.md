# material_set_texture_offset

Set the texture offset (UV scroll position) for a texture property.

**Signature:** `MaterialSetTextureOffset(string name = null, int instanceId = 0, string path = null, string propertyName = null, float x = 0, float y = 0)`

**Returns:** `{ success, target, property, offset: {x,y} }`

## Notes

- `propertyName` auto-detects the main texture property for the active pipeline if omitted (`_BaseMap` for URP, `_BaseColorMap` for HDRP, `_MainTex` for Standard).
- Offset values are in UV space (0–1 = one full tile).
- Use `material_set_texture_scale` to control tiling count independently.

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
        float  x            = 0.5f;     // horizontal offset (UV space)
        float  y            = 0.0f;     // vertical offset

        /* Original Logic:

            var (material, go, error) = FindMaterial(name, instanceId, path);
            if (error != null) return error;

            if (string.IsNullOrEmpty(propertyName))
                propertyName = ProjectSkills.GetMainTexturePropertyName();

            WorkflowManager.SnapshotObject(material);
            Undo.RecordObject(material, "Set Texture Offset");
            material.SetTextureOffset(propertyName, new Vector2(x, y));

            if (go == null) EditorUtility.SetDirty(material);

            return new { success = true, target = go != null ? go.name : path, property = propertyName, offset = new { x, y } };
        */
    }
}
```
