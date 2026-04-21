# material_set_gi_flags

Set global illumination flags on a material.

**Signature:** `MaterialSetGIFlags(string name = null, int instanceId = 0, string path = null, string flags = "RealtimeEmissive")`

**Returns:** `{ success, target, giFlags }`

## Notes

- `flags` defaults to `"RealtimeEmissive"` and is case-insensitive.
- Valid values: `None`, `RealtimeEmissive`, `BakedEmissive`, `EmissiveIsBlack`, `AnyEmissive`.
- Returns an error listing the valid options if an unrecognised value is provided.
- `material_set_emission` sets GI flags automatically; use this skill only when you need direct control.

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
        string name       = "Lantern"; // target GameObject name
        int    instanceId = 0;
        string path       = null;      // or material asset path
        string flags      = "BakedEmissive"; // None | RealtimeEmissive | BakedEmissive | EmissiveIsBlack | AnyEmissive

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        MaterialGlobalIlluminationFlags giFlags;
        if (!System.Enum.TryParse(flags, true, out giFlags))
        {
            { result.SetResult(new { 
                error = $"Invalid GI flags: {flags}",
                validOptions = new[] { "None", "RealtimeEmissive", "BakedEmissive", "EmissiveIsBlack", "AnyEmissive" }
            }); return; }
        }

        WorkflowManager.SnapshotObject(material);
        Undo.RecordObject(material, "Set GI Flags");
        material.globalIlluminationFlags = giFlags;

        if (go == null) EditorUtility.SetDirty(material);

        { result.SetResult(new { 
            success = true, 
            target = go != null ? go.name : path, 
            giFlags = flags
        }); return; }
    }
}
```
