# smart_scene_query_spatial

Find scene objects within a sphere region centered at a world position, optionally filtered by component.

**Signature:** `SmartSceneQuerySpatial(float x, float y, float z, float radius = 10f, string componentFilter = null, int limit = 50)`

**Returns:** `{ success, count, center: { x, y, z }, radius, results: [{ name, instanceId, path, distance }] }`

**Notes:**
- Uses `Physics.OverlapSphere`; only objects with colliders are detected.
- `x`, `y`, `z` are required world-space coordinates for the sphere center.
- `componentFilter` restricts results to objects that have the named component (case-insensitive).
- Results are ordered by collider discovery; sort by `distance` if order matters.
- Read-only; no undo entry is created.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float x = 0f;                        // sphere center X
        float y = 0f;                        // sphere center Y
        float z = 0f;                        // sphere center Z
        float radius = 10f;                  // search radius
        string componentFilter = "Light";    // null = no filter
        int limit = 50;

        var center = new Vector3(x, y, z);
        var colliders = Physics.OverlapSphere(center, radius);
        var results = new List<object>();
        foreach (var col in colliders)
        {
            if (results.Count >= limit) break;
            var go = col.gameObject;
            if (!string.IsNullOrEmpty(componentFilter))
            {
                var type = GetTypeByName(componentFilter);
                if (type != null && go.GetComponent(type) == null) continue;
            }
            results.Add(new
            {
                name = go.name, instanceId = go.GetInstanceID(),
                path = GameObjectFinder.GetPath(go),
                distance = Vector3.Distance(center, go.transform.position)
            });
        }
        { result.SetResult(new { success = true, count = results.Count, center = new { x, y, z }, radius, results }); return; }
    }
}
```
