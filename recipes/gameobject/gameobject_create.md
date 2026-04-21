# gameobject_create

Create a new GameObject (primitive or empty).

**Signature:** `GameObjectCreate(string name, string primitiveType = null, float x = 0, float y = 0, float z = 0, string parentName = null, int parentInstanceId = 0, string parentPath = null)`

**Returns:** `{ success, name, instanceId, path, parent, position: { x, y, z } }`

## Notes

- `primitiveType`: `Cube`, `Sphere`, `Capsule`, `Cylinder`, `Plane`, `Quad`, or `Empty`/`null` for an empty GameObject.
- Parent is resolved before the object is created — fails fast if the parent does not exist.
- Position is set as `localPosition` relative to the parent (or world origin if no parent).

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyObject";
        string primitiveType = "Cube"; // null or "Empty" for an empty GameObject
        float x = 0f, y = 0f, z = 0f;
        string parentName = null; // optional: parent object name
        int parentInstanceId = 0; // optional: parent instance ID (preferred)
        string parentPath = null; // optional: parent hierarchy path

        // Resolve parent first so we fail fast before creating the object
        GameObject parentGo = null;
        if (!string.IsNullOrEmpty(parentName) || parentInstanceId != 0 || !string.IsNullOrEmpty(parentPath))
        {
            var (found, parentErr) = GameObjectFinder.FindOrError(parentName, parentInstanceId, parentPath);
            if (parentErr != null) { result.SetResult(parentErr); return; }
            parentGo = found;
        }

        GameObject go;

        if (string.IsNullOrEmpty(primitiveType) ||
            primitiveType.Equals("Empty", System.StringComparison.OrdinalIgnoreCase) ||
            primitiveType.Equals("None", System.StringComparison.OrdinalIgnoreCase))
        {
            go = new GameObject(name);
            primitiveType = null;
        }
        else if (System.Enum.TryParse<PrimitiveType>(primitiveType, true, out var pt))
        {
            go = GameObject.CreatePrimitive(pt);
            go.name = name;
        }
        else
        {
            result.SetResult(new { error = $"Unknown primitive type: {primitiveType}. Use: Cube, Sphere, Capsule, Cylinder, Plane, Quad, or Empty/None for empty object" });
            return;
        }

        if (parentGo != null)
            go.transform.SetParent(parentGo.transform, false);

        go.transform.localPosition = new Vector3(x, y, z);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        WorkflowManager.SnapshotCreatedGameObject(go, primitiveType);

        result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            path = GameObjectFinder.GetPath(go),
            parent = parentGo != null ? parentGo.name : "(root)",
            position = new { x, y, z }
        });
    }
}
```
