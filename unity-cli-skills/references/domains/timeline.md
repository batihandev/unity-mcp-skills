# Timeline

Use this guide with `com.unity.timeline` 6.6.x, the selected Editor's live command catalog, and the [foundation safety rules](foundation.md). The optional `com.batihandev.unity-cli-commands` package adds five `timeline.*` commands when Timeline 6.6.x is present. The native Pipeline commands own Timeline asset creation, track creation, sourced clips, and structural reads. Inspect nested `Ok` and `Error` for typed commands and read resulting Unity state after every write.

## Create a Timeline and Director

Require a safe, nonempty name without `/`, `\\`, or `..`. Use `Assets/Timelines` when no folder is supplied. Build a unique `Assets/.../<name>.playable` path and check both the asset and its `.meta` are absent immediately before `create_timeline`; its `confirm=true` can overwrite, so leave it false. Create the folder through native `create_folder` if needed. The native command creates the asset but does not create a scene Director.

Create a GameObject with the requested name in the selected regular scene through native `create_gameobject`; add a `UnityEngine.Playables.PlayableDirector` through native `add_component`. Resolve and type-check the new TimelineAsset, then set the Director's public `playableAsset` using `component.member-set` with its exact component handle and asset reference. Check the returned `Ok`, Director readback, and asset path. Save the selected scene explicitly with native `save_scene`. Asset and scene operations are separate writes; a later failure requires deliberate cleanup. Record actual asset, GameObject, Director, and scene identities.

## Add and list tracks

Call native `add_timeline_track` with the exact Timeline asset and `trackType` `Animation`, `Audio`, `Activation`, `Control`, or `Signal`. Supply the old recipe defaults explicitly when `trackName` is omitted: `Animation Track`, `Audio Track`, `Activation Track`, `Control Track`, `Signal Track`. The native API may uniquify a requested name. Call native `get_timeline` after creation and select the newly created track by comparing before and after track identities; retain its actual name for later commands. A Signal track starts empty; signal emitter authoring is outside this workflow.

When an Animation track will be bound, resolve the selected Director and its Timeline and inspect the exact target before creating the track. The target must be in the same loaded regular scene, with an unambiguous Animator component; a missing Animator is acceptable only when `autoAddAnimator=true` was explicitly chosen. Create the track only after this preflight, then bind its actual returned name. These are separate commands. If binding fails afterward, remove only the newly created track through the guarded `timeline.track-remove` command and verify that cleanup.

For a list, native `get_timeline` supplies structural tracks and clips. A bounded read-only `eval` may add C# type name, muted state, and clip count from the public API, using the exact asset and Director already selected:

```csharp
var director = /* exact loaded PlayableDirector */;
var timeline = director.playableAsset as UnityEngine.Timeline.TimelineAsset;
if (timeline == null) throw new System.InvalidOperationException("Director has no TimelineAsset");
return new {
    director = UnityEngine.EntityId.ToULong(director.GetEntityId()).ToString(),
    assetPath = UnityEditor.AssetDatabase.GetAssetPath(timeline),
    tracks = timeline.GetOutputTracks().Select(t => new {
        name = t.name, type = t.GetType().Name, fullType = t.GetType().FullName, muted = t.muted,
        clipCount = t.GetClips().Count()
    }).ToArray()
};
```

The read is structural; it does not imply that a binding exists or a playable graph is active.

## Add clips

For an Animation track with a provided `AnimationClip`, or an Audio track with a provided `AudioClip`, use native `add_timeline_clip` with exact Timeline, actual track name, source asset, start, and duration. Preflight the source type and path, then inspect the resulting clip. For a source-free Animation, Audio, Activation, or Control clip, use `timeline.default-clip-create`. Its `timeline` is an exact TimelineAsset reference; `track` is an exact output-track name. Defaults are `start=0`, `duration=1`. It refuses nonfinite or invalid timing, duplicate or missing names, and tracks without a public `TrackClipTypeAttribute` default `ScriptableObject`/`IPlayableAsset` type. Signal is deliberately refused without a warning or mutation. Preview with `dryRun=true`, then set `confirm=true`. The command isolates an Undo group, saves the exact asset, and returns observed clip timing and asset type. `dryRun` wins over confirmation.

## Bind a track

`timeline.binding-set` accepts an exact loaded Director, an exact output-track name, and an exact target GameObject or component. The target must be a nonpersistent object in the Director's same regular loaded scene. Public `TrackBindingTypeAttribute` determines the required object: Animation needs `Animator`, Audio needs `AudioSource`, Activation needs `GameObject`, and Signal needs `SignalReceiver`. Control has no general binding and is refused. Multiple matching components on a GameObject require an exact component handle. For an Animation track only, `autoAddAnimator=true` creates a missing Animator and binds it in the same Undo group. Read `BoundObject`, `BoundType`, and `SceneDirty`, then inspect `GetGenericBinding` on the Director. Save the scene after a persistent binding change. A binding belongs to that Director; another Director using the same asset can have a different binding.

## Set duration and remove tracks

`timeline.duration-set` takes an exact loaded Director, nonnegative finite `duration` (default 0), optional `wrapMode` of `Hold`, `Loop`, or `None`, and `dryRun`/`confirm`. It sets the shared TimelineAsset to fixed-length mode and the selected Director's wrap mode. The result reports actual `FixedDuration`, `Duration`, `DurationMode`, and `WrapMode`; Timeline can quantize the fixed duration. It records the asset and Director in one Undo group, saves the exact asset, and dirties the scene. Save the scene separately. A duration change affects every Director using the asset, while wrap mode affects the selected Director.

`timeline.track-remove` takes an exact Timeline asset and exact output-track name. Preview, then confirm. It uses public `TimelineAsset.DeleteTrack`, including the track's children and clips, and saves the exact asset. The deletion affects all Directors that use the asset. Existing Director bindings are not silently cleared. Verify the track is absent through `get_timeline`, exercise Undo/Redo as needed, save after Undo when retention matters, and reload the asset to prove persistence.

## Director transport

`timeline.director-control` takes an exact loaded Director and `action=play` (default), `pause`, or `stop`. It calls the public PlayableDirector API and returns before/after state, time, and `IsPlaying`. Runtime transport is not an Undoable asset edit. In Edit mode, do not infer autonomous clock advance from `Play`; explicitly set `director.time` and call `director.Evaluate()` in a bounded verification when clip effects matter. For an Activation track, read the bound GameObject's `activeSelf` before and after evaluation at times inside and outside the clip.
