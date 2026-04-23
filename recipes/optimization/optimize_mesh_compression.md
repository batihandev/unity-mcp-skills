# optimize_mesh_compression

Set mesh compression for all 3D model assets (`t:Model`) matching an optional filter. Skips models that already use the target compression level.

**Signature:** `OptimizeMeshCompression(string compressionLevel = "Medium", string filter = "")`

**Returns:** `{ success, count, compression, modified }`

- `compressionLevel` — `Off` | `Low` | `Medium` | `High` (case-insensitive; invalid values fall back to `Medium`)
- `modified` — array of `{ path, name }` for each reimported asset

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string compressionLevel = "Medium"; // Off / Low / Medium / High
        string filter = "";                 // Extra AssetDatabase filter (e.g. "Environment")

        ModelImporterMeshCompression comp;
        switch (compressionLevel.ToLower())
        {
            case "off":    comp = ModelImporterMeshCompression.Off;    break;
            case "low":    comp = ModelImporterMeshCompression.Low;    break;
            case "high":   comp = ModelImporterMeshCompression.High;   break;
            default:       comp = ModelImporterMeshCompression.Medium; break; // "medium" or invalid
        }

        var guids = AssetDatabase.FindAssets("t:Model " + filter);
        var modified = new List<object>();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            if (importer.meshCompression != comp)
            {
                importer.meshCompression = comp;
                importer.SaveAndReimport();
                modified.Add(new { path, name = System.IO.Path.GetFileName(path) });
            }
        }

        result.SetResult(new
        {
            success = true,
            count = modified.Count,
            compression = comp.ToString(),
            modified
        });
    }
}
```
