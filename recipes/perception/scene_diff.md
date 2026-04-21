# scene_diff

**Skill:** `scene_diff`
**C# method:** `PerceptionSkills.SceneDiff`

## Signature

```
SceneDiff(string snapshotJson = null)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `snapshotJson` | `string` | `null` | JSON snapshot from a previous `scene_diff` call. When null, the skill captures and returns the current state |

## Return Shape

**Snapshot mode** (snapshotJson is null): returns `success`, `mode="snapshot"`, `sceneName`, `objectCount`, `snapshot` array (store this for later comparison).

**Diff mode** (snapshotJson provided): returns `success`, `mode="diff"`, `sceneName`, `summary` (addedCount, removedCount, modifiedCount), `added` array, `removed` array, `modified` array (each with instanceId, name, path, changes[]).

## RunCommand Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Set snapshotJson to null to capture; set it to a prior snapshot string to compare
        string snapshotJson = null;

        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            var snapshot = CaptureSceneSnapshot();
            result.SetValue(new
            {
                success = true,
                mode = "snapshot",
                sceneName = SceneManager.GetActiveScene().name,
                objectCount = snapshot.Count,
                snapshot
            });
            return;
        }

        JArray previousSnapshot;
        try { previousSnapshot = JArray.Parse(snapshotJson); }
        catch (Exception ex)
        {
            result.SetValue(new { error = $"Invalid snapshotJson: {ex.Message}" });
            return;
        }

        var previousMap = new Dictionary<int, JObject>();
        foreach (var item in previousSnapshot)
        {
            var id = item["instanceId"]?.Value<int>() ?? 0;
            if (id != 0) previousMap[id] = item as JObject;
        }

        var currentSnapshot = CaptureSceneSnapshot();
        var currentMap = new Dictionary<int, Dictionary<string, object>>();
        foreach (var item in currentSnapshot)
        {
            var id = GetPropertyValue<int>(item, "instanceId", 0);
            if (id != 0) currentMap[id] = item as Dictionary<string, object> ?? new Dictionary<string, object>();
        }

        var added = new List<object>();
        var removed = new List<object>();
        var modified = new List<object>();

        foreach (var kvp in currentMap)
        {
            if (!previousMap.ContainsKey(kvp.Key))
                added.Add(new { instanceId = kvp.Key, name = GetPropertyValue<string>(kvp.Value, "name", ""), path = GetPropertyValue<string>(kvp.Value, "path", "") });
        }

        foreach (var kvp in previousMap)
        {
            if (!currentMap.ContainsKey(kvp.Key))
                removed.Add(new { instanceId = kvp.Key, name = kvp.Value["name"]?.ToString() ?? "", path = kvp.Value["path"]?.ToString() ?? "" });
        }

        foreach (var kvp in currentMap)
        {
            if (previousMap.TryGetValue(kvp.Key, out var prev))
            {
                var changes = new List<string>();
                if (!string.Equals(GetPropertyValue<string>(kvp.Value, "name", ""), prev["name"]?.ToString() ?? "", StringComparison.Ordinal)) changes.Add("name");
                if (!string.Equals(GetPropertyValue<string>(kvp.Value, "path", ""), prev["path"]?.ToString() ?? "", StringComparison.Ordinal)) changes.Add("path");
                if (!string.Equals(GetPropertyValue<string>(kvp.Value, "componentList", ""), prev["componentList"]?.ToString() ?? "", StringComparison.Ordinal)) changes.Add("components");
                if (HasVectorDifference(kvp.Value, prev, "position")) changes.Add("position");
                if (HasVectorDifference(kvp.Value, prev, "rotation")) changes.Add("rotation");
                if (HasVectorDifference(kvp.Value, prev, "scale")) changes.Add("scale");

                if (changes.Count > 0)
                    modified.Add(new { instanceId = kvp.Key, name = GetPropertyValue<string>(kvp.Value, "name", ""), path = GetPropertyValue<string>(kvp.Value, "path", ""), changes = changes.ToArray() });
            }
        }

        result.SetValue(new
        {
            success = true,
            mode = "diff",
            sceneName = SceneManager.GetActiveScene().name,
            summary = new { addedCount = added.Count, removedCount = removed.Count, modifiedCount = modified.Count },
            added = added.ToArray(),
            removed = removed.ToArray(),
            modified = modified.ToArray()
        });
    }
}
```

## Workflow

1. Call with `snapshotJson = null` before making changes — save the returned `snapshot` JSON array.
2. Make your scene changes.
3. Call again with `snapshotJson = <saved snapshot>` — receive the diff.

## Notes

- Tracked change properties: `name`, `path`, `components`, `position`, `rotation`, `scale`.
- Objects are matched by `instanceId`; renamed or moved objects still match correctly.
- Useful for validating that a tool call produced exactly the expected changes and nothing else.
