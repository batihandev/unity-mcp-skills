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

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/hero.png"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) return err;
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null) return new { error = $"Texture not found: {assetPath}" };

        long memSize = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex);
        return new
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
        };
    }
}
```

## Notes

- `memorySizeKB` is the runtime GPU memory estimate from the Profiler API.
- `format` reflects the final compressed GPU format, not the source file format.
- For importer-level settings (maxSize, compression policy) use `texture_get_settings`.
