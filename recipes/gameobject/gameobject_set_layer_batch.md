# gameobject_set_layer_batch

Set the layer for multiple GameObjects in one call.

**Signature:** `GameObjectSetLayerBatch(string items)`

`items`: JSON array of objects `{ name, instanceId, path, layer, recursive }`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target, layer }] }`

## Notes

- `layer` is a layer name string (e.g., `"UI"`, `"Default"`, `"Ignore Raycast"`). Must match a layer defined in the project.
- `recursive` (bool, default `false`): when `true`, the layer change is applied to the object and all of its children recursively.
- A missing object or invalid layer name causes that item to fail without stopping the rest.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchSetLayerItem
{
    public string name;
    public int instanceId;
    public string path;
    public string layer;
    public bool recursive;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchSetLayerItem { name = "Player", layer = "Player" },
            new _BatchSetLayerItem { name = "EnemyRoot", layer = "Enemy", recursive = true },
            new _BatchSetLayerItem { instanceId = 12345, layer = "Default" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var target = item.name ?? item.path ?? ("#" + item.instanceId);
            var (go, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (err != null) { results.Add(new { target, success = false, error = "Object not found" }); failCount++; continue; }

            int layerId = LayerMask.NameToLayer(item.layer);
            if (layerId == -1) { results.Add(new { target, success = false, error = "Layer not found: " + item.layer }); failCount++; continue; }

            WorkflowManager.SnapshotObject(go);
            Undo.RecordObject(go, "Batch Set Layer");
            go.layer = layerId;

            if (item.recursive)
            {
                foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
                {
                    Undo.RecordObject(child.gameObject, "Batch Set Layer Recursive");
                    child.gameObject.layer = layerId;
                }
            }

            results.Add(new { target = go.name, success = true, layer = item.layer });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
