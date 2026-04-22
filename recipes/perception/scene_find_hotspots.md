# scene_find_hotspots

**Skill:** `scene_find_hotspots`
**C# method:** `PerceptionSkills.SceneFindHotspots`

## Signature

```
SceneFindHotspots(int deepHierarchyThreshold = 8, int largeChildCountThreshold = 25, int maxResults = 20)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `deepHierarchyThreshold` | `int` | `8` | Depth at which a node is considered deeply nested |
| `largeChildCountThreshold` | `int` | `25` | Number of direct children that triggers a large-group finding |
| `maxResults` | `int` | `20` | Maximum hotspots returned |

## Return Shape

Returns `success`, `sceneName`, `thresholds`, `hotspotCount`, and `hotspots` array with `type`, `severity`, `name`, `path`, `count`, `depth`, `message`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder.GetSceneObjects` / `GetDepth` / `GetCachedPath`
- `recipes/_shared/perception_helpers.md` — for `PerceptionHelpers.CollectSceneMetrics` / `CollectHotspots`

## RunCommand Recipe

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
