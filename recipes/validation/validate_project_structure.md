# validate_project_structure

Get an overview of the project folder structure and asset counts by type.

**Signature:** `ValidateProjectStructure(rootPath string = "Assets", maxDepth int = 2)`

**Returns:** `{ rootPath, assetCounts: { Material, Prefab, Script, Texture2D, AudioClip, Scene, Shader }, structure }`

**Notes:**
- `structure` is a recursive tree of `{ name, fileCount, children }` objects up to `maxDepth` levels deep
- Asset counts cover only assets under `rootPath`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string rootPath = "Assets";
        int maxDepth = 2;

        var structure = GetFolderStructure(rootPath, 0, maxDepth);

        var assetCounts = new Dictionary<string, int>();
        var commonTypes = new[] { "Material", "Prefab", "Script", "Texture2D", "AudioClip", "Scene", "Shader" };

        foreach (var type in commonTypes)
        {
            var count = AssetDatabase.FindAssets($"t:{type}", new[] { rootPath }).Length;
            assetCounts[type] = count;
        }

        result.SetResult(new
        {
            rootPath,
            assetCounts,
            structure
        });
    }

    private object GetFolderStructure(string path, int depth, int maxDepth)
    {
        if (!Directory.Exists(path) || depth >= maxDepth)
            return null;

        var subDirs = Directory.GetDirectories(path)
            .Select(d => new DirectoryInfo(d))
            .Select(d => new
            {
                name = d.Name,
                fileCount = Directory.GetFiles(d.FullName).Count(f => !f.EndsWith(".meta")),
                children = depth < maxDepth - 1 ? GetFolderStructure(d.FullName, depth + 1, maxDepth) : null
            })
            .ToArray();

        return subDirs;
    }
}
```
