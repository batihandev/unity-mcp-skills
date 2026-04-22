# audio_find_clips

Search for AudioClip assets in the project using an AssetDatabase filter.

**Skill ID:** `audio_find_clips`
**Source:** `AudioSkills.cs` — `AudioFindClips`

## Signature

```
audio_find_clips(filter?: string = "", limit?: int = 50)
  → { success, totalFound, showing, clips[{ path, name, length }] }
```

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `filter` | string | no | `""` | Additional AssetDatabase search terms appended to `t:AudioClip` |
| `limit` | int | no | `50` | Max results returned |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filter = "";  // e.g. "label:sfx" or "footstep"
        int limit = 50;

        var guids = AssetDatabase.FindAssets("t:AudioClip " + filter);
        var clips = guids.Take(limit).Select(guid =>
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            return new
            {
                path,
                name   = clip != null ? clip.name   : System.IO.Path.GetFileNameWithoutExtension(path),
                length = clip != null ? clip.length  : 0f
            };
        }).ToArray();

        { result.SetResult(new { success = true, totalFound = guids.Length, showing = clips.Length, clips }); return; }
    }
}
```

## Notes

- `length` is in seconds.
- `totalFound` reflects the full database count before the `limit` cap.
- Use `audio_get_clip_info` to inspect channels, frequency, and load state of a specific clip.
