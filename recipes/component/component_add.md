# component_add

Add a component to a GameObject. Supports finding the object by name, instanceId, or path. Works with built-in Unity types, Cinemachine, TextMeshPro, and other third-party components.

**Signature:** `ComponentAdd(string name = null, int instanceId = 0, string path = null, string componentType = null)`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | GameObject name |
| `instanceId` | int | No* | Instance ID (preferred) |
| `path` | string | No* | Hierarchy path |
| `componentType` | string | Yes | Component type name (case-sensitive) |

*At least one object identifier required.

## Returns

```json
{
  "success": true,
  "gameObject": "Player",
  "instanceId": 12345,
  "component": "Rigidbody",
  "fullTypeName": "UnityEngine.Rigidbody"
}
```

If the component already exists (and disallows multiple):
```json
{
  "warning": "Component Rigidbody already exists on Player",
  "gameObject": "Player",
  "instanceId": 12345
}
```

## Notes

- `componentType` is case-sensitive: `Rigidbody` not `rigidbody`, `BoxCollider` not `boxcollider`.
- For custom scripts use the exact class name; if namespaced, use `Namespace.ClassName`.
- For Cinemachine, try `CinemachineVirtualCamera` or `Unity.Cinemachine.CinemachineCamera`.
- Uses `Undo.AddComponent` — operation is undoable in the editor.
- Records the created component in the workflow snapshot if a workflow is recording.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`
- `recipes/_shared/component_type_finder.md` — for `ComponentSkills.FindComponentType`
- `recipes/_shared/skills_common.md` — for `SkillsCommon.GetAllLoadedTypes` (transitive)

## C# Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string componentType = "Rigidbody";

        if (Validate.Required(componentType, "componentType") is object err) { result.SetResult(err); return; }

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var type = ComponentSkills.FindComponentType(componentType);
        if (type == null)
        {
            result.SetResult(new {
                error = $"Component type not found: {componentType}",
                hint = "Try using full type name like 'CinemachineVirtualCamera' or 'Unity.Cinemachine.CinemachineCamera'",
                availableTypes = GetSimilarTypes(componentType)
            });
            return;
        }

        if (go.GetComponent(type) != null && !AllowMultiple(type))
        {
            result.SetResult(new {
                warning = $"Component {type.Name} already exists on {go.name}",
                gameObject = go.name,
                instanceId = go.GetInstanceID()
            });
            return;
        }

        var comp = Undo.AddComponent(go, type);
        if (WorkflowManager.IsRecording)
            WorkflowManager.SnapshotCreatedComponent(comp);
        EditorUtility.SetDirty(go);

        result.SetResult(new {
            success = true,
            gameObject = go.name,
            instanceId = go.GetInstanceID(),
            component = type.Name,
            fullTypeName = type.FullName
        });
    }

    private static string[] GetSimilarTypes(string searchTerm)
    {
        var simpleName = searchTerm.Contains(".") ? searchTerm.Substring(searchTerm.LastIndexOf('.') + 1) : searchTerm;
        return SkillsCommon.GetAllLoadedTypes()
            .Where(t => typeof(Component).IsAssignableFrom(t) &&
                        t.Name.IndexOf(simpleName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            .Take(10)
            .Select(t => t.FullName)
            .ToArray();
    }

    private static bool AllowMultiple(System.Type type)
    {
        try { return type.GetCustomAttributes(typeof(DisallowMultipleComponent), true).Length == 0; }
        catch { return true; }
    }
}
```
