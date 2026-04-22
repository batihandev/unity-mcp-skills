# scene_contract_validate

**Skill:** `scene_contract_validate`
**C# method:** `PerceptionSkills.SceneContractValidate`

## Signature

```
SceneContractValidate(
    string requiredRootsJson = null,
    string requiredTagsJson = null,
    string requiredLayersJson = null,
    bool requireEventSystemForUi = true)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `requiredRootsJson` | `string` | `null` | JSON array of required root object names, e.g. `'["Managers","World"]'` |
| `requiredTagsJson` | `string` | `null` | JSON array of tags that must be defined, e.g. `'["Player","Enemy"]'` |
| `requiredLayersJson` | `string` | `null` | JSON array of layers that must be defined, e.g. `'["UI","Interactable"]'` |
| `requireEventSystemForUi` | `bool` | `true` | Whether to require an EventSystem when UGUI objects are present |

## Return Shape

Returns `success`, `sceneName`, `checkedRoots`, `checkedTags`, `checkedLayers`, `summary` (passed, errors, warnings, info), and `findings` array with `type`, `severity`, `name`, `message`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder.GetSceneObjects`
- `recipes/_shared/perception_helpers.md` — for `PerceptionHelpers.CollectSceneMetrics` / `ParseOptionalStringArray` / `DeduplicateFindings` / `ContainsIgnoreCase` / `GetPropertyValue`

## RunCommand Recipe

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string requiredRootsJson = null;
        string requiredTagsJson = null;
        string requiredLayersJson = null;
        bool requireEventSystemForUi = true;

        var defaultRoots = new[] { "Systems", "Managers", "Gameplay", "UIRoot" };

        string[] requiredRoots;
        string[] requiredTags;
        string[] requiredLayers;
        try
        {
            requiredRoots = PerceptionHelpers.ParseOptionalStringArray(requiredRootsJson, defaultRoots);
            requiredTags = PerceptionHelpers.ParseOptionalStringArray(requiredTagsJson, Array.Empty<string>());
            requiredLayers = PerceptionHelpers.ParseOptionalStringArray(requiredLayersJson, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            result.SetResult(new { success = false, error = "Invalid contract JSON array: " + ex.Message });
            return;
        }

        var metrics = PerceptionHelpers.CollectSceneMetrics(includeComponentStats: false);
        var findings = new List<object>();

        var rootNames = metrics.Scene.GetRootGameObjects().Select(go => go.name).ToList();
        foreach (var requiredRoot in requiredRoots)
            if (!PerceptionHelpers.ContainsIgnoreCase(rootNames, requiredRoot))
                findings.Add(new { type = "MissingRoot", severity = "Warning", name = requiredRoot, message = "Required root '" + requiredRoot + "' is missing." });

        if (metrics.MainCameraCount == 0)
            findings.Add(new { type = "MissingMainCamera", severity = "Error", message = "Convention requires a MainCamera-tagged camera." });
        if (metrics.Lights == 0)
            findings.Add(new { type = "MissingLight", severity = "Warning", message = "Convention expects at least one Light in the scene." });
        if (metrics.HasUiGraphic && metrics.Canvases == 0)
            findings.Add(new { type = "MissingCanvas", severity = "Error", message = "UGUI graphics were found but no Canvas exists." });
        if (requireEventSystemForUi && (metrics.Canvases > 0 || metrics.HasUiGraphic) && metrics.EventSystems == 0)
            findings.Add(new { type = "MissingEventSystem", severity = "Error", message = "UGUI infrastructure exists but EventSystem is missing." });

        var definedTags = new List<string>(InternalEditorUtility.tags);
        foreach (var requiredTag in requiredTags)
            if (!PerceptionHelpers.ContainsIgnoreCase(definedTags, requiredTag))
                findings.Add(new { type = "MissingTagDefinition", severity = "Warning", name = requiredTag, message = "Required tag '" + requiredTag + "' is not defined in TagManager." });

        var definedLayers = new List<string>(InternalEditorUtility.layers);
        foreach (var requiredLayer in requiredLayers)
            if (!PerceptionHelpers.ContainsIgnoreCase(definedLayers, requiredLayer))
                findings.Add(new { type = "MissingLayerDefinition", severity = "Warning", name = requiredLayer, message = "Required layer '" + requiredLayer + "' is not defined in TagManager." });

        var unique = PerceptionHelpers.DeduplicateFindings(findings);
        result.SetResult(new
        {
            success = true,
            sceneName = metrics.Scene.name,
            checkedRoots = requiredRoots,
            checkedTags = requiredTags,
            checkedLayers = requiredLayers,
            summary = new
            {
                passed = unique.Count == 0,
                errors = unique.Count(f => PerceptionHelpers.GetPropertyValue<string>(f, "severity", "Info") == "Error"),
                warnings = unique.Count(f => PerceptionHelpers.GetPropertyValue<string>(f, "severity", "Info") == "Warning"),
                info = unique.Count(f => PerceptionHelpers.GetPropertyValue<string>(f, "severity", "Info") == "Info")
            },
            findings = unique.ToArray()
        });
    }
}
```

## Notes

- Pass JSON arrays as strings: `requiredRootsJson = "[\"Managers\",\"World\"]"`.
- Default roots are defined by `DefaultContractRoots` in the skill implementation (e.g. common structural roots).
- `scene_analyze` calls this internally; use it standalone when you only need convention validation.
