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

        var metrics = CollectSceneMetrics(includeComponentStats: false);
        var findings = new List<object>();

        var sceneValidation = ValidationSkills.ValidateScene(checkEmptyGameObjects: true);
        foreach (var issue in GetEnumerableProperty(sceneValidation, "issues"))
        {
            findings.Add(new
            {
                type = GetPropertyValue<string>(issue, "type", "Unknown"),
                severity = GetPropertyValue<string>(issue, "severity", "Info"),
                gameObject = GetPropertyValue<string>(issue, "gameObject", null),
                path = GetPropertyValue<string>(issue, "path", null),
                message = GetPropertyValue<string>(issue, "message", null),
                count = GetPropertyValue<int>(issue, "count", 0),
                source = "validate_scene"
            });
        }

        var missingReferences = ValidationSkills.ValidateMissingReferences(issueLimit);
        foreach (var issue in GetEnumerableProperty(missingReferences, "issues"))
        {
            findings.Add(new
            {
                type = "MissingReference",
                severity = "Error",
                gameObject = GetPropertyValue<string>(issue, "gameObject", null),
                path = GetPropertyValue<string>(issue, "path", null),
                message = $"{GetPropertyValue<string>(issue, "component", "Component")}.{GetPropertyValue<string>(issue, "property", "property")} is missing a reference.",
                source = "validate_missing_references"
            });
        }

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

        var hotspots = CollectHotspots(metrics.Objects, deepHierarchyThreshold, largeChildCountThreshold, issueLimit);
        foreach (var hotspot in hotspots.Where(h => h.Type != "DuplicateNameCluster"))
        {
            findings.Add(new
            {
                type = hotspot.Type,
                severity = hotspot.Severity,
                path = hotspot.Path,
                message = hotspot.Message,
                count = hotspot.Count,
                depth = hotspot.Depth,
                source = "scene_hotspots"
            });
        }

        var uniqueFindings = DeduplicateFindings(findings);
        var visibleFindings = uniqueFindings.Take(issueLimit).ToArray();
        var suggestedNextSkills = BuildSuggestedNextSkills(visibleFindings);

        result.SetValue(new
        {
            success = true,
            sceneName = metrics.Scene.name,
            summary = new
            {
                totalFindings = uniqueFindings.Count,
                shownFindings = visibleFindings.Length,
                errors = visibleFindings.Count(f => GetPropertyValue<string>(f, "severity", "Info") == "Error"),
                warnings = visibleFindings.Count(f => GetPropertyValue<string>(f, "severity", "Info") == "Warning"),
                info = visibleFindings.Count(f => GetPropertyValue<string>(f, "severity", "Info") == "Info"),
                truncated = uniqueFindings.Count > visibleFindings.Length
            },
            findings = visibleFindings,
            hotspots = hotspots.Select(h => new
            {
                type = h.Type,
                severity = h.Severity,
                name = h.Name,
                path = h.Path,
                count = h.Count,
                depth = h.Depth,
                message = h.Message
            }).ToArray(),
            suggestedNextSkills = suggestedNextSkills.ToArray()
        });
    }
}
```

## Notes

- Aggregates findings from `ValidateScene`, `ValidateMissingReferences`, facility checks, and hotspot analysis.
- `suggestedNextSkills` is auto-generated based on finding types.
- Use `scene_analyze` for a full diagnosis that also includes contract validation and stack detection.
