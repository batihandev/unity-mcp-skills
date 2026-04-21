# component_add_batch

Add components to multiple GameObjects in a single call. Use instead of repeated `component_add` calls when operating on 2+ objects.

**Signature:** `ComponentAddBatch(string items)`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `items` | string | Yes | JSON array of batch items |

### Batch Item Schema

```json
[
  { "name": "Enemy1", "componentType": "Rigidbody" },
  { "instanceId": 12346, "componentType": "BoxCollider" },
  { "path": "Level/Obstacles/Rock", "componentType": "MeshCollider" }
]
```

Each item supports: `name`, `instanceId`, `path` (at least one required), and `componentType` (required).

## Returns

```json
{
  "success": true,
  "totalItems": 3,
  "successCount": 3,
  "failCount": 0,
  "results": [
    { "target": "Enemy1", "success": true, "component": "Rigidbody" },
    { "target": "Obstacle", "success": true, "component": "BoxCollider" },
    { "target": "Rock", "success": true, "component": "MeshCollider" }
  ]
}
```

If a component already exists (and disallows multiple), the item returns with a `warning` field instead of failing.

## Notes

- Reduces N API calls to 1 — always prefer batch for 2+ objects.
- Each item is processed independently; failures in one item do not block others.
- Records created components in the workflow snapshot if a workflow is recording.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        { result.SetResult(BatchExecutor.Execute<BatchAddComponentItem>(items, item =>
        {
            var (go, error) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (error != null) throw new System.Exception("Object not found");

            if (string.IsNullOrEmpty(item.componentType))
                throw new System.Exception("componentType required");

            var type = FindComponentType(item.componentType);
            if (type == null)
                throw new System.Exception($"Component type not found: {item.componentType}");

            // Check if component already exists (for single-instance components)
            if (go.GetComponent(type) != null && !AllowMultiple(type))
                return new { target = go.name, success = true, warning = "Component already exists", component = type.Name };

            var comp = Undo.AddComponent(go, type);

            if (WorkflowManager.IsRecording)
                WorkflowManager.SnapshotCreatedComponent(comp);

            EditorUtility.SetDirty(go);
            return new { target = go.name, success = true, component = type.Name };
        }, item => item.name ?? item.path)); return; }
    }
}
```
