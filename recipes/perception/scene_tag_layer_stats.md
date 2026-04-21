# scene_tag_layer_stats

**Skill:** `scene_tag_layer_stats`
**C# method:** `PerceptionSkills.SceneTagLayerStats`

## Signature

```
SceneTagLayerStats()
```

## Parameters

None.

## Return Shape

Returns `success`, `totalObjects`, `untaggedCount`, `tags` array (tag, count, sorted by count desc), `layers` array (layer, count, sorted by count desc), `emptyDefinedLayers` array (layer names that are defined in TagManager but have no objects).

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

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
        var allObjects = GameObjectFinder.GetSceneObjects();
        var tagCounts = new Dictionary<string, int>();
        var layerCounts = new Dictionary<string, int>();
        var usedLayers = new HashSet<int>();
        int untaggedCount = 0;

        foreach (var go in allObjects)
        {
            var tag = go.tag ?? "Untagged";
            if (tag == "Untagged") untaggedCount++;
            tagCounts[tag] = tagCounts.TryGetValue(tag, out var tc) ? tc + 1 : 1;
            var layerName = LayerMask.LayerToName(go.layer);
            if (string.IsNullOrEmpty(layerName)) layerName = $"Layer {go.layer}";
            layerCounts[layerName] = layerCounts.TryGetValue(layerName, out var lc) ? lc + 1 : 1;
            usedLayers.Add(go.layer);
        }

        var emptyLayers = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            var layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName) && !usedLayers.Contains(i))
                emptyLayers.Add(layerName);
        }

        result.SetValue(new
        {
            success = true,
            totalObjects = allObjects.Count,
            untaggedCount,
            tags = tagCounts.OrderByDescending(kv => kv.Value).Select(kv => new { tag = kv.Key, count = kv.Value }).ToArray(),
            layers = layerCounts.OrderByDescending(kv => kv.Value).Select(kv => new { layer = kv.Key, count = kv.Value }).ToArray(),
            emptyDefinedLayers = emptyLayers.ToArray()
        });
    }
}
```

## Notes

- `emptyDefinedLayers` lists layers that exist in TagManager but are assigned to no scene objects — useful for spotting orphaned layer definitions.
- Use alongside `scene_contract_validate` when validating required layer/tag conventions.
