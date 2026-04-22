# xr_list_interactables

Lists all XR interactables in the scene (XRGrabInteractable, XRSimpleInteractable, TeleportationArea, TeleportationAnchor) with type, path, and selection/hover state.

**Signature:** `XRListInteractables(verbose bool = false)`

**Returns:** `{ success, count, interactables, xriVersion }`

**Notes:**
- TeleportationArea and TeleportationAnchor are included because they implement XRBaseInteractable.
- Pass `verbose=true` to include a short `properties` map for grab and simple interactable entries.
- Read-only; does not modify the scene.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool verbose = false;

        var results = new List<object>();

        foreach (var grab in UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var entry = new Dictionary<string, object>
            {
                ["type"] = grab.GetType().Name,
                ["gameObject"] = grab.gameObject.name,
                ["instanceId"] = grab.gameObject.GetInstanceID(),
                ["path"] = GameObjectFinder.GetPath(grab.gameObject),
                ["enabled"] = grab.enabled,
                ["isSelected"] = grab.isSelected,
                ["isHovered"] = grab.isHovered
            };
            if (verbose)
            {
                entry["movementType"] = grab.movementType.ToString();
                entry["throwOnDetach"] = grab.throwOnDetach;
                entry["smoothPosition"] = grab.smoothPosition;
                entry["smoothRotation"] = grab.smoothRotation;
                entry["selectMode"] = grab.selectMode.ToString();
            }
            results.Add(entry);
        }

        foreach (var simple in UnityEngine.Object.FindObjectsByType<XRSimpleInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var entry = new Dictionary<string, object>
            {
                ["type"] = simple.GetType().Name,
                ["gameObject"] = simple.gameObject.name,
                ["instanceId"] = simple.gameObject.GetInstanceID(),
                ["path"] = GameObjectFinder.GetPath(simple.gameObject),
                ["enabled"] = simple.enabled,
                ["isSelected"] = simple.isSelected,
                ["isHovered"] = simple.isHovered
            };
            if (verbose)
                entry["selectMode"] = simple.selectMode.ToString();
            results.Add(entry);
        }

        // TeleportationArea / TeleportationAnchor are XRBaseInteractable too.
        foreach (var area in UnityEngine.Object.FindObjectsByType<TeleportationArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            results.Add(new Dictionary<string, object>
            {
                ["type"] = area.GetType().Name,
                ["gameObject"] = area.gameObject.name,
                ["instanceId"] = area.gameObject.GetInstanceID(),
                ["path"] = GameObjectFinder.GetPath(area.gameObject),
                ["enabled"] = area.enabled,
                ["matchOrientation"] = area.matchOrientation.ToString()
            });
        }
        foreach (var anchor in UnityEngine.Object.FindObjectsByType<TeleportationAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            results.Add(new Dictionary<string, object>
            {
                ["type"] = anchor.GetType().Name,
                ["gameObject"] = anchor.gameObject.name,
                ["instanceId"] = anchor.gameObject.GetInstanceID(),
                ["path"] = GameObjectFinder.GetPath(anchor.gameObject),
                ["enabled"] = anchor.enabled,
                ["matchOrientation"] = anchor.matchOrientation.ToString()
            });
        }

        { result.SetResult(new
        {
            success = true,
            count = results.Count,
            interactables = results,
            xriVersion = 3
        }); return; }
    }
}
```
