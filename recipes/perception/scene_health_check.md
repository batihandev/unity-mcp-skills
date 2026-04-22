# scene_health_check

**Skill:** `scene_health_check`
**C# method:** `PerceptionSkills.SceneHealthCheck`

## Signature

```
SceneHealthCheck(int issueLimit = 100, int deepHierarchyThreshold = 8, int largeChildCountThreshold = 25)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `issueLimit` | `int` | `100` | Maximum findings to return |
| `deepHierarchyThreshold` | `int` | `8` | Depth threshold for deep-hierarchy hotspots |
| `largeChildCountThreshold` | `int` | `25` | Child count threshold for large-group hotspots |

## Return Shape

Returns `success`, `sceneName`, `summary` (totalFindings, shownFindings, errors, warnings, info, truncated), `findings` array, `hotspots` array, `suggestedNextSkills`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`perception_helpers`](../_shared/perception_helpers.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

## Notes

- Does the facility / hotspot / missing-reference checks inline (no delegation to `validate_scene` / `validate_missing_references`). For those specific validations call their recipes directly.
- `suggestedNextSkills` is auto-generated based on finding types.

## RunCommand Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int issueLimit = 100;
        int deepHierarchyThreshold = 8;
        int largeChildCountThreshold = 25;

        var metrics = PerceptionHelpers.CollectSceneMetrics(includeComponentStats: false);
        var findings = new List<object>();

        if (metrics.MainCameraCount == 0)
            findings.Add(new { type = "MissingMainCamera", severity = "Error", message = "No MainCamera-tagged camera was found.", source = "scene_health" });
        if (metrics.Lights == 0)
            findings.Add(new { type = "MissingLight", severity = "Warning", message = "No Light component was found.", source = "scene_health" });
        if ((metrics.Canvases > 0 || metrics.HasUiGraphic) && metrics.EventSystems == 0)
            findings.Add(new { type = "MissingEventSystem", severity = "Error", message = "UGUI objects exist but no EventSystem was found.", source = "scene_health" });
        if (metrics.HasUiGraphic && metrics.Canvases == 0)
            findings.Add(new { type = "MissingCanvas", severity = "Error", message = "UI graphics exist but no Canvas was found.", source = "scene_health" });
        if (metrics.Cameras > 0 && metrics.AudioListeners == 0)
            findings.Add(new { type = "MissingAudioListener", severity = "Warning", message = "Scene has cameras but no AudioListener.", source = "scene_health" });

        var hotspots = PerceptionHelpers.CollectHotspots(metrics.Objects, deepHierarchyThreshold, largeChildCountThreshold, issueLimit);
        foreach (var h in hotspots.Where(x => x.Type != "DuplicateNameCluster"))
        {
            findings.Add(new
            {
                type = h.Type,
                severity = h.Severity,
                path = h.Path,
                message = h.Message,
                count = h.Count,
                depth = h.Depth,
                source = "scene_hotspots"
            });
        }

        var unique = PerceptionHelpers.DeduplicateFindings(findings);
        var visible = unique.Take(issueLimit).ToArray();
        var suggestedNextSkills = PerceptionHelpers.BuildSuggestedNextSkills(visible);

        result.SetResult(new
        {
            success = true,
            sceneName = metrics.Scene.name,
            summary = new
            {
                totalFindings = unique.Count,
                shownFindings = visible.Length,
                errors = visible.Count(f => PerceptionHelpers.GetPropertyValue<string>(f, "severity", "Info") == "Error"),
                warnings = visible.Count(f => PerceptionHelpers.GetPropertyValue<string>(f, "severity", "Info") == "Warning"),
                info = visible.Count(f => PerceptionHelpers.GetPropertyValue<string>(f, "severity", "Info") == "Info"),
                truncated = unique.Count > visible.Length
            },
            findings = visible,
            hotspots = hotspots.Select(h => new { type = h.Type, severity = h.Severity, name = h.Name, path = h.Path, count = h.Count, depth = h.Depth, message = h.Message }).ToArray(),
            suggestedNextSkills = suggestedNextSkills.ToArray()
        });
    }
}
```
