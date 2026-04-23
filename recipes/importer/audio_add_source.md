# audio_add_source

Add an AudioSource component to a GameObject in the active scene.

## Signature

```
audio_add_source(
  name?: string,
  instanceId?: int,
  path?: string,
  clipPath?: string,
  playOnAwake?: bool = false,
  loop?: bool = false,
  volume?: float = 1
) → { success, gameObject, instanceId }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Player";                           // Replace with target GameObject name
        int instanceId = 0;
        string path = null;
        string clipPath = "Assets/Audio/footstep.wav";   // Optional: clip to assign
        bool playOnAwake = false;
        bool loop = false;
        float volume = 1f;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var source = Undo.AddComponent<AudioSource>(go);
        source.playOnAwake = playOnAwake;
        source.loop = loop;
        source.volume = volume;

        if (!string.IsNullOrEmpty(clipPath))
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip != null) source.clip = clip;
        }

        { result.SetResult(new { success = true, gameObject = go.name, instanceId = go.GetInstanceID() }); return; }
    }
}
```

## Notes

- Uses `Undo.AddComponent` so the operation is undo-able in the Editor.
- If `clipPath` is provided but the asset is not found, the source is still added (without a clip assignment).
- Use `audio_set_source_properties` to update an existing AudioSource's fields.
