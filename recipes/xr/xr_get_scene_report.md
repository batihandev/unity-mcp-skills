# xr_get_scene_report

Generates a comprehensive XR scene diagnostic report listing all XR component types present, their counts, paths, and a summary.

**Signature:** `XRGetSceneReport(verbose bool = false)`

**Returns:** `{ success, xriVersion, totalXRComponents, components, summary }`

**Notes:**
- `summary` contains counts for `interactionManagers`, `origins`, `interactors`, `interactables`, `teleportTargets`.
- Pass `verbose=true` to include a `properties` map for each component entry.

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
                    var report = new Dictionary<string, object>();

                    report["xriVersion"] = XRReflectionHelper.XRIMajorVersion;

                    // Collect all XR component types
                    var componentTypes = new[]
                    {
                        "XRInteractionManager", "XROrigin",
                        "XRRayInteractor", "XRDirectInteractor", "XRSocketInteractor", "NearFarInteractor",
                        "XRGrabInteractable", "XRSimpleInteractable",
                        "TeleportationProvider", "TeleportationArea", "TeleportationAnchor",
                        "ActionBasedContinuousMoveProvider", "ContinuousMoveProvider",
                        "ActionBasedSnapTurnProvider", "SnapTurnProvider",
                        "ActionBasedContinuousTurnProvider", "ContinuousTurnProvider",
                        "TrackedDeviceGraphicRaycaster", "XRUIInputModule",
                        "ActionBasedController", "XRController"
                    };

                    var components = new List<object>();
                    int totalCount = 0;

                    foreach (var typeName in componentTypes)
                    {
                        var found = XRReflectionHelper.FindComponentsOfXRType(typeName);
                        if (found.Length > 0)
                        {
                            totalCount += found.Length;
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

                                components.Add(entry);
                            }
                        }
                    }

                    report["totalXRComponents"] = totalCount;
                    report["components"] = components;

                    // Summary counts
                    report["summary"] = new
                    {
                        interactionManagers = XRReflectionHelper.FindComponentsOfXRType("XRInteractionManager").Length,
                        origins = XRReflectionHelper.FindComponentsOfXRType("XROrigin").Length,
                        interactors = XRReflectionHelper.FindComponentsOfXRType("XRBaseInteractor").Length,
                        interactables = XRReflectionHelper.FindComponentsOfXRType("XRBaseInteractable").Length,
                        teleportTargets =
                            XRReflectionHelper.FindComponentsOfXRType("TeleportationArea").Length +
                            XRReflectionHelper.FindComponentsOfXRType("TeleportationAnchor").Length
                    };

                    report["success"] = true;
                    { result.SetResult(report); return; }
        #endif
    }
}
```
