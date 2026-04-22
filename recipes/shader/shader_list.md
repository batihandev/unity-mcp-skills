# shader_list

List all shader assets in the project, with optional name filter and result limit.

**Signature:** `ShaderList(string filter = null, int limit = 100)`

**Returns:** `{ count, shaders: [{ path, name, propertyCount }] }`

## Notes

- `filter` is matched as a substring against the asset path. Leave `null` to list all shaders.
- `limit` caps the number of results (default 100).
- Each entry includes the shader's internal `name`, its `path`, and the number of exposed properties.
- Built-in shaders (no path) are included when found via `AssetDatabase`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filter = null; // e.g., "Custom" to filter by path substring
        int limit = 100;

        var guids = AssetDatabase.FindAssets("t:Shader");
        var shaders = guids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Where(p => string.IsNullOrEmpty(filter) || p.Contains(filter))
            .Take(limit)
            .Select(p =>
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(p);
                return new
                {
                    path = p,
                    name = shader?.name,
                    propertyCount = shader != null ? ShaderUtil.GetPropertyCount(shader) : 0
                };
            })
            .ToArray();

        { result.SetResult(new { count = shaders.Length, shaders }); return; }
    }
}
```
