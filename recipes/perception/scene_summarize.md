# scene_summarize

**Skill:** `scene_summarize`
**C# method:** `PerceptionSkills.SceneSummarize`

## Signature

```
SceneSummarize(bool includeComponentStats = true, int topComponentsLimit = 10)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `includeComponentStats` | `bool` | `true` | Whether to collect and return per-component-type counts |
| `topComponentsLimit` | `int` | `10` | Max component types to return in `topComponents` |

## Return Shape

Returns `success`, `sceneName`, `scenePath`, `isDirty`, `stats` (totalObjects, activeObjects, inactiveObjects, rootObjects, maxHierarchyDepth, lights, cameras, canvases), and `topComponents` array (component name + count, excluding Transform).

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
        bool includeComponentStats = true;
        int topComponentsLimit = 10;

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var allObjects = GameObjectFinder.GetSceneObjects();
        var rootObjects = scene.GetRootGameObjects();
        var componentBuffer = new List<Component>(8);

        int totalObjects = allObjects.Count;
        int activeObjects = 0;
        int maxDepth = 0;
        int lightCount = 0, cameraCount = 0, canvasCount = 0;
        var componentCounts = new Dictionary<string, int>();

        foreach (var go in allObjects)
        {
            if (go.activeInHierarchy) activeObjects++;

            int depth = GameObjectFinder.GetDepth(go);
            if (depth > maxDepth) maxDepth = depth;

            componentBuffer.Clear();
            go.GetComponents(componentBuffer);
            foreach (var comp in componentBuffer)
            {
                if (comp == null) continue;
                var typeName = comp.GetType().Name;

                if (comp is Light) lightCount++;
                else if (comp is Camera) cameraCount++;
                else if (comp is Canvas) canvasCount++;

                if (includeComponentStats)
                {
                    if (!componentCounts.ContainsKey(typeName))
                        componentCounts[typeName] = 0;
                    componentCounts[typeName]++;
                }
            }
        }

        componentCounts.Remove("Transform");
        var topComponents = componentCounts
            .OrderByDescending(kv => kv.Value)
            .Take(topComponentsLimit)
            .Select(kv => (object)new { component = kv.Key, count = kv.Value })
            .ToArray();

        result.SetValue(new
        {
            success = true,
            sceneName = scene.name,
            scenePath = scene.path,
            isDirty = scene.isDirty,
            stats = new
            {
                totalObjects,
                activeObjects,
                inactiveObjects = totalObjects - activeObjects,
                rootObjects = rootObjects.Length,
                maxHierarchyDepth = maxDepth,
                lights = lightCount,
                cameras = cameraCount,
                canvases = canvasCount
            },
            topComponents
        });
    }
}
```

## Notes

- Fastest overview: single pass over all objects.
- Does not run validation or hotspot detection; use `scene_analyze` for a full diagnostic.
- Set `includeComponentStats = false` for the fastest possible summary when component counts are not needed.
