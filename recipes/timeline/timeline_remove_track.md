# timeline_remove_track

Remove a track by name from a Timeline.

**Signature:** `TimelineRemoveTrack(name string = null, instanceId int = 0, path string = null, trackName string = null)`

**Returns:** `{ success, removed }`

**Notes:**
- `trackName` is required; returns an error if the track is not found
- Uses `timeline.GetOutputTracks()` to find the track by exact name match

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

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

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var director = go.GetComponent<PlayableDirector>();
        if (director == null) { result.SetResult(new { error = "PlayableDirector not found" }); return; }

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) { result.SetResult(new { error = "No TimelineAsset assigned" }); return; }

        var track = timeline.GetOutputTracks().FirstOrDefault(t => t.name == trackName);
        if (track == null) { result.SetResult(new { error = $"Track not found: {trackName}" }); return; }

        timeline.DeleteTrack(track);
        AssetDatabase.SaveAssets();

        result.SetResult(new { success = true, removed = trackName });
    }
}
```
