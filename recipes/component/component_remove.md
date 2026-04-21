# component_remove

Remove a component from a GameObject. Supports removing a specific instance by index when multiple components of the same type exist.

**Signature:** `ComponentRemove(string name = null, int instanceId = 0, string path = null, string componentType = null, int componentIndex = 0)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | No* | null | GameObject name |
| `instanceId` | int | No* | 0 | Instance ID |
| `path` | string | No* | null | Hierarchy path |
| `componentType` | string | Yes | - | Component type to remove |
| `componentIndex` | int | No | 0 | Index when multiple instances exist |

*At least one object identifier required.

## Returns

```json
{
  "success": true,
  "gameObject": "Player",
  "removed": "Rigidbody"
}
```

On failure:
```json
{ "error": "Cannot remove Rigidbody - required by: CharacterController", "hint": "Remove dependent components first" }
```

## Notes

- Uses `Undo.DestroyObjectImmediate` — operation is undoable.
- Will refuse if another component has `[RequireComponent]` dependency on the target type.
- Use `componentIndex` only when the same component type appears multiple times on an object.
- Snapshots the object state for workflow undo before removing.

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        /* Original Logic:

            if (Validate.Required(componentType, "componentType") is object err) return err;

            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) return error;

            var type = FindComponentType(componentType);
            if (type == null)
                return new { error = $"Component type not found: {componentType}" };

            var components = go.GetComponents(type);
            if (components.Length == 0)
                return new { error = $"Component not found on {go.name}: {componentType}" };

            if (componentIndex >= components.Length)
                return new { error = $"Component index {componentIndex} out of range. Found {components.Length} components of type {componentType}" };

            var comp = components[componentIndex];

            var requiredBy = GetRequiredByComponents(go, type);
            if (requiredBy.Any())
                return new {
                    error = $"Cannot remove {componentType} - required by: {string.Join(", ", requiredBy)}",
                    hint = "Remove dependent components first"
                };

            WorkflowManager.SnapshotObject(comp);
            Undo.DestroyObjectImmediate(comp);
            EditorUtility.SetDirty(go);

            return new { success = true, gameObject = go.name, removed = componentType };
        */
    }
}
```
