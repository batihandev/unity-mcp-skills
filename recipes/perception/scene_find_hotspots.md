# scene_find_hotspots

## Signature

```
SceneFindHotspots(int deepHierarchyThreshold = 8, int largeChildCountThreshold = 25, int maxResults = 20)
```

## Return Shape

Returns `success`, `sceneName`, `thresholds`, `hotspotCount`, and `hotspots` array with `type`, `severity`, `name`, `path`, `count`, `depth`, `message`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`perception_helpers`](../_shared/perception_helpers.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int deepHierarchyThreshold = 8;
        int largeChildCountThreshold = 25;
        int maxResults = 20;

        var metrics = PerceptionHelpers.CollectSceneMetrics(includeComponentStats: false);
        var hotspots = PerceptionHelpers.CollectHotspots(metrics.Objects, deepHierarchyThreshold, largeChildCountThreshold, maxResults);

        result.SetResult(new
        {
            success = true,
            sceneName = metrics.Scene.name,
            thresholds = new { deepHierarchyThreshold, largeChildCountThreshold },
            hotspotCount = hotspots.Count,
            hotspots = hotspots.Select(h => new
            {
                type = h.Type,
                severity = h.Severity,
                name = h.Name,
                path = h.Path,
                count = h.Count,
                depth = h.Depth,
                message = h.Message
            }).ToArray()
        });
    }
}
```

## Notes

- Hotspot types include `DeepHierarchy`, `LargeChildGroup`, `EmptyNode`, and `DuplicateNameCluster`.
- `scene_health_check` calls this internally and adds structural/facility findings on top.
- Useful for targeted clutter investigation when `scene_health_check` is too broad.
