# cleaner_get_dependency_tree

Get the dependency tree for an asset — all assets that the given asset depends on, optionally resolved recursively.

**Signature:** `CleanerGetDependencyTree(string assetPath, bool recursive = true)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `assetPath` | string | Yes | — | Project-relative path to the asset |
| `recursive` | bool | No | true | Resolve dependencies recursively (transitive) |

## Returns

```json
{
  "success": true,
  "assetPath": "Assets/Prefabs/Player.prefab",
  "dependencyCount": 4,
  "dependencies": [
    { "path": "Assets/Materials/PlayerMat.mat", "type": "Material" },
    { "path": "Assets/Textures/PlayerTex.png", "type": "Texture2D" }
  ]
}
```

## Error Case

When the asset path does not exist on disk, the skill returns (note: no `success` field in this error response):

```json
{ "error": "Asset not found: Assets/Missing.prefab" }
```

## Notes

- Uses `AssetDatabase.GetDependencies(assetPath, recursive)` and excludes the source asset itself from results.
- When `recursive = false`, only direct (first-level) dependencies are returned.
- When `recursive = true` (default), the full transitive closure is returned.
- The error case returns `{ "error": "..." }` without a `success` key — this matches the upstream implementation.
- Directories are also valid inputs: the asset path can point to a folder.

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Prefabs/Player.prefab";
        bool recursive = true;

        if (!File.Exists(assetPath) && !Directory.Exists(assetPath))
        {
            result.SetValue(new { error = $"Asset not found: {assetPath}" });
            return;
        }

        var deps = AssetDatabase.GetDependencies(assetPath, recursive)
            .Where(d => d != assetPath)
            .Select(d => new
            {
                path = d,
                type = AssetDatabase.LoadMainAssetAtPath(d)?.GetType().Name
            })
            .ToArray();

        result.SetValue(new
        {
            success = true,
            assetPath,
            dependencyCount = deps.Length,
            dependencies = deps
        });
    }
}
```
