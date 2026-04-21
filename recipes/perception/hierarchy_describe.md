# hierarchy_describe

**Skill:** `hierarchy_describe`
**C# method:** `PerceptionSkills.HierarchyDescribe`

## Signature

```
HierarchyDescribe(int maxDepth = 5, bool includeInactive = false, int maxItemsPerLevel = 20)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `maxDepth` | `int` | `5` | Maximum depth levels to traverse |
| `includeInactive` | `bool` | `false` | Whether to include inactive GameObjects |
| `maxItemsPerLevel` | `int` | `20` | Maximum children shown per level |

## Return Shape

Returns `success`, `sceneName`, `hierarchy` (text-formatted tree string), `totalObjectsShown`.

## RunCommand Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int maxDepth = 5;
        bool includeInactive = false;
        int maxItemsPerLevel = 20;

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var rootObjects = scene.GetRootGameObjects()
            .Where(g => includeInactive || g.activeInHierarchy)
            .OrderBy(g => g.transform.GetSiblingIndex())
            .Take(maxItemsPerLevel)
            .ToArray();

        var sb = new StringBuilder();
        sb.AppendLine($"Scene: {scene.name}");
        sb.AppendLine("─".PadRight(40, '─'));

        int totalShown = 0;
        var componentBuffer = new List<Component>(8);
        foreach (var root in rootObjects)
        {
            BuildHierarchyTree(sb, root.transform, 0, maxDepth, includeInactive, maxItemsPerLevel, ref totalShown, componentBuffer);
        }

        var allRoots = scene.GetRootGameObjects();
        if (allRoots.Length > maxItemsPerLevel)
            sb.AppendLine($"... and {allRoots.Length - maxItemsPerLevel} more root objects");

        result.SetValue(new
        {
            success = true,
            sceneName = scene.name,
            hierarchy = sb.ToString(),
            totalObjectsShown = totalShown
        });
    }
}
```

## Notes

- Returns a human-readable indented text tree, not structured data.
- For structured AI coding context (with component lists and references), use `scene_context` instead.
- Increase `maxDepth` or `maxItemsPerLevel` when large scenes are truncated.
