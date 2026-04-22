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
- `recipes/_shared/component_type_finder.md` — for `ComponentSkills.FindComponentType` (transitively needs `skills_common.md`)
- `recipes/_shared/skills_common.md` — required by `component_type_finder.md` for `SkillsCommon.GetAllLoadedTypes`

## C# Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _AddComponentItem
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
            new _AddComponentItem { name = "Enemy1", componentType = "Rigidbody" },
            new _AddComponentItem { path = "Level/Obstacles/Rock", componentType = "MeshCollider" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var target = item.name ?? item.path ?? ("#" + item.instanceId);

            var (go, findErr) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (findErr != null) { results.Add(new { target, success = false, error = "Object not found" }); failCount++; continue; }

            if (string.IsNullOrEmpty(item.componentType))
            { results.Add(new { target, success = false, error = "componentType required" }); failCount++; continue; }

            var type = ComponentSkills.FindComponentType(item.componentType);
            if (type == null)
            { results.Add(new { target, success = false, error = "Component type not found: " + item.componentType }); failCount++; continue; }

            if (go.GetComponent(type) != null && !AllowMultiple(type))
            { results.Add(new { target = go.name, success = true, warning = "Component already exists", component = type.Name }); successCount++; continue; }

            var comp = Undo.AddComponent(go, type);
            if (WorkflowManager.IsRecording) WorkflowManager.SnapshotCreatedComponent(comp);
            EditorUtility.SetDirty(go);

            results.Add(new { target = go.name, success = true, component = type.Name });
            successCount++;
        }

        result.SetResult(new
        {
            success = failCount == 0,
            totalItems = items.Length,
            successCount,
            failCount,
            results
        });
    }

    private static bool AllowMultiple(System.Type type)
    {
        try { return type.GetCustomAttributes(typeof(DisallowMultipleComponent), true).Length == 0; }
        catch { return true; }
    }
}
```
