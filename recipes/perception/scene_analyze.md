# scene_analyze

**Skill:** `scene_analyze`
**C# method:** `PerceptionSkills.SceneAnalyze`

## Signature

```
SceneAnalyze(int topComponentsLimit = 10, int issueLimit = 100, int deepHierarchyThreshold = 8, int largeChildCountThreshold = 25)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `topComponentsLimit` | `int` | `10` | Max component types to include in component stats |
| `issueLimit` | `int` | `100` | Max findings to return |
| `deepHierarchyThreshold` | `int` | `8` | Depth threshold for hotspots |
| `largeChildCountThreshold` | `int` | `25` | Child count threshold for hotspots |

## Return Shape

Returns `success`, `sceneName`, `summary` (headline, projectProfile, totalObjects, activeObjects, errors, warnings), `stats`, `findings` array, `warnings` array, `recommendations`, `suggestedNextSkills`, plus nested `componentStats`, `health`, `contract`, and `stack` sub-results.

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
        int topComponentsLimit = 10;
        int issueLimit = 100;
        int deepHierarchyThreshold = 8;
        int largeChildCountThreshold = 25;

        var metrics = CollectSceneMetrics(includeComponentStats: true);
        var componentStats = SceneComponentStats(topComponentsLimit);
        var health = SceneHealthCheck(issueLimit, deepHierarchyThreshold, largeChildCountThreshold);
        var contract = SceneContractValidate();
        var stack = ProjectStackDetect();

        var allFindings = new List<object>();
        allFindings.AddRange(GetEnumerableProperty(health, "findings"));
        allFindings.AddRange(GetEnumerableProperty(contract, "findings"));
        var dedupedFindings = DeduplicateFindings(allFindings);
        var warnings = dedupedFindings
            .Where(f => GetPropertyValue<string>(f, "severity", "Info") != "Error")
            .Take(Math.Min(20, dedupedFindings.Count))
            .ToArray();

        var recommendations = BuildSuggestedNextSkills(dedupedFindings);
        var projectProfile = GetPropertyValue<string>(stack, "projectProfile", "Unknown");
        var errorCount = dedupedFindings.Count(f => GetPropertyValue<string>(f, "severity", "Info") == "Error");
        var warningCount = dedupedFindings.Count(f => GetPropertyValue<string>(f, "severity", "Info") == "Warning");

        result.SetValue(new
        {
            success = true,
            sceneName = metrics.Scene.name,
            summary = new
            {
                headline = $"{projectProfile} scene with {metrics.TotalObjects} objects, {errorCount} errors, and {warningCount} warnings detected.",
                projectProfile,
                totalObjects = metrics.TotalObjects,
                activeObjects = metrics.ActiveObjects,
                errors = errorCount,
                warnings = warningCount
            },
            stats = GetPropertyValue(componentStats, "stats"),
            findings = dedupedFindings.ToArray(),
            warnings,
            recommendations = recommendations.ToArray(),
            suggestedNextSkills = recommendations.Select(r => new
            {
                skill = GetPropertyValue<string>(r, "skill", null),
                reason = GetPropertyValue<string>(r, "reason", null)
            }).ToArray(),
            componentStats,
            health,
            contract,
            stack
        });
    }
}
```

## Notes

- This is the broadest single-call diagnosis: it internally calls `scene_component_stats`, `scene_health_check`, `scene_contract_validate`, and `project_stack_detect`.
- Use `scene_summarize` for a lightweight overview without the full findings pipeline.
- Use `scene_health_check` directly when you only need hygiene findings.
