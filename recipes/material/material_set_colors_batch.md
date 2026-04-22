# material_set_colors_batch

Set colors on multiple GameObjects or material assets in a single call (efficient batch operation).

**Signature:** `MaterialSetColorsBatch(string items = null, string propertyName = null)`

**`items`:** JSON array of `{ name?, instanceId?, path?, r?, g?, b?, a? }` objects. Color defaults are 1.0 for all channels.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target }] }`

## Notes

- `propertyName` applies to all items; auto-detected from the active render pipeline if omitted.
- Per-item fallback: tries `propertyName → _BaseColor → _Color` in order.
- Color channels are in the **0–1** range (not 0–255).
- Prefer this over calling `material_set_color` repeatedly when setting colors on 2+ objects.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder.FindOrError`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.SnapshotObject`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchColorItem
{
    public string name;
    public int instanceId;
    public string path;
    public float r = 1f, g = 1f, b = 1f, a = 1f;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string propertyName = null; // null → probe _BaseColor / _Color

        var items = new[]
        {
            new _BatchColorItem { name = "Cube", r = 1f, g = 0f, b = 0f, a = 1f },
            new _BatchColorItem { name = "Sphere", r = 0f, g = 1f, b = 0f, a = 1f },
            new _BatchColorItem { name = "Plane", r = 0f, g = 0f, b = 1f, a = 1f },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var target = item.name ?? item.path ?? ("#" + item.instanceId);
            Material material = null;
            GameObject go = null;

            if (!string.IsNullOrEmpty(item.path) && item.path.EndsWith(".mat"))
            {
                material = AssetDatabase.LoadAssetAtPath<Material>(item.path);
            }
            else
            {
                var (foundGo, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
                if (err != null) { results.Add(new { target, success = false, error = "Object not found" }); failCount++; continue; }
                go = foundGo;
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null) { results.Add(new { target, success = false, error = "No Renderer" }); failCount++; continue; }
                material = renderer.sharedMaterial;
            }

            if (material == null) { results.Add(new { target, success = false, error = "No material" }); failCount++; continue; }

            var color = new Color(item.r, item.g, item.b, item.a);

            WorkflowManager.SnapshotObject(material);
            Undo.RecordObject(material, "Batch Set Color");

            bool colorSet = false;
            var propertiesToTry = new[] { propertyName, "_BaseColor", "_Color" };
            foreach (var prop in propertiesToTry)
            {
                if (string.IsNullOrEmpty(prop)) continue;
                if (material.HasProperty(prop))
                {
                    material.SetColor(prop, color);
                    colorSet = true;
                    break;
                }
            }

            if (!colorSet) { results.Add(new { target, success = false, error = "No color property found" }); failCount++; continue; }

            if (go == null) EditorUtility.SetDirty(material);
            results.Add(new { target = go?.name ?? item.path, success = true });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
