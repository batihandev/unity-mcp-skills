# material_assign_batch

Assign materials to multiple GameObjects in a single call via a typed item array.

**Signature:** `MaterialAssignBatch(_BatchMaterialAssignItem[] items)`

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, name, materialPath }] }`

## Notes
- Each item resolves its target GameObject via `name`, `instanceId`, or `path` — at least one must be provided.
- Prefer this over calling `material_assign` repeatedly when assigning to 2+ objects.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchMaterialAssignItem
{
    public string name;
    public int instanceId;
    public string path;
    public string materialPath;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchMaterialAssignItem { name = "Cube",   materialPath = "Assets/Materials/Red.mat" },
            new _BatchMaterialAssignItem { name = "Sphere", materialPath = "Assets/Materials/Blue.mat" },
        };

        var materialCache = new Dictionary<string, Material>();
        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.materialPath))
            { results.Add(new { success = false, name = item.name, error = "materialPath required" }); failCount++; continue; }

            var (go, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (err != null) { results.Add(new { success = false, name = item.name ?? item.path, error = err }); failCount++; continue; }

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            { results.Add(new { success = false, name = go.name, error = "No Renderer component" }); failCount++; continue; }

            if (!materialCache.TryGetValue(item.materialPath, out var material))
            {
                material = AssetDatabase.LoadAssetAtPath<Material>(item.materialPath);
                if (material != null) materialCache[item.materialPath] = material;
            }
            if (material == null)
            { results.Add(new { success = false, name = go.name, error = "Material not found: " + item.materialPath }); failCount++; continue; }

            WorkflowManager.SnapshotObject(renderer);
            Undo.RecordObject(renderer, "Batch Assign Material");
            renderer.sharedMaterial = material;

            results.Add(new { success = true, name = go.name, materialPath = item.materialPath });
            successCount++;
        }

        result.SetResult(new { success = true, totalItems = items.Length, successCount, failCount, results });
    }
}
```
