# xr_list_interactables

Lists all XR interactables in the scene (XRGrabInteractable, XRSimpleInteractable, TeleportationArea, TeleportationAnchor) with type, path, and selection/hover state.

**Signature:** `XRListInteractables(verbose bool = false)`

**Returns:** `{ success, count, interactables, xriVersion }`

**Notes:**
- TeleportationArea and TeleportationAnchor are included because they implement XRBaseInteractable.
- Pass `verbose=true` to include a `properties` map for grab and simple interactable entries.
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
                    var interactableTypes = new[] { "XRGrabInteractable", "XRSimpleInteractable" };
                    var results = new List<object>();

                    foreach (var typeName in interactableTypes)
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
                                ["enabled"] = comp is Behaviour b ? b.enabled : true,
                                ["isSelected"] = (bool)(XRReflectionHelper.GetProperty(comp, "isSelected") ?? false),
                                ["isHovered"] = (bool)(XRReflectionHelper.GetProperty(comp, "isHovered") ?? false)
                            };

                            if (verbose)
                                entry["properties"] = XRReflectionHelper.GetComponentInfo(comp);

                            results.Add(entry);
                        }
                    }

                    // Also find TeleportationArea/Anchor since they are interactables
                    foreach (var typeName in new[] { "TeleportationArea", "TeleportationAnchor" })
                    {
                        var found = XRReflectionHelper.FindComponentsOfXRType(typeName);
                        foreach (var comp in found)
                        {
                            results.Add(new Dictionary<string, object>
                            {
                                ["type"] = comp.GetType().Name,
                                ["gameObject"] = comp.gameObject.name,
                                ["instanceId"] = comp.gameObject.GetInstanceID(),
                                ["path"] = GameObjectFinder.GetPath(comp.gameObject),
                                ["enabled"] = comp is Behaviour b2 ? b2.enabled : true
                            });
                        }
                    }

                    { result.SetResult(new
                    {
                        success = true,
                        count = results.Count,
                        interactables = results,
                        xriVersion = XRReflectionHelper.XRIMajorVersion
                    }); return; }
        #endif
    }
}
```
