# navmesh_set_obstacle

Set `NavMeshObstacle` properties. Pass only the parameters you want to change; omitted parameters are left unchanged.

**Signature:** `NavMeshSetObstacle(string name = null, int instanceId = 0, string path = null, string shape = null, float? sizeX = null, float? sizeY = null, float? sizeZ = null, bool? carving = null)`

**Returns:** `{ success, gameObject, shape, carving }`

Valid `shape` values: `Box`, `Capsule`

```csharp
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Provide at least one of: name, instanceId, or path
        string name = "Wall";
        int instanceId = 0;
        string path = null;

        // Only set the values you want to change; leave others null
        string shape = "Box";       // "Box" or "Capsule"
        float? sizeX = 2f;
        float? sizeY = null;
        float? sizeZ = null;
        bool? carving = true;

        var (go, err) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var obs = go.GetComponent<NavMeshObstacle>();
        if (obs == null) { result.SetResult(new { error = $"No NavMeshObstacle on {go.name}" }); return; }

        WorkflowManager.SnapshotObject(obs);
        Undo.RecordObject(obs, "Set NavMeshObstacle");

        if (!string.IsNullOrEmpty(shape) && System.Enum.TryParse<NavMeshObstacleShape>(shape, true, out var s))
            obs.shape = s;

        if (sizeX.HasValue || sizeY.HasValue || sizeZ.HasValue)
        {
            var sz = obs.size;
            obs.size = new Vector3(sizeX ?? sz.x, sizeY ?? sz.y, sizeZ ?? sz.z);
        }

        if (carving.HasValue) obs.carving = carving.Value;

        result.SetResult(new { success = true, gameObject = go.name, shape = obs.shape.ToString(), carving = obs.carving });
    }
}
```
