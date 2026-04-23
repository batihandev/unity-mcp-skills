# timeline_add_control_track

Add a Control track for nested Timelines or prefab spawning.

**Signature:** `TimelineAddControlTrack(name string = null, instanceId int = 0, path string = null, trackName string = "Control Track")`

**Returns:** `{ success, trackName }`

**Notes:**
- Locate the Director GameObject via `name`, `instanceId`, or `path` (at least one required)
- Control tracks are used to drive nested `PlayableDirector` components or spawn prefabs at runtime

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

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
        string trackName = "Control Track";

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var director = go.GetComponent<PlayableDirector>();
        if (director == null) { result.SetResult(new { error = "PlayableDirector not found" }); return; }

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) { result.SetResult(new { error = "No TimelineAsset assigned" }); return; }

        var track = timeline.CreateTrack<ControlTrack>(null, trackName);
        AssetDatabase.SaveAssets();

        result.SetResult(new { success = true, trackName = track.name });
    }
}
```
