# timeline_add_activation_track

Add an Activation track to control object visibility in a Timeline.

**Signature:** `TimelineAddActivationTrack(name string = null, instanceId int = 0, path string = null, trackName string = "Activation Track")`

**Returns:** `{ success, trackName }`

**Notes:**
- Locate the Director GameObject via `name`, `instanceId`, or `path` (at least one required)
- Uses the internal `GetTimeline` helper which requires a `PlayableDirector` with a `TimelineAsset`

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
        string trackName = "Activation Track";

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var director = go.GetComponent<PlayableDirector>();
        if (director == null) { result.SetResult(new { error = "PlayableDirector not found" }); return; }

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) { result.SetResult(new { error = "No TimelineAsset assigned" }); return; }

        var track = timeline.CreateTrack<ActivationTrack>(null, trackName);
        AssetDatabase.SaveAssets();

        result.SetResult(new { success = true, trackName = track.name });
    }
}
```
