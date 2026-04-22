# project_list_shaders

List all available shaders in the project, combining asset-database shaders with common built-in shaders. Results are sorted alphabetically and capped by `limit`.

**Signature:** `ProjectListShaders(string filter = null, int limit = 50)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `filter` | string | No | null | Case-insensitive substring filter on shader name |
| `limit` | int | No | 50 | Maximum number of results returned |

## Returns

```json
{
  "success": true,
  "count": 12,
  "filter": "unlit",
  "shaders": ["HDRP/Unlit", "Unlit/Color", "Unlit/Texture", "Universal Render Pipeline/Unlit"]
}
```

## Notes

- Both project shaders (`t:Shader` assets) and hardcoded built-in shaders are merged before filtering.
- Results are deduplicated and sorted before the `limit` cap is applied.
- To find all shaders, omit `filter` and raise `limit`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## C# Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filter = null; // set filter string or leave null
        int limit = 50;       // set max results

        var shaderNames = new List<string>();

        var guids = AssetDatabase.FindAssets("t:Shader");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader != null)
            {
                if (string.IsNullOrEmpty(filter) || shader.name.ToLower().Contains(filter.ToLower()))
                    shaderNames.Add(shader.name);
            }
        }

        var builtInShaders = new[]
        {
            "Standard", "Standard (Specular setup)",
            "Unlit/Color", "Unlit/Texture", "Unlit/Transparent",
            "Mobile/Diffuse", "Mobile/Bumped Diffuse",
            "Particles/Standard Unlit", "Particles/Standard Surface",
            "Skybox/6 Sided", "Skybox/Procedural",
            "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Universal Render Pipeline/Unlit",
            "HDRP/Lit", "HDRP/Unlit"
        };

        foreach (var s in builtInShaders)
        {
            if (Shader.Find(s) != null && !shaderNames.Contains(s))
                if (string.IsNullOrEmpty(filter) || s.ToLower().Contains(filter.ToLower()))
                    shaderNames.Add(s);
        }

        var sorted = shaderNames.Distinct().OrderBy(s => s).Take(limit).ToList();

        result.SetResult(new
        {
            success = true,
            count = sorted.Count,
            filter,
            shaders = sorted
        });
    }
}
```
