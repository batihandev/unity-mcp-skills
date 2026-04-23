# audio_get_source_info

Read the AudioSource component configuration on a scene GameObject.

## Signature

```
audio_get_source_info(name?: string, instanceId?: int, path?: string)
  → { success, gameObject, clip, volume, pitch, loop, playOnAwake,
      mute, spatialBlend, minDistance, maxDistance, priority }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Player"; // Replace with target
        int instanceId = 0;
        string path = null;

        var (source, error) = GameObjectFinder.FindComponentOrError<AudioSource>(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        { result.SetResult(new
        {
            success = true,
            gameObject = source.gameObject.name,
            clip = source.clip != null ? source.clip.name : "null",
            volume = source.volume,
            pitch = source.pitch,
            loop = source.loop,
            playOnAwake = source.playOnAwake,
            mute = source.mute,
            spatialBlend = source.spatialBlend,
            minDistance = source.minDistance,
            maxDistance = source.maxDistance,
            priority = source.priority
        }); return; }
    }
}
```

## Notes
- Returns `"null"` as a string if no clip is assigned.
- Use `audio_set_source_properties` to update the AudioSource.

