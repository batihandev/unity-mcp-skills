# xr_add_direct_interactor

Adds XRDirectInteractor for close-range hand grab and a trigger SphereCollider (if no collider is present) to a controller GameObject.

**Signature:** `XRAddDirectInteractor(name string = null, instanceId int = 0, path string = null, radius float = 0.1)`

**Returns:** `{ success, name, instanceId, interactorType, triggerRadius }`

**Notes:**
- The SphereCollider is automatically set to `isTrigger = true`.
- If a Collider already exists on the GameObject it is not replaced.
- Recommended radius range: `0.1–0.25`.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Left Controller";
        float radius = 0.1f;

        var res = UnitySkillsBridge.Call("xr_add_direct_interactor", new { name, radius });
        result.SetResult(res);
    }
}
```
