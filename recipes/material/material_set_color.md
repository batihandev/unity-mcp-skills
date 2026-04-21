# material_set_color

Set a color property on a material with optional HDR intensity.

**Signature:** `MaterialSetColor(string name = null, int instanceId = 0, string path = null, float r = 1, float g = 1, float b = 1, float a = 1, string propertyName = null, float intensity = 1.0f)`

**Returns:** `{ success, target, color: {r,g,b,a}, intensity, propertyUsed, hdrEnabled }`

## Notes

- Color channels `r`, `g`, `b`, `a` are in the **0–1** range (not 0–255).
- `propertyName` is auto-detected from the active render pipeline when omitted (`_BaseColor` for URP/HDRP, `_Color` for Standard). Falls back through `_BaseColor → _Color → _TintColor → _EmissionColor`.
- When `propertyName` resolves to `_EmissionColor` and `intensity > 0`, the `_EMISSION` keyword is automatically enabled and `globalIlluminationFlags` is set to `RealtimeEmissive`.
- `intensity > 1.0` creates HDR bloom (the stored color values exceed 1.0).
- Target is resolved as a material asset path (if `path` ends in `.mat`) or via a GameObject renderer.

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
        string name         = "Cube";  // target GameObject name
        int    instanceId   = 0;
        string path         = null;    // or material asset path like "Assets/Materials/M.mat"
        float  r = 1f, g = 0f, b = 0f, a = 1f; // red; values 0-1
        string propertyName = null;    // null → auto-detect
        float  intensity    = 1.0f;   // >1 for HDR bloom

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        // Auto-detect color property name if not specified
        if (string.IsNullOrEmpty(propertyName))
        {
            propertyName = ProjectSkills.GetColorPropertyName();
        }

        // Apply HDR intensity (for emission, values > 1 create bloom effect)
        var color = new Color(r, g, b, a);
        if (intensity != 1.0f)
        {
            color = new Color(r * intensity, g * intensity, b * intensity, a);
        }

        WorkflowManager.SnapshotObject(material);
        Undo.RecordObject(material, "Set Material Color");

        // Try setting color with detected property, fallback to common names
        bool colorSet = false;
        var propertiesToTry = new[] { propertyName, "_BaseColor", "_Color", "_TintColor", "_EmissionColor" };

        foreach (var prop in propertiesToTry)
        {
            if (material.HasProperty(prop))
            {
                material.SetColor(prop, color);
                propertyName = prop;
                colorSet = true;
        
                // Smart Emission Handling: Auto-enable emission when setting emission color
                if (prop == "_EmissionColor" && intensity > 0)
                {
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
        
                break;
            }
        }

        if (!colorSet)
        {
            { result.SetResult(new { 
                error = $"Material does not have a color property. Tried: {string.Join(", ", propertiesToTry)}",
                shaderName = material.shader.name,
                suggestion = "Use material_get_properties to see available properties"
            }); return; }
        }

        if (go == null) EditorUtility.SetDirty(material);

        { result.SetResult(new { 
            success = true, 
            target = go != null ? go.name : path, 
            color = new { r, g, b, a },
            intensity,
            propertyUsed = propertyName,
            hdrEnabled = (propertyName == "_EmissionColor" && intensity > 0)
        }); return; }
    }
}
```
