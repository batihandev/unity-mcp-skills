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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md), [`component_type_finder`](../_shared/component_type_finder.md), [`skills_common`](../_shared/skills_common.md)

## C# Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null; int instanceId = 0; string path = null;
        string componentType = "Rigidbody";
        int componentIndex = 0;

        if (Validate.Required(componentType, "componentType") is object err) { result.SetResult(err); return; }

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var type = ComponentSkills.FindComponentType(componentType);
        if (type == null)
            { result.SetResult(new { error = $"Component type not found: {componentType}" }); return; }

        var components = go.GetComponents(type);
        if (components.Length == 0)
            { result.SetResult(new { error = $"Component not found on {go.name}: {componentType}" }); return; }

        if (componentIndex >= components.Length)
            { result.SetResult(new { error = $"Component index {componentIndex} out of range. Found {components.Length} components of type {componentType}" }); return; }

        var comp = components[componentIndex];

        var requiredBy = GetRequiredByComponents(go, type);
        if (requiredBy.Any())
            { result.SetResult(new {
                error = $"Cannot remove {componentType} - required by: {string.Join(", ", requiredBy)}",
                hint = "Remove dependent components first"
            }); return; }

        WorkflowManager.SnapshotObject(comp);
        Undo.DestroyObjectImmediate(comp);
        EditorUtility.SetDirty(go);

        result.SetResult(new { success = true, gameObject = go.name, removed = componentType });
    }

    private static string[] GetRequiredByComponents(UnityEngine.GameObject go, System.Type typeToRemove)
    {
        var required = new System.Collections.Generic.List<string>();
        foreach (var comp in go.GetComponents<Component>())
        {
            if (comp == null) continue;
            foreach (var attr in comp.GetType().GetCustomAttributes(typeof(RequireComponent), true))
            {
                var rc = (RequireComponent)attr;
                if (rc.m_Type0 == typeToRemove || rc.m_Type1 == typeToRemove || rc.m_Type2 == typeToRemove)
                    required.Add(comp.GetType().Name);
            }
        }
        return required.ToArray();
    }
}
```
