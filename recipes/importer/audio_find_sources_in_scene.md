# audio_find_sources_in_scene

List all AudioSource components in the active scene.

**Skill ID:** `audio_find_sources_in_scene`
**Source:** `AudioSkills.cs` — `AudioFindSourcesInScene`

## Signature

```
audio_find_sources_in_scene(limit?: int = 50)
  → { success, totalFound, showing, sources[{ gameObject, path, clip, volume, loop, enabled }] }
```

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `limit` | int | no | `50` | Max results returned |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int limit = 50;

        var sources = FindHelper.FindAll<AudioSource>();
        var results = sources.Take(limit).Select(s => new
        {
            gameObject = s.gameObject.name,
            path = GameObjectFinder.GetPath(s.gameObject),
            clip = s.clip != null ? s.clip.name : "null",
            volume = s.volume,
            loop = s.loop,
            enabled = s.enabled
        }).ToArray();

        { result.SetResult(new { success = true, totalFound = sources.Length, showing = results.Length, sources = results }); return; }
    }
}
```

## Notes

- Searches the currently active scene only.
- `path` is the full hierarchy path of the GameObject.
- Use `audio_get_source_info` to inspect a specific AudioSource in detail.
