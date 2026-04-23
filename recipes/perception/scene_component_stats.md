# scene_component_stats

## Signature

```
SceneComponentStats(int topComponentsLimit = 15)
```

## Return Shape

Returns `success`, `sceneName`, `stats` (object counts, hierarchy depth, cameras, lights, canvases, EventSystems, AudioListeners, prefab instances, disabled ratio, empty leaf count), `keyFacilities` (bool flags for main camera, light, canvas, EventSystem, AudioListener, UGUI, UI Toolkit), and `topComponents` array.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`perception_helpers`](../_shared/perception_helpers.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;
using System;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int topComponentsLimit = 15;

        var metrics = PerceptionHelpers.CollectSceneMetrics(includeComponentStats: true);
        var totalObjects = Math.Max(metrics.TotalObjects, 1);

        result.SetResult(new
        {
            success = true,
            sceneName = metrics.Scene.name,
            stats = new
            {
                totalObjects = metrics.TotalObjects,
                activeObjects = metrics.ActiveObjects,
                inactiveObjects = metrics.TotalObjects - metrics.ActiveObjects,
                rootObjects = metrics.RootObjects,
                maxHierarchyDepth = metrics.MaxHierarchyDepth,
                prefabInstances = metrics.PrefabInstances,
                disabledObjects = metrics.DisabledObjects,
                disabledRatio = (float)Math.Round(metrics.DisabledObjects / (double)totalObjects, 3),
                emptyLeafObjects = metrics.EmptyLeafCount,
                cameras = metrics.Cameras,
                mainCameras = metrics.MainCameraCount,
                lights = metrics.Lights,
                canvases = metrics.Canvases,
                eventSystems = metrics.EventSystems,
                audioListeners = metrics.AudioListeners
            },
            keyFacilities = new
            {
                hasMainCamera = metrics.MainCameraCount > 0,
                hasLight = metrics.Lights > 0,
                hasCanvas = metrics.Canvases > 0,
                hasEventSystem = metrics.EventSystems > 0,
                hasAudioListener = metrics.AudioListeners > 0,
                hasUgui = metrics.Canvases > 0 || metrics.HasUiGraphic,
                hasUiToolkit = metrics.HasUiToolkitDocument
            },
            topComponents = PerceptionHelpers.BuildTopComponents(metrics, topComponentsLimit)
        });
    }
}
```

## Notes

- Use this for a quick facility and component-count snapshot.
- `keyFacilities` booleans are useful for detecting missing infrastructure (e.g. no MainCamera, no EventSystem).
- For broader diagnosis, prefer `scene_analyze` which calls this internally.
