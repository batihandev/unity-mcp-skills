# shader_find

Find a shader by its exact internal name (case-sensitive).

**Signature:** `ShaderFind(string searchName)`

**Returns:** `{ found, name, path }`

## Notes

- `searchName` must match the shader's internal name exactly, e.g., `"Standard"` or `"Universal Render Pipeline/Lit"`.
- Shader names are case-sensitive and path-like — do not use shortened forms.
- `path` is `"(built-in)"` for Unity built-in shaders that have no asset file.
- Returns an error if no shader with that name exists.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string searchName = "Universal Render Pipeline/Lit";

        var shader = Shader.Find(searchName);
        if (shader == null)
            { result.SetResult(new { error = $"Shader not found: {searchName}" }); return; }

        var path = AssetDatabase.GetAssetPath(shader);
        { result.SetResult(new
        {
            found = true,
            name = shader.name,
            path = string.IsNullOrEmpty(path) ? "(built-in)" : path
        }); return; }
    }
}
```
