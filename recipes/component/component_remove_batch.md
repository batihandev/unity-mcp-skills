# component_remove_batch

Remove components from multiple GameObjects in a single call. Removes all instances of the specified type from each target object.

**Signature:** `ComponentRemoveBatch(string items)`

### Batch Item Schema

```json
[
  { "name": "Enemy1", "componentType": "Rigidbody" },
  { "path": "Level/Prop", "componentType": "AudioSource" }
]
```

Each item supports: `name`, `instanceId`, `path` (at least one required), and `componentType` (required).

## Returns

```json
{
  "success": true,
  "totalItems": 2,
  "successCount": 2,
  "failCount": 0,
  "results": [
    { "target": "Enemy1", "success": true, "removed": "Rigidbody", "count": 1 },
    { "target": "Prop", "success": true, "removed": "AudioSource", "count": 2 }
  ]
}
```

The `count` field reports how many instances of the component were removed from that object.

## Notes

- Removes ALL instances of the component type on each target object (unlike single `component_remove` which supports `componentIndex`).
- Each item is processed independently; failures in one item do not block others.
- Snapshots each component for workflow undo before removing.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md), [`component_type_finder`](../_shared/component_type_finder.md), [`skills_common`](../_shared/skills_common.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchRemoveComponentItem
{
    public string name;
    public int instanceId;
    public string path;
    public string componentType;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchRemoveComponentItem { name = "Enemy1", componentType = "Rigidbody" },
            new _BatchRemoveComponentItem { path = "Level/Prop", componentType = "AudioSource" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var target = item.name ?? item.path ?? ("#" + item.instanceId);
            var (go, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (err != null) { results.Add(new { target, success = false, error = "Object not found" }); failCount++; continue; }

            if (string.IsNullOrEmpty(item.componentType))
            { results.Add(new { target, success = false, error = "componentType required" }); failCount++; continue; }

            var type = ComponentSkills.FindComponentType(item.componentType);
            if (type == null)
            { results.Add(new { target, success = false, error = "Component type not found: " + item.componentType }); failCount++; continue; }

            var components = go.GetComponents(type);
            if (components.Length == 0)
            { results.Add(new { target, success = false, error = "Component not found: " + item.componentType }); failCount++; continue; }

            Undo.RecordObject(go, "Batch Remove Component");
            foreach (var c in components)
            {
                WorkflowManager.SnapshotObject(c);
                Undo.DestroyObjectImmediate(c);
            }

            EditorUtility.SetDirty(go);
            results.Add(new { target = go.name, success = true, removed = type.Name, count = components.Length });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
