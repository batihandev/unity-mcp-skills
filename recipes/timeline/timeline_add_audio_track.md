# timeline_add_audio_track

Add an Audio track to a Timeline.

**Signature:** `TimelineAddAudioTrack(name string = null, instanceId int = 0, path string = null, trackName string = "Audio Track")`

**Returns:** `{ success, trackName }`

**Notes:**
- Locate the Director GameObject via `name`, `instanceId`, or `path` (at least one required)
- The GameObject must have a `PlayableDirector` with a `TimelineAsset` assigned

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string trackName = "Audio Track";

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var director = go.GetComponent<PlayableDirector>();
        if (director == null) { result.SetResult(new { error = "PlayableDirector component not found" }); return; }

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) { result.SetResult(new { error = "No TimelineAsset assigned to Director" }); return; }

        var track = timeline.CreateTrack<AudioTrack>(null, trackName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        result.SetResult(new { success = true, trackName = track.name });
    }
}
```
