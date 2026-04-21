# xr_add_socket_interactor

Adds XRSocketInteractor and a trigger SphereCollider (if no collider is present) to a GameObject for snap-to-slot object placement.

**Signature:** `XRAddSocketInteractor(name string = null, instanceId int = 0, path string = null, showHoverMesh bool = true, recycleDelay float = 1.0)`

**Returns:** `{ success, name, instanceId, interactorType, showHoverMesh, recycleDelay }`

**Notes:**
- Auto-added SphereCollider has `isTrigger = true` and `radius = 0.15`.
- `recycleDelay`: seconds before the socket can accept another object after release.
- `showHoverMesh` controls whether a ghost mesh appears when an interactable hovers over the socket.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "ItemSlot";
        bool showHoverMesh = true;
        float recycleDelay = 1f;

        var res = UnitySkillsBridge.Call("xr_add_socket_interactor", new {
            name, showHoverMesh, recycleDelay
        });
        result.SetResult(res);
    }
}
```
