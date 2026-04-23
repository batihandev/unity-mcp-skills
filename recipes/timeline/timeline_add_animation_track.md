# timeline_add_animation_track

Add an Animation track to a Timeline, optionally binding an Animator to it.

**Signature:** `TimelineAddAnimationTrack(name string = null, instanceId int = 0, path string = null, trackName string = "Animation Track", bindingObjectName string = null)`

**Returns:** `{ success, trackName, boundObject }`

**Notes:**
- If `bindingObjectName` is provided, the named GameObject's `Animator` (added if missing) is bound to the track
- `boundObject` is `"None"` when no binding is requested

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
        string trackName = "Animation Track";
        string bindingObjectName = null;

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var director = go.GetComponent<PlayableDirector>();
        if (director == null) { result.SetResult(new { error = "PlayableDirector component not found" }); return; }

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) { result.SetResult(new { error = "No TimelineAsset assigned to Director" }); return; }

        var track = timeline.CreateTrack<AnimationTrack>(null, trackName);

        if (!string.IsNullOrEmpty(bindingObjectName))
        {
            var (bindingGo, bindErr) = GameObjectFinder.FindOrError(name: bindingObjectName);
            if (bindErr != null) { result.SetResult(bindErr); return; }

            var animator = bindingGo.GetComponent<Animator>();
            if (animator == null) animator = bindingGo.AddComponent<Animator>();

            director.SetGenericBinding(track, animator);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        result.SetResult(new { success = true, trackName = track.name, boundObject = bindingObjectName ?? "None" });
    }
}
```
