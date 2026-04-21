# physics_set_material

Set PhysicMaterial on a collider. Supports lookup by name, instanceId, or hierarchy path.

**Signature:** `PhysicsSetMaterial(string materialPath, string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, gameObject, material }`

> Unity 6+: loads `PhysicsMaterial`; older versions: `PhysicMaterial`.

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

#if UNITY_6000_0_OR_NEWER
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(materialPath);
#else
        var mat = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(materialPath);
#endif
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
