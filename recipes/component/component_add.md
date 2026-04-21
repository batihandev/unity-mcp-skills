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
                return new {
                    error = $"Component type not found: {componentType}",
                    hint = "Try using full type name like 'CinemachineVirtualCamera' or 'Unity.Cinemachine.CinemachineCamera'",
                    availableTypes = GetSimilarTypes(componentType)
                };

            // Check if component already exists (for single-instance components)
            if (go.GetComponent(type) != null && !AllowMultiple(type))
                return new {
                    warning = $"Component {type.Name} already exists on {go.name}",
                    gameObject = go.name,
                    instanceId = go.GetInstanceID()
                };

            var comp = Undo.AddComponent(go, type);

            if (WorkflowManager.IsRecording)
                WorkflowManager.SnapshotCreatedComponent(comp);

            EditorUtility.SetDirty(go);

            return new {
                success = true,
                gameObject = go.name,
                instanceId = go.GetInstanceID(),
                component = type.Name,
                fullTypeName = type.FullName
            };
        */
    }
}
```
