# audio_add_source

Add an AudioSource component to a GameObject in the active scene.

**Skill ID:** `audio_add_source`
**Source:** `AudioSkills.cs` — `AudioAddSource`

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

## Parameters

Target resolution — provide at least one of `name`, `instanceId`, or `path`:

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | no | — | GameObject name in the scene |
| `instanceId` | int | no | `0` | GameObject instance ID |
| `path` | string | no | — | Hierarchy path (e.g. `Player/Body`) |
| `clipPath` | string | no | — | Asset path to the AudioClip to assign |
| `playOnAwake` | bool | no | `false` | Play clip automatically on scene start |
| `loop` | bool | no | `false` | Loop the clip |
| `volume` | float | no | `1` | Initial volume |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

## Unity_RunCommand Template

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
