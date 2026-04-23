# physics_check_overlap

Check for colliders in a sphere.

**Signature:** `PhysicsCheckOverlap(float x, float y, float z, float radius, int layerMask = -1)`

**Returns:** `{ count, colliders: [{ collider, objectName, path, isTrigger }] }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float x = 0f;
        float y = 0f;
        float z = 0f;
        float radius = 1f;
        int layerMask = -1;

        var position = new Vector3(x, y, z);
        var colliders = Physics.OverlapSphere(position, radius, layerMask);

        var results = colliders.Select(c => new
        {
            collider = c.name,
            objectName = c.gameObject.name,
            path = GameObjectFinder.GetPath(c.gameObject),
            isTrigger = c.isTrigger
        }).ToArray();

        result.SetResult(new { count = results.Length, colliders = results });
    }
}
```
