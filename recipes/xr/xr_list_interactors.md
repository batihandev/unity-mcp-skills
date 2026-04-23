# xr_list_interactors

Lists all XR interactors in the scene (XRRayInteractor, XRDirectInteractor, XRSocketInteractor, NearFarInteractor) with type, path, and enabled state.

**Signature:** `XRListInteractors(verbose bool = false)`

**Returns:** `{ success, count, interactors, xriVersion }`

**Notes:**
- Pass `verbose=true` to include a short `properties` map for each interactor entry.
- Read-only; does not modify the scene.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool verbose = false;

        var results = new List<object>();

        foreach (var ray in UnityEngine.Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var entry = new Dictionary<string, object>
            {
                ["type"] = ray.GetType().Name,
                ["gameObject"] = ray.gameObject.name,
                ["instanceId"] = ray.gameObject.GetInstanceID(),
                ["path"] = GameObjectFinder.GetPath(ray.gameObject),
                ["enabled"] = ray.enabled
            };
            if (verbose)
            {
                entry["maxRaycastDistance"] = ray.maxRaycastDistance;
                entry["lineType"] = ray.lineType.ToString();
                entry["interactionLayers"] = (int)ray.interactionLayers;
            }
            results.Add(entry);
        }

        foreach (var dir in UnityEngine.Object.FindObjectsByType<XRDirectInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var entry = new Dictionary<string, object>
            {
                ["type"] = dir.GetType().Name,
                ["gameObject"] = dir.gameObject.name,
                ["instanceId"] = dir.gameObject.GetInstanceID(),
                ["path"] = GameObjectFinder.GetPath(dir.gameObject),
                ["enabled"] = dir.enabled
            };
            if (verbose)
                entry["interactionLayers"] = (int)dir.interactionLayers;
            results.Add(entry);
        }

        foreach (var sock in UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var entry = new Dictionary<string, object>
            {
                ["type"] = sock.GetType().Name,
                ["gameObject"] = sock.gameObject.name,
                ["instanceId"] = sock.gameObject.GetInstanceID(),
                ["path"] = GameObjectFinder.GetPath(sock.gameObject),
                ["enabled"] = sock.enabled
            };
            if (verbose)
            {
                entry["showInteractableHoverMeshes"] = sock.showInteractableHoverMeshes;
                entry["recycleDelayTime"] = sock.recycleDelayTime;
                entry["socketActive"] = sock.socketActive;
            }
            results.Add(entry);
        }

        foreach (var nf in UnityEngine.Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            results.Add(new Dictionary<string, object>
            {
                ["type"] = nf.GetType().Name,
                ["gameObject"] = nf.gameObject.name,
                ["instanceId"] = nf.gameObject.GetInstanceID(),
                ["path"] = GameObjectFinder.GetPath(nf.gameObject),
                ["enabled"] = nf.enabled
            });
        }

        { result.SetResult(new
        {
            success = true,
            count = results.Count,
            interactors = results,
            xriVersion = 3
        }); return; }
    }
}
```
