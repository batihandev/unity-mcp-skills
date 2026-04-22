# texture_get_info

Inspect runtime dimensions, format, and memory size of a texture.

**Skill ID:** `texture_get_info`
**Source:** `TextureSkills.cs` — `TextureGetInfo`

## Signature

```
texture_get_info(assetPath: string)
  → { success, name, path, width, height, format, mipmapCount, isReadable,
      filterMode, wrapMode, memorySizeKB }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the texture |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/hero.png"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null) { result.SetResult(new { error = $"Texture not found: {assetPath}" }); return; }

        long memSize = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex);
        { result.SetResult(new
        {
            success = true,
            name = tex.name,
            path = assetPath,
            width = tex.width,
            height = tex.height,
            format = tex.format.ToString(),
            mipmapCount = tex.mipmapCount,
            isReadable = tex.isReadable,
            filterMode = tex.filterMode.ToString(),
            wrapMode = tex.wrapMode.ToString(),
            memorySizeKB = memSize / 1024f
        }); return; }
    }
}
```

## Notes

- `memorySizeKB` is the runtime GPU memory estimate from the Profiler API.
- `format` reflects the final compressed GPU format, not the source file format.
- For importer-level settings (maxSize, compression policy) use `texture_get_settings`.
