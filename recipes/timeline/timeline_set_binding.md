# timeline_set_binding

Set the binding object for a track on a Timeline Director.

**Signature:** `TimelineSetBinding(name string = null, instanceId int = 0, path string = null, trackName string = null, bindingObjectName string = null)`

**Returns:** `{ success, trackName, boundTo }`

**Notes:**
- Both `trackName` and `bindingObjectName` are required; errors are returned if either is not found
- Binds the entire found GameObject (not just its Animator); use `timeline_add_animation_track` to auto-bind an Animator specifically
- Does not save assets; call `AssetDatabase.SaveAssets()` separately if persistence is needed

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
        string bindingObjectName = null;

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var director = go.GetComponent<PlayableDirector>();
        if (director == null) { result.SetResult(new { error = "PlayableDirector not found" }); return; }

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) { result.SetResult(new { error = "No TimelineAsset assigned" }); return; }

        var track = timeline.GetOutputTracks().FirstOrDefault(t => t.name == trackName);
        if (track == null) { result.SetResult(new { error = $"Track not found: {trackName}" }); return; }

        var (bindGo, bindErr) = GameObjectFinder.FindOrError(name: bindingObjectName);
        if (bindErr != null) { result.SetResult(bindErr); return; }

        director.SetGenericBinding(track, bindGo);

        result.SetResult(new { success = true, trackName, boundTo = bindingObjectName });
    }
}
```
