# xr_get_scene_report

Generates a comprehensive XR scene diagnostic report listing all XR component types present, their counts, paths, and a summary.

**Signature:** `XRGetSceneReport(verbose bool = false)`

**Returns:** `{ success, xriVersion, totalXRComponents, components, summary }`

**Notes:**
- `summary` contains counts for `interactionManagers`, `origins`, `interactors`, `interactables`, `teleportTargets`.
- Pass `verbose=true` to include a short `properties` map for each component entry.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.UI;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool verbose = false;

        var report = new Dictionary<string, object>();
        report["xriVersion"] = 3;

        // Enumerate all XR component types with a common typed pass.
        var all = new List<Component>();
        void AddAll<T>() where T : Component
        {
            foreach (var c in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                all.Add(c);
        }

        AddAll<XRInteractionManager>();
        AddAll<XROrigin>();
        AddAll<XRRayInteractor>();
        AddAll<XRDirectInteractor>();
        AddAll<XRSocketInteractor>();
        AddAll<NearFarInteractor>();
        AddAll<XRGrabInteractable>();
        AddAll<XRSimpleInteractable>();
        AddAll<TeleportationProvider>();
        AddAll<TeleportationArea>();
        AddAll<TeleportationAnchor>();
        AddAll<ContinuousMoveProvider>();
        AddAll<SnapTurnProvider>();
        AddAll<ContinuousTurnProvider>();
        AddAll<TrackedDeviceGraphicRaycaster>();
        AddAll<XRUIInputModule>();
        AddAll<XRInteractorLineVisual>();

        var components = new List<object>();
        foreach (var comp in all)
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
                entry["properties"] = ComponentInfo(comp);

            components.Add(entry);
        }

        report["totalXRComponents"] = all.Count;
        report["components"] = components;

        report["summary"] = new
        {
            interactionManagers = UnityEngine.Object.FindObjectsByType<XRInteractionManager>(FindObjectsSortMode.None).Length,
            origins = UnityEngine.Object.FindObjectsByType<XROrigin>(FindObjectsSortMode.None).Length,
            interactors = UnityEngine.Object.FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None).Length,
            interactables = UnityEngine.Object.FindObjectsByType<XRBaseInteractable>(FindObjectsSortMode.None).Length,
            teleportTargets =
                UnityEngine.Object.FindObjectsByType<TeleportationArea>(FindObjectsSortMode.None).Length +
                UnityEngine.Object.FindObjectsByType<TeleportationAnchor>(FindObjectsSortMode.None).Length
        };

        report["success"] = true;
        { result.SetResult(report); return; }
    }

    // Short, typed property dump for common XR components.
    private static Dictionary<string, object> ComponentInfo(Component comp)
    {
        var info = new Dictionary<string, object>();
        info["type"] = comp.GetType().Name;
        info["gameObject"] = comp.gameObject.name;
        info["instanceId"] = comp.gameObject.GetInstanceID();
        info["enabled"] = comp is Behaviour b ? b.enabled : true;

        switch (comp)
        {
            case XRRayInteractor ray:
                info["maxRaycastDistance"] = ray.maxRaycastDistance;
                info["lineType"] = ray.lineType.ToString();
                info["interactionLayers"] = (int)ray.interactionLayers;
                break;
            case XRDirectInteractor dir:
                info["interactionLayers"] = (int)dir.interactionLayers;
                break;
            case XRSocketInteractor sock:
                info["showInteractableHoverMeshes"] = sock.showInteractableHoverMeshes;
                info["recycleDelayTime"] = sock.recycleDelayTime;
                info["socketActive"] = sock.socketActive;
                break;
            case XRGrabInteractable grab:
                info["movementType"] = grab.movementType.ToString();
                info["throwOnDetach"] = grab.throwOnDetach;
                info["smoothPosition"] = grab.smoothPosition;
                info["smoothRotation"] = grab.smoothRotation;
                info["trackPosition"] = grab.trackPosition;
                info["trackRotation"] = grab.trackRotation;
                info["isSelected"] = grab.isSelected;
                info["isHovered"] = grab.isHovered;
                break;
            case XRSimpleInteractable simple:
                info["selectMode"] = simple.selectMode.ToString();
                info["isSelected"] = simple.isSelected;
                info["isHovered"] = simple.isHovered;
                break;
            case TeleportationArea area:
                info["matchOrientation"] = area.matchOrientation.ToString();
                break;
            case TeleportationAnchor anchor:
                info["matchOrientation"] = anchor.matchOrientation.ToString();
                break;
            case ContinuousMoveProvider move:
                info["moveSpeed"] = move.moveSpeed;
                info["enableStrafe"] = move.enableStrafe;
                info["enableFly"] = move.enableFly;
                break;
            case SnapTurnProvider snap:
                info["turnAmount"] = snap.turnAmount;
                info["enableTurnLeftRight"] = snap.enableTurnLeftRight;
                info["enableTurnAround"] = snap.enableTurnAround;
                break;
            case ContinuousTurnProvider turn:
                info["turnSpeed"] = turn.turnSpeed;
                break;
        }
        return info;
    }
}
```
