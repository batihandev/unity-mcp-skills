# component_remove_batch

Remove components from multiple GameObjects in a single call. Removes all instances of the specified type from each target object.

**Signature:** `ComponentRemoveBatch(string items)`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `items` | string | Yes | JSON array of batch items |

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

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        /* Original Logic:

            return BatchExecutor.Execute<BatchRemoveComponentItem>(items, item =>
            {
                var (go, error) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
                if (error != null) throw new System.Exception("Object not found");

                if (string.IsNullOrEmpty(item.componentType))
                    throw new System.Exception("componentType required");

                var type = FindComponentType(item.componentType);
                if (type == null)
                    throw new System.Exception($"Component type not found: {item.componentType}");

                var components = go.GetComponents(type);
                if (components.Length == 0)
                    throw new System.Exception($"Component not found: {item.componentType}");

                Undo.RecordObject(go, "Batch Remove Component");
                foreach (var c in components)
                {
                    WorkflowManager.SnapshotObject(c);
                    Undo.DestroyObjectImmediate(c);
                }

                EditorUtility.SetDirty(go);
                return new { target = go.name, success = true, removed = type.Name, count = components.Length };
            }, item => item.name ?? item.path);
        */
    }
}
```
