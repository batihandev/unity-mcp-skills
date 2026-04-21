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

## RunCommand Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int deepHierarchyThreshold = 8;
        int largeChildCountThreshold = 25;
        int maxResults = 20;

        var metrics = CollectSceneMetrics(includeComponentStats: false);
        var hotspots = CollectHotspots(metrics.Objects, deepHierarchyThreshold, largeChildCountThreshold, maxResults);

        result.SetValue(new
        {
            success = true,
            sceneName = metrics.Scene.name,
            thresholds = new
            {
                deepHierarchyThreshold,
                largeChildCountThreshold
            },
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
