using System;
using System.Collections.Generic;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Scene
{
    public static class SceneViewCommands
    {
        private const string InfoSchema = "unity.scene.view-info@1";
        private const string FrameSchema = "unity.scene.view-frame@1";
        private const string AlignSchema = "unity.scene.view-align@1";

        [CliCommand("scene.view-info", "Read the existing active Scene View camera, pivot, size, and projection.", Tags = new[] { "unity-cli-commands", "scene" })]
        public static CommandResult<SceneViewInfoResult> Info()
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<SceneViewInfoResult>.Failure(InfoSchema, compatibility.Error);
            var view = ActiveView();
            if (view == null) return Failure<SceneViewInfoResult>(InfoSchema, "SCENE_VIEW_NOT_FOUND", null);
            return CommandResult<SceneViewInfoResult>.Success(InfoSchema, Read(view));
        }

        [CliCommand("scene.view-frame", "Frame the active Scene View at a world pivot, optionally setting its Euler rotation and size.", Tags = new[] { "unity-cli-commands", "scene" })]
        public static CommandResult<SceneViewFrameResult> Frame(
            [CliArg("pivotX", "World-space view pivot X.", Required = true)] float pivotX,
            [CliArg("pivotY", "World-space view pivot Y.", Required = true)] float pivotY,
            [CliArg("pivotZ", "World-space view pivot Z.", Required = true)] float pivotZ,
            float? rotX = null, float? rotY = null, float? rotZ = null,
            float? size = null, bool instant = true)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<SceneViewFrameResult>.Failure(FrameSchema, compatibility.Error);
            var view = ActiveView();
            if (view == null) return Failure<SceneViewFrameResult>(FrameSchema, "SCENE_VIEW_NOT_FOUND", null);
            if (!Finite(pivotX) || !Finite(pivotY) || !Finite(pivotZ))
                return Failure<SceneViewFrameResult>(FrameSchema, "INVALID_PIVOT", "Pivot coordinates must be finite.");
            var rotationParts = (rotX.HasValue ? 1 : 0) + (rotY.HasValue ? 1 : 0) + (rotZ.HasValue ? 1 : 0);
            if (rotationParts != 0 && rotationParts != 3)
                return Failure<SceneViewFrameResult>(FrameSchema, "INCOMPLETE_ROTATION", "Supply all three Euler coordinates or omit rotation.");
            if ((rotX.HasValue && (!Finite(rotX.Value) || !Finite(rotY.Value) || !Finite(rotZ.Value))) ||
                (size.HasValue && (!Finite(size.Value) || size.Value <= 0f)))
                return Failure<SceneViewFrameResult>(FrameSchema, "INVALID_VIEW_STATE", "Euler coordinates must be finite and size must be positive and finite.");

            var before = Read(view);
            var pivot = new Vector3(pivotX, pivotY, pivotZ);
            var rotation = rotX.HasValue ? Quaternion.Euler(rotX.Value, rotY.Value, rotZ.Value) : view.rotation;
            var viewSize = size ?? view.size;
            if (instant) view.LookAtDirect(pivot, rotation, viewSize);
            else view.LookAt(pivot, rotation, viewSize, before.Orthographic);
            view.Repaint();
            var requested = new SceneViewScalarState { Pivot = ToArray(pivot), Euler = ToArray(rotation.eulerAngles), Size = viewSize, Orthographic = before.Orthographic };
            return CommandResult<SceneViewFrameResult>.Success(FrameSchema, new SceneViewFrameResult
            { Before = before, Requested = requested, Actual = Read(view), Instant = instant });
        }

        [CliCommand("scene.view-align", "Align the active Scene View to one exact GameObject transform.", Tags = new[] { "unity-cli-commands", "scene" })]
        public static CommandResult<SceneViewAlignResult> Align(
            [CliArg("target", "Exact GameObject ObjectRef string.", Required = true)] string target)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<SceneViewAlignResult>.Failure(AlignSchema, compatibility.Error);
            var gameObject = Editor.ExactObjectReference.Resolve<GameObject>(target);
            if (gameObject == null) return Failure<SceneViewAlignResult>(AlignSchema, "TARGET_NOT_FOUND", target);
            var view = ActiveView();
            if (view == null) return Failure<SceneViewAlignResult>(AlignSchema, "SCENE_VIEW_NOT_FOUND", null);
            var before = Read(view);
            view.AlignViewToObject(gameObject.transform);
            view.Repaint();
            return CommandResult<SceneViewAlignResult>.Success(AlignSchema, new SceneViewAlignResult
            { Target = Editor.ExactObjectReference.ExactId(gameObject), Before = before, Requested = Read(view) });
        }

        private static UnityEditor.SceneView ActiveView() => UnityEditor.SceneView.lastActiveSceneView;

        private static SceneViewInfoResult Read(UnityEditor.SceneView view)
        {
            var camera = view.camera;
            return new SceneViewInfoResult
            {
                HasCamera = camera != null,
                CameraPosition = camera == null ? null : ToArray(camera.transform.position),
                CameraEuler = camera == null ? null : ToArray(camera.transform.eulerAngles),
                Pivot = ToArray(view.pivot), Euler = ToArray(view.rotation.eulerAngles),
                Size = view.size, Orthographic = view.orthographic
            };
        }

        private static float[] ToArray(Vector3 value) => new[] { value.x, value.y, value.z };
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static CommandResult<T> Failure<T>(string schema, string code, string detail) =>
            CommandResult<T>.Failure(schema, code, detail ?? "The requested active Scene View or exact target is unavailable.",
                new Dictionary<string, object> { ["detail"] = detail ?? string.Empty });
    }

    [Serializable] public class SceneViewScalarState { public float[] Pivot { get; set; } public float[] Euler { get; set; } public float Size { get; set; } public bool Orthographic { get; set; } }
    [Serializable] public sealed class SceneViewInfoResult : SceneViewScalarState { public bool HasCamera { get; set; } public float[] CameraPosition { get; set; } public float[] CameraEuler { get; set; } }
    [Serializable] public sealed class SceneViewFrameResult { public SceneViewInfoResult Before { get; set; } public SceneViewScalarState Requested { get; set; } public SceneViewInfoResult Actual { get; set; } public bool Instant { get; set; } }
    [Serializable] public sealed class SceneViewAlignResult { public string Target { get; set; } public SceneViewInfoResult Before { get; set; } public SceneViewInfoResult Requested { get; set; } }
}
