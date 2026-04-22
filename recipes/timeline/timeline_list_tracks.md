# timeline_list_tracks

List all tracks in a Timeline.

**Signature:** `TimelineListTracks(name string = null, instanceId int = 0, path string = null)`

**Returns:** `{ count, tracks: [{ name, type, muted, clipCount }] }`

**Notes:**
- Read-only operation; does not modify or save any assets
- `type` is the C# class name (e.g. `AudioTrack`, `AnimationTrack`)

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

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var director = go.GetComponent<PlayableDirector>();
        if (director == null) { result.SetResult(new { error = "PlayableDirector not found" }); return; }

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) { result.SetResult(new { error = "No TimelineAsset assigned" }); return; }

        var tracks = timeline.GetOutputTracks().Select(t => new
        {
            name = t.name,
            type = t.GetType().Name,
            muted = t.muted,
            clipCount = t.GetClips().Count()
        }).ToArray();

        result.SetResult(new { count = tracks.Length, tracks });
    }
}
```
