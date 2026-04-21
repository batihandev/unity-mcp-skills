# timeline_add_clip

Add a clip to a track by track name.

**Signature:** `TimelineAddClip(name string = null, instanceId int = 0, path string = null, trackName string = null, start double = 0, duration double = 1)`

**Returns:** `{ success, trackName, clipStart, clipDuration }`

**Notes:**
- `trackName` is required; returns an error if the track is not found
- Uses `track.CreateDefaultClip()` so the clip type matches the track type
- Timeline uses clips (not keyframes); `start` and `duration` are in seconds

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string trackName = null;
        double start = 0;
        double duration = 1;

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var director = go.GetComponent<PlayableDirector>();
        if (director == null) { result.SetResult(new { error = "PlayableDirector not found" }); return; }

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) { result.SetResult(new { error = "No TimelineAsset assigned" }); return; }

        var track = timeline.GetOutputTracks().FirstOrDefault(t => t.name == trackName);
        if (track == null) { result.SetResult(new { error = $"Track not found: {trackName}" }); return; }

        var clip = track.CreateDefaultClip();
        clip.start = start;
        clip.duration = duration;
        AssetDatabase.SaveAssets();

        result.SetResult(new { success = true, trackName, clipStart = clip.start, clipDuration = clip.duration });
    }
}
```
