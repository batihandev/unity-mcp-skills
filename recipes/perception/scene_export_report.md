# scene_export_report

**Skill:** `scene_export_report`
**C# method:** `PerceptionSkills.SceneExportReport`

## Signature

```
SceneExportReport(
    string savePath = "Assets/Docs/SceneReport.md",
    int maxDepth = 10,
    int maxObjects = 500)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `savePath` | `string` | `"Assets/Docs/SceneReport.md"` | Output path for the markdown report |
| `maxDepth` | `int` | `10` | Maximum hierarchy depth to traverse |
| `maxObjects` | `int` | `500` | Maximum number of objects to include |

## Return Shape

Returns `success`, `savedTo`, `objectCount`, `userScriptCount`, `referenceCount`, `codeReferenceCount`.

## RunCommand Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string savePath = "Assets/Docs/SceneReport.md";
        int maxDepth = 10;
        int maxObjects = 500;

        if (Validate.SafePath(savePath, "savePath") is object pathErr0)
        {
            result.SetValue(pathErr0);
            return;
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects().Select(g => g.transform).ToArray();

        var objList = new List<(GameObject go, int depth)>();
        var queue = new Queue<(Transform t, int depth)>();
        foreach (var r in roots) queue.Enqueue((r, 0));
        while (queue.Count > 0 && objList.Count < maxObjects)
        {
            var (t, depth) = queue.Dequeue();
            objList.Add((t.gameObject, depth));
            if (depth + 1 <= maxDepth)
                foreach (Transform child in t) queue.Enqueue((child, depth + 1));
        }

        var allObjects = new GameObject[objList.Count];
        for (int i = 0; i < objList.Count; i++) allObjects[i] = objList[i].go;
        var edges = CollectDependencyEdges(allObjects);
        var codeEdges = CollectCodeDependencies();
        var allEdges = new List<DependencyEdge>(edges);
        allEdges.AddRange(codeEdges);

        var sb = new StringBuilder();
        sb.AppendLine($"# Scene Report: {scene.name}");

        int userScriptCount = 0;
        var userMonos = new List<(string objPath, MonoBehaviour mb)>();
        var componentBuffer = new List<Component>(8);
        foreach (var (go, _) in objList)
        {
            componentBuffer.Clear();
            go.GetComponents(componentBuffer);
            foreach (var component in componentBuffer)
            {
                if (component is MonoBehaviour mono && mono != null && IsUserScript(mono.GetType()))
                {
                    userScriptCount++;
                    userMonos.Add((GameObjectFinder.GetCachedPath(go), mono));
                }
            }
        }

        sb.AppendLine($"> Generated: {DateTime.Now:yyyy-MM-dd HH:mm} | Objects: {objList.Count} | User Scripts: {userScriptCount} | References: {allEdges.Count}");
        sb.AppendLine();
        sb.AppendLine("## Hierarchy");
        sb.AppendLine();

        var componentNamesBuilder = new StringBuilder(64);
        foreach (var (go, depth) in objList)
        {
            var indent = new string(' ', depth * 2);
            componentBuffer.Clear();
            go.GetComponents(componentBuffer);
            componentNamesBuilder.Clear();
            bool isFirst = true;
            foreach (var component in componentBuffer)
            {
                if (component == null || component is Transform) continue;
                var typeName = component.GetType().Name;
                if (component is MonoBehaviour mono && mono != null && IsUserScript(mono.GetType()))
                    typeName += "*";
                if (!isFirst) componentNamesBuilder.Append(", ");
                componentNamesBuilder.Append(typeName);
                isFirst = false;
            }
            var compStr = componentNamesBuilder.ToString();
            sb.AppendLine($"{indent}{go.name}{(compStr.Length > 0 ? $" [{compStr}]" : "")}");
        }
        sb.AppendLine();

        // User script field tables omitted for brevity; full logic in PerceptionSkills.SceneExportReport

        sb.AppendLine("---");
        sb.AppendLine($"*Generated: {DateTime.Now:yyyy-MM-dd HH:mm}*");

        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(savePath, sb.ToString(), SkillsCommon.Utf8NoBom);
        AssetDatabase.ImportAsset(savePath);

        result.SetValue(new
        {
            success = true,
            savedTo = savePath,
            objectCount = objList.Count,
            userScriptCount,
            referenceCount = allEdges.Count,
            codeReferenceCount = codeEdges.Count
        });
    }
}
```

## Notes

- The markdown report has three sections: **Hierarchy** (user scripts marked with `*`), **Script Fields** (serialized values), and **Dependency Graph** (merged serialized + code edges).
- The path must be under `Assets/` (validated by `Validate.SafePath`).
- Use this when you want a durable, shareable artifact rather than in-memory data.
