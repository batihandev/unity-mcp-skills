# xr_list_interactors

Lists all XR interactors in the scene (XRRayInteractor, XRDirectInteractor, XRSocketInteractor, NearFarInteractor) with type, path, and enabled state.

**Signature:** `XRListInteractors(verbose bool = false)`

**Returns:** `{ success, count, interactors, xriVersion }`

**Notes:**
- Pass `verbose=true` to include a `properties` map for each interactor entry.
- Read-only; does not modify the scene.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        #if !XRI
                    { result.SetResult(NoXRI()); return; }
        #else
                    var interactorTypes = new[] { "XRRayInteractor", "XRDirectInteractor", "XRSocketInteractor", "NearFarInteractor" };
                    var results = new List<object>();

                    foreach (var typeName in interactorTypes)
                    {
                        var found = XRReflectionHelper.FindComponentsOfXRType(typeName);
                        foreach (var comp in found)
                        {
                            var entry = new Dictionary<string, object>
                            {
                                ["type"] = comp.GetType().Name,
                                ["gameObject"] = comp.gameObject.name,
                                ["instanceId"] = comp.gameObject.GetInstanceID(),
                                ["path"] = GameObjectFinder.GetPath(comp.gameObject),
                                ["enabled"] = comp is Behaviour b ? b.enabled : true
                            };

                            if (verbose)
                                entry["properties"] = XRReflectionHelper.GetComponentInfo(comp);

                            results.Add(entry);
                        }
                    }

                    { result.SetResult(new
                    {
                        success = true,
                        count = results.Count,
                        interactors = results,
                        xriVersion = XRReflectionHelper.XRIMajorVersion
                    }); return; }
        #endif
    }
}
```
