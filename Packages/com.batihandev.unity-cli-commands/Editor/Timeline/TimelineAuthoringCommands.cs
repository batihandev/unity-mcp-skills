using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityObject = UnityEngine.Object;

namespace BatihanDev.UnityCliCommands.TimelineAuthoring
{
    internal static class TimelineCore
    {
        internal static TimelineAsset Asset(string reference, string schema, out CommandResult<TimelineResult> failure)
        {
            failure = null;
            var asset = Editor.ExactObjectReference.Resolve<TimelineAsset>(reference);
            if (asset == null)
            {
                failure = Fail(schema, "TIMELINE_NOT_FOUND", "The exact TimelineAsset reference is stale or has the wrong type.");
                return null;
            }
            var path = AssetDatabase.GetAssetPath(asset);
            var guard = ProjectPathPolicy.Validate(path);
            if (!guard.Ok || !string.Equals(path, guard.Result.Path, StringComparison.Ordinal) ||
                !path.EndsWith(".playable", StringComparison.OrdinalIgnoreCase) ||
                AssetDatabase.LoadMainAssetAtPath(path) != asset)
            {
                failure = Fail(schema, "TIMELINE_PATH_REFUSED", "The TimelineAsset must be the main asset at a confined Assets .playable path.");
                return null;
            }
            return asset;
        }

        internal static TrackAsset Track(TimelineAsset asset, string name, string schema, out CommandResult<TimelineResult> failure)
        {
            failure = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                failure = Fail(schema, "TRACK_NAME_REQUIRED", "An exact output-track name is required.");
                return null;
            }
            var matches = asset.GetOutputTracks().Where(t => t != null && string.Equals(t.name, name, StringComparison.Ordinal)).Take(2).ToArray();
            if (matches.Length != 1)
            {
                failure = Fail(schema, matches.Length == 0 ? "TRACK_NOT_FOUND" : "TRACK_AMBIGUOUS",
                    "The track name must select exactly one output track.");
                return null;
            }
            return matches[0];
        }

        internal static PlayableDirector Director(string reference, string schema, out CommandResult<TimelineResult> failure)
        {
            failure = null;
            var director = Editor.ExactObjectReference.Resolve<PlayableDirector>(reference);
            if (director == null)
            {
                failure = Fail(schema, "DIRECTOR_NOT_FOUND", "The exact PlayableDirector reference is stale or has the wrong type.");
                return null;
            }
            var scene = director.gameObject.scene;
            if (EditorUtility.IsPersistent(director) || !scene.IsValid() || !scene.isLoaded ||
                EditorSceneManager.IsPreviewSceneObject(director) || PrefabStageUtility.GetPrefabStage(director.gameObject) != null)
            {
                failure = Fail(schema, "REGULAR_SCENE_REQUIRED", "The Director must be in a regular loaded scene.");
                return null;
            }
            return director;
        }

        internal static bool Edit(string schema, out CommandResult<TimelineResult> failure)
        {
            failure = null;
            if (!EditorApplication.isPlayingOrWillChangePlaymode) return true;
            failure = Fail(schema, "EDIT_MODE_REQUIRED", "Asset authoring requires Edit mode.");
            return false;
        }

        internal static CommandResult<TimelineResult> Fail(string schema, string code, string message) =>
            CommandResult<TimelineResult>.Failure(schema, code, message);

