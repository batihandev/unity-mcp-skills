# physics_set_material

Set PhysicMaterial on a collider. Supports lookup by name, instanceId, or hierarchy path.

**Signature:** `PhysicsSetMaterial(string materialPath, string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, gameObject, material }`

> Unity 6+: loads `PhysicsMaterial`; older versions: `PhysicMaterial`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string materialPath = "Assets/Materials/Bouncy.physicMaterial";
        string name = "Cube"; // or set instanceId / path instead
        int instanceId = 0;
        string path = null;

        var (go, err) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var collider = go.GetComponent<Collider>();
        if (collider == null)
        {
            result.SetResult(new { error = $"No Collider on {go.name}" });
            return;
        }

        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(materialPath);
        if (mat == null)
        {
            result.SetResult(new { error = $"PhysicMaterial not found: {materialPath}" });
            return;
        }

        WorkflowManager.SnapshotObject(collider);
        Undo.RecordObject(collider, "Set PhysicMaterial");
        collider.sharedMaterial = mat;
        result.SetResult(new { success = true, gameObject = go.name, material = materialPath });
    }
}
```
