# validate_mesh_collider_convex

Find non-convex MeshColliders in the active scene, which can cause physics performance issues.

**Signature:** `ValidateMeshColliderConvex(limit int = 50)`

**Returns:** `{ success, count, nonConvexColliders: [{ gameObject, path, vertexCount }] }`

**Notes:**
- Non-convex MeshColliders cannot be used on non-kinematic Rigidbodies and incur higher physics cost
- `vertexCount` is 0 if `sharedMesh` is null

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int limit = 50;

        var colliders = FindHelper.FindAll<MeshCollider>()
            .Where(mc => !mc.convex)
            .Take(limit)
            .Select(mc => new
            {
                gameObject = mc.gameObject.name,
                path = GameObjectFinder.GetPath(mc.gameObject),
                vertexCount = mc.sharedMesh != null ? mc.sharedMesh.vertexCount : 0
            })
            .ToArray();

        result.SetResult(new { success = true, count = colliders.Length, nonConvexColliders = colliders });
    }
}
```