        internal static TimelineResult State(TimelineAsset asset, TrackAsset track = null, PlayableDirector director = null,
            bool applied = false, bool dryRun = false)
        {
            return new TimelineResult
            {
                AssetPath = asset == null ? null : AssetDatabase.GetAssetPath(asset),
                Timeline = Editor.ExactObjectReference.ExactId(asset),
                Track = track == null ? null : track.name,
                TrackType = track == null ? null : track.GetType().FullName,
                ClipCount = track == null ? -1 : track.GetClips().Count(),
                Director = Editor.ExactObjectReference.ExactId(director),
                ScenePath = director == null ? null : director.gameObject.scene.path,
                SceneDirty = director != null && director.gameObject.scene.isDirty,
                FixedDuration = asset == null ? 0 : asset.fixedDuration,
                Duration = asset == null ? 0 : asset.duration,
                DurationMode = asset == null ? null : asset.durationMode.ToString(),
                WrapMode = director == null ? null : director.extrapolationMode.ToString(),
                Applied = applied, DryRun = dryRun
            };
        }
    }

    public static class TimelineAuthoringCommands
    {
        private const string BindSchema = "unity.timeline.binding-set@1";
        private const string ClipSchema = "unity.timeline.default-clip-create@1";
        private const string RemoveSchema = "unity.timeline.track-remove@1";
        private const string DurationSchema = "unity.timeline.duration-set@1";
        private const string ControlSchema = "unity.timeline.director-control@1";

        [CliCommand("timeline.binding-set", "Bind one exact scene target to one exact output track on a Director.", Tags = new[] { "unity-cli-commands", "timeline" })]
        public static CommandResult<TimelineResult> SetBinding(
            [CliArg("director", "Exact loaded PlayableDirector component.", Required = true)] string director,
            [CliArg("track", "Exact output track name.", Required = true)] string track,
            [CliArg("target", "Exact loaded GameObject or component.", Required = true)] string target,
            [CliArg("autoAddAnimator", "Add Animator to the selected GameObject when its Animation track needs one.")] bool autoAddAnimator = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<TimelineResult>.Failure(BindSchema, compatibility.Error);
            var selected = TimelineCore.Director(director, BindSchema, out var failure);
            if (selected == null) return failure;
            var asset = selected.playableAsset as TimelineAsset;
            if (asset == null) return TimelineCore.Fail(BindSchema, "DIRECTOR_TIMELINE_REQUIRED", "The Director must reference a TimelineAsset.");
            asset = TimelineCore.Asset(Editor.ExactObjectReference.ExactId(asset), BindSchema, out failure);
            if (asset == null) return failure;
            var output = TimelineCore.Track(asset, track, BindSchema, out failure);
            if (output == null) return failure;
            var binding = Attribute.GetCustomAttribute(output.GetType(), typeof(TrackBindingTypeAttribute), true) as TrackBindingTypeAttribute;
            if (binding == null || binding.type == null)
                return TimelineCore.Fail(BindSchema, "TRACK_BINDING_UNSUPPORTED", "This track has no public binding type.");
            var exact = Editor.ExactObjectReference.Resolve<UnityObject>(target);
            var go = exact as GameObject ?? (exact as UnityEngine.Component)?.gameObject;
            if (go == null)
                return TimelineCore.Fail(BindSchema, "TARGET_NOT_FOUND", "The target must be an exact GameObject or component.");
            var scene = go.scene;
            if (EditorUtility.IsPersistent(go) || !scene.IsValid() || !scene.isLoaded ||
                EditorSceneManager.IsPreviewSceneObject(go) || PrefabStageUtility.GetPrefabStage(go) != null ||
                scene != selected.gameObject.scene)
                return TimelineCore.Fail(BindSchema, "TARGET_SCENE_REFUSED", "The target must be in the same regular loaded scene as the Director.");
            UnityObject bound = null;
            var addAnimator = false;
            if (binding.type == typeof(GameObject))
            {
                if (exact is UnityEngine.Component) return TimelineCore.Fail(BindSchema, "TARGET_TYPE_REFUSED", "This track requires an exact GameObject.");
                bound = go;
            }
            else if (typeof(UnityEngine.Component).IsAssignableFrom(binding.type))
            {
                if (exact is UnityEngine.Component component)
                {
                    if (!binding.type.IsInstanceOfType(component))
                        return TimelineCore.Fail(BindSchema, "TARGET_TYPE_REFUSED", "The component is not the track's binding type.");
                    bound = component;
                }
                else
                {
                    var matches = go.GetComponents(binding.type);
                    if (matches.Length > 1)
                        return TimelineCore.Fail(BindSchema, "TARGET_AMBIGUOUS", "More than one matching component exists; supply its exact reference.");
                    if (matches.Length == 1) bound = matches[0];
                    else if (binding.type == typeof(Animator) && autoAddAnimator) addAnimator = true;
                    else return TimelineCore.Fail(BindSchema, "TARGET_COMPONENT_MISSING", "The required binding component is absent.");
                }
            }
            else return TimelineCore.Fail(BindSchema, "TRACK_BINDING_UNSUPPORTED", "The track binding type is unsupported.");
            if (autoAddAnimator && binding.type != typeof(Animator))
                return TimelineCore.Fail(BindSchema, "AUTO_ADD_UNSUPPORTED", "autoAddAnimator applies only to Animator bindings.");
            if (!TimelineCore.Edit(BindSchema, out failure)) return failure;
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Set Timeline Binding");
            Undo.RecordObject(selected, "Set Timeline Binding");
            if (addAnimator) bound = Undo.AddComponent<Animator>(go);
            selected.SetGenericBinding(output, bound);
            PrefabUtility.RecordPrefabInstancePropertyModifications(selected);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            var result = TimelineCore.State(asset, output, selected, true);
            var actual = selected.GetGenericBinding(output);
            result.BoundObject = Editor.ExactObjectReference.ExactId(actual);
            result.BoundType = actual == null ? null : actual.GetType().FullName;
            result.AnimatorAdded = addAnimator;
            return CommandResult<TimelineResult>.Success(BindSchema, result);
        }

        [CliCommand("timeline.default-clip-create", "Create a source-free default Timeline clip on an exact output track.", Tags = new[] { "unity-cli-commands", "timeline" })]
        public static CommandResult<TimelineResult> CreateDefaultClip(
            [CliArg("timeline", "Exact TimelineAsset reference.", Required = true)] string timeline,
            [CliArg("track", "Exact output track name.", Required = true)] string track,
            double start = 0, double duration = 1, bool confirm = false, bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<TimelineResult>.Failure(ClipSchema, compatibility.Error);
            var asset = TimelineCore.Asset(timeline, ClipSchema, out var failure);
            if (asset == null) return failure;
            var output = TimelineCore.Track(asset, track, ClipSchema, out failure);
            if (output == null) return failure;
            if (!Finite(start) || start < 0 || !Finite(duration) || duration <= 0)
                return TimelineCore.Fail(ClipSchema, "CLIP_TIME_INVALID", "start must be finite and nonnegative; duration must be finite and positive.");
            var playableType = output.GetType().GetCustomAttributes(typeof(TrackClipTypeAttribute), true)
                .OfType<TrackClipTypeAttribute>().Select(a => a.inspectedType)
                .FirstOrDefault(t => t != null && typeof(UnityEngine.ScriptableObject).IsAssignableFrom(t) && typeof(IPlayableAsset).IsAssignableFrom(t));
            if (playableType == null)
                return TimelineCore.Fail(ClipSchema, "DEFAULT_CLIP_UNSUPPORTED", "The track has no ScriptableObject IPlayableAsset default clip type.");
            if (!TimelineCore.Edit(ClipSchema, out failure)) return failure;
            if (dryRun) return CommandResult<TimelineResult>.Success(ClipSchema, TimelineCore.State(asset, output, dryRun: true));
            if (!confirm) return TimelineCore.Fail(ClipSchema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.");
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Timeline Clip");
            var clip = output.CreateDefaultClip();
            if (clip == null) return TimelineCore.Fail(ClipSchema, "DEFAULT_CLIP_FAILED", "Timeline did not create a default clip.");
            Undo.RecordObject(output, "Set Timeline Clip Timing");
            clip.start = start;
            clip.duration = duration;
            EditorUtility.SetDirty(output);
            EditorUtility.SetDirty(asset);
            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssetIfDirty(asset);
            var result = TimelineCore.State(asset, output, applied: true);
            result.ClipStart = clip.start;
            result.ClipDuration = clip.duration;
            result.ClipAssetType = clip.asset == null ? null : clip.asset.GetType().FullName;
            return CommandResult<TimelineResult>.Success(ClipSchema, result);
        }

        [CliCommand("timeline.track-remove", "Delete one exact output track and its owned children/clips from a TimelineAsset.", Tags = new[] { "unity-cli-commands", "timeline" })]
        public static CommandResult<TimelineResult> RemoveTrack(
            [CliArg("timeline", "Exact TimelineAsset reference.", Required = true)] string timeline,
            [CliArg("track", "Exact output track name.", Required = true)] string track,
            bool confirm = false, bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<TimelineResult>.Failure(RemoveSchema, compatibility.Error);
            var asset = TimelineCore.Asset(timeline, RemoveSchema, out var failure);
            if (asset == null) return failure;
            var output = TimelineCore.Track(asset, track, RemoveSchema, out failure);
            if (output == null) return failure;
            if (!TimelineCore.Edit(RemoveSchema, out failure)) return failure;
            var before = TimelineCore.State(asset, output, dryRun: dryRun);
            if (dryRun) return CommandResult<TimelineResult>.Success(RemoveSchema, before);
            if (!confirm) return TimelineCore.Fail(RemoveSchema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.");
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove Timeline Track");
            if (!asset.DeleteTrack(output)) return TimelineCore.Fail(RemoveSchema, "TRACK_REMOVE_FAILED", "Timeline refused to delete the track.");
            EditorUtility.SetDirty(asset);
            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssetIfDirty(asset);
            before.Applied = true;
            return CommandResult<TimelineResult>.Success(RemoveSchema, before);
        }

        [CliCommand("timeline.duration-set", "Set fixed Timeline duration and optional Director wrap mode.", Tags = new[] { "unity-cli-commands", "timeline" })]
        public static CommandResult<TimelineResult> SetDuration(
            [CliArg("director", "Exact loaded PlayableDirector component.", Required = true)] string director,
            double duration = 0, string wrapMode = null, bool confirm = false, bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<TimelineResult>.Failure(DurationSchema, compatibility.Error);
            var selected = TimelineCore.Director(director, DurationSchema, out var failure);
            if (selected == null) return failure;
            var asset = selected.playableAsset as TimelineAsset;
            if (asset == null) return TimelineCore.Fail(DurationSchema, "DIRECTOR_TIMELINE_REQUIRED", "The Director must reference a TimelineAsset.");
            asset = TimelineCore.Asset(Editor.ExactObjectReference.ExactId(asset), DurationSchema, out failure);
            if (asset == null) return failure;
            if (!Finite(duration) || duration < 0)
                return TimelineCore.Fail(DurationSchema, "DURATION_INVALID", "duration must be finite and nonnegative.");
            DirectorWrapMode? selectedWrap = null;
            if (wrapMode != null)
            {
                if (string.Equals(wrapMode, "Hold", StringComparison.OrdinalIgnoreCase)) selectedWrap = DirectorWrapMode.Hold;
                else if (string.Equals(wrapMode, "Loop", StringComparison.OrdinalIgnoreCase)) selectedWrap = DirectorWrapMode.Loop;
                else if (string.Equals(wrapMode, "None", StringComparison.OrdinalIgnoreCase)) selectedWrap = DirectorWrapMode.None;
                else return TimelineCore.Fail(DurationSchema, "WRAP_MODE_INVALID", "wrapMode must be Hold, Loop, or None.");
            }
            if (!TimelineCore.Edit(DurationSchema, out failure)) return failure;
            if (dryRun) return CommandResult<TimelineResult>.Success(DurationSchema, TimelineCore.State(asset, director: selected, dryRun: true));
            if (!confirm) return TimelineCore.Fail(DurationSchema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.");
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Set Timeline Duration");
            Undo.RecordObjects(new UnityObject[] { asset, selected }, "Set Timeline Duration");
            asset.durationMode = TimelineAsset.DurationMode.FixedLength;
            asset.fixedDuration = duration;
            if (selectedWrap.HasValue) selected.extrapolationMode = selectedWrap.Value;
            EditorUtility.SetDirty(asset);
            PrefabUtility.RecordPrefabInstancePropertyModifications(selected);
            EditorSceneManager.MarkSceneDirty(selected.gameObject.scene);
            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssetIfDirty(asset);
            return CommandResult<TimelineResult>.Success(DurationSchema, TimelineCore.State(asset, director: selected, applied: true));
        }

        [CliCommand("timeline.director-control", "Play, pause, or stop an exact loaded PlayableDirector.", Tags = new[] { "unity-cli-commands", "timeline" })]
        public static CommandResult<TimelineResult> Control(
            [CliArg("director", "Exact loaded PlayableDirector component.", Required = true)] string director,
            string action = "play")
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<TimelineResult>.Failure(ControlSchema, compatibility.Error);
            var selected = TimelineCore.Director(director, ControlSchema, out var failure);
            if (selected == null) return failure;
            var asset = selected.playableAsset as TimelineAsset;
            if (asset == null) return TimelineCore.Fail(ControlSchema, "DIRECTOR_TIMELINE_REQUIRED", "The Director must reference a TimelineAsset.");
            var normalized = action?.ToLowerInvariant();
            if (normalized != "play" && normalized != "pause" && normalized != "stop")
                return TimelineCore.Fail(ControlSchema, "ACTION_INVALID", "action must be play, pause, or stop.");
            var beforeState = selected.state.ToString();
            var beforeTime = selected.time;
            if (normalized == "play") selected.Play();
            else if (normalized == "pause") selected.Pause();
            else selected.Stop();
            var result = TimelineCore.State(asset, director: selected, applied: true);
            result.Action = normalized;
            result.BeforeState = beforeState;
            result.BeforeTime = beforeTime;
            result.AfterState = selected.state.ToString();
            result.AfterTime = selected.time;
            result.IsPlaying = selected.state == PlayState.Playing;
            return CommandResult<TimelineResult>.Success(ControlSchema, result);
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    [Serializable]
    public sealed class TimelineResult
    {
        public string AssetPath { get; set; }
        public string Timeline { get; set; }
        public string Track { get; set; }
        public string TrackType { get; set; }
        public int ClipCount { get; set; }
        public string Director { get; set; }
        public string ScenePath { get; set; }
        public bool SceneDirty { get; set; }
        public string BoundObject { get; set; }
        public string BoundType { get; set; }
        public bool AnimatorAdded { get; set; }
        public double ClipStart { get; set; }
        public double ClipDuration { get; set; }
        public string ClipAssetType { get; set; }
        public double FixedDuration { get; set; }
        public double Duration { get; set; }
        public string DurationMode { get; set; }
        public string WrapMode { get; set; }
        public string Action { get; set; }
        public string BeforeState { get; set; }
        public double BeforeTime { get; set; }
        public string AfterState { get; set; }
        public double AfterTime { get; set; }
        public bool IsPlaying { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
    }
}
