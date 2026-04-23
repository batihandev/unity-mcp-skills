# hierarchy_describe

## Signature

```
HierarchyDescribe(int maxDepth = 5, bool includeInactive = false, int maxItemsPerLevel = 20)
```

## Return Shape

Returns `success`, `sceneName`, `hierarchy` (text-formatted tree string), `totalObjectsShown`.

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

        result.SetResult(new
        {
            success = true,
            sceneName = scene.name,
            hierarchy = sb.ToString(),
            totalObjectsShown = totalShown
        });
    }

    private static void BuildHierarchyTree(System.Text.StringBuilder sb, Transform t, int depth, int maxDepth, bool includeInactive, int maxItemsPerLevel, ref int totalShown, List<Component> componentBuffer)
    {
        if (depth > maxDepth) return;
        var indent = new string(' ', depth * 2);
        componentBuffer.Clear();
        t.gameObject.GetComponents(componentBuffer);
        var comps = string.Join(", ", componentBuffer.ConvertAll(c => c != null && !(c is Transform) ? c.GetType().Name : null).FindAll(s => s != null));
        sb.AppendLine($"{indent}{t.gameObject.name}{(comps.Length > 0 ? $" [{comps}]" : "")}");
        totalShown++;
        int childCount = 0;
        foreach (Transform child in t)
        {
            if (!includeInactive && !child.gameObject.activeInHierarchy) continue;
            if (childCount >= maxItemsPerLevel) { sb.AppendLine($"{indent}  ... more"); break; }
            BuildHierarchyTree(sb, child, depth + 1, maxDepth, includeInactive, maxItemsPerLevel, ref totalShown, componentBuffer);
            childCount++;
        }
    }
}
```

## Notes

- Returns a human-readable indented text tree, not structured data.
- For structured AI coding context (with component lists and references), use `scene_context` instead.
- Increase `maxDepth` or `maxItemsPerLevel` when large scenes are truncated.
