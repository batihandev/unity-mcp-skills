# timeline_set_duration

Set the Timeline asset duration and the Director wrap mode.

**Signature:** `TimelineSetDuration(name string = null, instanceId int = 0, path string = null, duration double = 0, wrapMode string = null)`

**Returns:** `{ success, duration, wrapMode }`

**Notes:**
- Sets `DurationMode.FixedLength`; the Timeline will no longer auto-size to clip contents
- `wrapMode` accepts `Hold`, `Loop`, or `None` (case-insensitive); invalid values are silently ignored
- `wrapMode` in the result reflects `director.extrapolationMode` after the call

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
        double duration = 0;
        string wrapMode = null;

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var director = go.GetComponent<PlayableDirector>();
        if (director == null) { result.SetResult(new { error = "PlayableDirector not found" }); return; }

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) { result.SetResult(new { error = "No TimelineAsset assigned" }); return; }

        timeline.fixedDuration = duration;
        timeline.durationMode = TimelineAsset.DurationMode.FixedLength;

        if (!string.IsNullOrEmpty(wrapMode))
        {
            if (System.Enum.TryParse<DirectorWrapMode>(wrapMode, true, out var wm))
                director.extrapolationMode = wm;
        }

        AssetDatabase.SaveAssets();

        result.SetResult(new { success = true, duration, wrapMode = director.extrapolationMode.ToString() });
    }
}
```
