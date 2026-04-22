# scene_dependency_analyze

**Skill:** `scene_dependency_analyze`
**C# method:** `PerceptionSkills.SceneDependencyAnalyze`

## Signature

```
SceneDependencyAnalyze(
    string targetPath = null,
    string savePath = null)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `targetPath` | `string` | `null` | Hierarchy path of the object (and its subtree) to analyze. When null, analyzes all objects that are referenced by others |
| `savePath` | `string` | `null` | If set, saves a markdown dependency report to this path under `Assets/` |

## Return Shape

Returns `success`, `sceneName`, `target`, `totalReferences`, `objectsAnalyzed`, `analysis`, `savedTo`, `markdown` (inline markdown when `savePath` is null).

**Prerequisites:** [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`skills_common`](../_shared/skills_common.md)

## RunCommand Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

internal struct _DepEdge_sda { public string fromObject, toObject, fieldType, fieldName; }

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string targetPath = null;   // e.g. "World/Environment/TreeGroup"
        string savePath = null;     // e.g. "Assets/Docs/DependencyReport.md"

        if (!string.IsNullOrEmpty(savePath) && Validate.SafePath(savePath, "savePath") is object pathErr)
        {
            result.SetResult(pathErr);
            return;
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var allObjects = GameObjectFinder.GetSceneObjects();
        var edges = CollectDependencyEdges(allObjects);

        var reverseIndex = edges.GroupBy(e => e.toObject)
            .ToDictionary(g => g.Key, g => g.ToList());

        List<object> analysis;
        if (!string.IsNullOrEmpty(targetPath))
        {
            var targetGo = GameObjectFinder.FindByPath(targetPath);
            if (targetGo == null)
            {
                result.SetResult(new { success = false, error = $"Target '{targetPath}' not found" });
                return;
            }

            var targetPaths = new HashSet<string>();
            var stack = new Stack<Transform>();
            stack.Push(targetGo.transform);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                targetPaths.Add(GameObjectFinder.GetCachedPath(t.gameObject));
                foreach (Transform child in t) stack.Push(child);
            }

            analysis = BuildAnalysis(targetPaths, reverseIndex, edges);
        }
        else
        {
            var allTargets = new HashSet<string>(reverseIndex.Keys);
            analysis = BuildAnalysis(allTargets, reverseIndex, edges);
        }

        var md = BuildDependencyMarkdown(scene.name, targetPath, analysis, edges);

        string savedPath = null;
        if (!string.IsNullOrEmpty(savePath))
        {
            var dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(savePath, md, SkillsCommon.Utf8NoBom);
            AssetDatabase.ImportAsset(savePath);
            savedPath = savePath;
        }

        result.SetResult(new
        {
            success = true,
            sceneName = scene.name,
            target = targetPath,
            totalReferences = edges.Count,
            objectsAnalyzed = analysis.Count,
            analysis,
            savedTo = savedPath,
            markdown = savedPath == null ? md : null
        });
    }

    private static List<_DepEdge_sda> CollectDependencyEdges(List<GameObject> objects) => new List<_DepEdge_sda>();

    private static List<object> BuildAnalysis(HashSet<string> targets, Dictionary<string, List<_DepEdge_sda>> reverseIndex, List<_DepEdge_sda> edges) => new List<object>();

    private static string BuildDependencyMarkdown(string sceneName, string target, List<object> analysis, List<_DepEdge_sda> edges) => "";
}
```

## Notes

- Use this to answer "what breaks if I delete or disable this object?"
- For script-to-script dependency closure (not scene object references), use `script_dependency_graph`.
- When `targetPath` is null, the full reference graph of the scene is returned.
