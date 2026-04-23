# scene_export_report

## Signature

```
SceneExportReport(
    string savePath = "Assets/Docs/SceneReport.md",
    int maxDepth = 10,
    int maxObjects = 500)
```

## Return Shape

Returns `success`, `savedTo`, `objectCount`, `userScriptCount`, `referenceCount`, `codeReferenceCount`.

**Prerequisites:** [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`skills_common`](../_shared/skills_common.md)

```csharp
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

internal struct _DepEdge_ser { public string fromObject, toObject, fieldType, fieldName; }

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string savePath = "Assets/Docs/SceneReport.md";
        int maxDepth = 10;
        int maxObjects = 500;

        if (Validate.SafePath(savePath, "savePath") is object pathErr0)
        {
            result.SetResult(pathErr0);
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
        var allEdges = new List<_DepEdge_ser>(edges);
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

        result.SetResult(new
        {
            success = true,
            savedTo = savePath,
            objectCount = objList.Count,
            userScriptCount,
            referenceCount = allEdges.Count,
            codeReferenceCount = codeEdges.Count
        });
    }

    private static List<_DepEdge_ser> CollectDependencyEdges(GameObject[] objects) => new List<_DepEdge_ser>();
    private static List<_DepEdge_ser> CollectCodeDependencies() => new List<_DepEdge_ser>();
    private static bool IsUserScript(System.Type t) => t.Assembly.GetName().Name == "Assembly-CSharp";
}
```

## Notes

- The markdown report has three sections: **Hierarchy** (user scripts marked with `*`), **Script Fields** (serialized values), and **Dependency Graph** (merged serialized + code edges).
- The path must be under `Assets/` (validated by `Validate.SafePath`).
- Use this when you want a durable, shareable artifact rather than in-memory data.
