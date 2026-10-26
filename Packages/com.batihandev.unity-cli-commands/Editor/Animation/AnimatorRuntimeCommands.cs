using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Animation
{
    public static class AnimatorRuntimeCommands
    {
        private const string ParameterSchema = "unity.animator.runtime-set-parameter@1";
        private const string PlaySchema = "unity.animator.runtime-play@1";

        [CliCommand("animator.runtime-set-parameter",
            "Set one exact Animator float, int, bool, or trigger parameter during Play Mode.",
            Tags = new[] { "unity-cli-commands", "animation", "animator" })]
        public static CommandResult<AnimatorParameterSetResult> SetParameter(
            [CliArg("target", "Exact Animator component EntityId or GlobalObjectId.", Required = true)] string target,
            [CliArg("name", "Exact controller parameter name.", Required = true)] string name,
            [CliArg("type", "Expected parameter type: float, int, bool, or trigger.")] string type = "float",
            [CliArg("floatValue", "Float value when type is float.")] float floatValue = 0f,
            [CliArg("intValue", "Integer value when type is int.")] int intValue = 0,
            [CliArg("boolValue", "Boolean value when type is bool.")] bool boolValue = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<AnimatorParameterSetResult>.Failure(ParameterSchema, compatibility.Error);
            if (!EditorApplication.isPlaying)
                return Failure<AnimatorParameterSetResult>(ParameterSchema, "PLAY_MODE_REQUIRED", "Animator runtime changes require Play Mode.", target);

            var animator = Editor.ExactObjectReference.Resolve<Animator>(target);
            if (animator == null)
                return Failure<AnimatorParameterSetResult>(ParameterSchema, "ANIMATOR_NOT_FOUND", "The exact Animator reference is stale or does not identify an Animator component.", target);
            if (animator.runtimeAnimatorController == null)
                return Failure<AnimatorParameterSetResult>(ParameterSchema, "CONTROLLER_REQUIRED", "The selected Animator has no runtime controller.", target);
            if (!animator.isInitialized)
                return Failure<AnimatorParameterSetResult>(ParameterSchema, "ANIMATOR_NOT_INITIALIZED", "The selected Animator is not initialized for runtime evaluation.", target);
            if (string.IsNullOrWhiteSpace(name))
                return Failure<AnimatorParameterSetResult>(ParameterSchema, "PARAMETER_NAME_REQUIRED", "A parameter name is required.", target);

            if (!TryParameterType(type, out var requestedType))
                return Failure<AnimatorParameterSetResult>(ParameterSchema, "PARAMETER_TYPE_INVALID", "type must be float, int, bool, or trigger.", target);
            if (requestedType == AnimatorControllerParameterType.Float && !Finite(floatValue))
                return Failure<AnimatorParameterSetResult>(ParameterSchema, "PARAMETER_VALUE_INVALID", "floatValue must be finite.", target);

            var matches = animator.parameters.Where(item => string.Equals(item.name, name, StringComparison.Ordinal)).ToArray();
            if (matches.Length == 0)
                return Failure<AnimatorParameterSetResult>(ParameterSchema, "PARAMETER_NOT_FOUND", "The exact parameter name is absent from the current controller.", target, name);
            if (matches.Length != 1)
                return Failure<AnimatorParameterSetResult>(ParameterSchema, "PARAMETER_AMBIGUOUS", "The current controller contains more than one parameter with that exact name.", target, name);
            if (matches[0].type != requestedType)
                return Failure<AnimatorParameterSetResult>(ParameterSchema, "PARAMETER_TYPE_MISMATCH", "type does not match the selected controller parameter.", target, name);

            object actualValue = null;
            switch (requestedType)
            {
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(name, floatValue);
                    actualValue = animator.GetFloat(name);
                    if (!Mathf.Approximately((float)actualValue, floatValue))
                        return PostconditionFailure<AnimatorParameterSetResult>(ParameterSchema, "PARAMETER_READBACK_MISMATCH", target, name, actualValue);
                    break;
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(name, intValue);
                    actualValue = animator.GetInteger(name);
                    if ((int)actualValue != intValue)
                        return PostconditionFailure<AnimatorParameterSetResult>(ParameterSchema, "PARAMETER_READBACK_MISMATCH", target, name, actualValue);
                    break;
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(name, boolValue);
                    actualValue = animator.GetBool(name);
                    if ((bool)actualValue != boolValue)
                        return PostconditionFailure<AnimatorParameterSetResult>(ParameterSchema, "PARAMETER_READBACK_MISMATCH", target, name, actualValue);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    animator.SetTrigger(name);
                    actualValue = "trigger-set";
                    break;
            }

            return CommandResult<AnimatorParameterSetResult>.Success(ParameterSchema, new AnimatorParameterSetResult
            {
                Target = Editor.ExactObjectReference.ExactId(animator),
                Parameter = name,
                Type = requestedType.ToString().ToLowerInvariant(),
                Value = actualValue,
                Applied = true,
                ReadBack = requestedType != AnimatorControllerParameterType.Trigger
            });
        }

        [CliCommand("animator.runtime-play",
            "Play one exact state on one layer of an exact Animator during Play Mode.",
            Tags = new[] { "unity-cli-commands", "animation", "animator" })]
        public static CommandResult<AnimatorPlayResult> Play(
            [CliArg("target", "Exact Animator component EntityId or GlobalObjectId.", Required = true)] string target,
            [CliArg("stateName", "Exact layer-qualified state path, or a unique bare state name in the selected layer.", Required = true)] string stateName,
            [CliArg("layer", "Layer index, default 0.")] int layer = 0,
            [CliArg("normalizedTime", "Finite normalized playback time, default 0.")] float normalizedTime = 0f)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<AnimatorPlayResult>.Failure(PlaySchema, compatibility.Error);
            if (!EditorApplication.isPlaying)
                return Failure<AnimatorPlayResult>(PlaySchema, "PLAY_MODE_REQUIRED", "Animator playback requires Play Mode.", target);

            var animator = Editor.ExactObjectReference.Resolve<Animator>(target);
            if (animator == null)
                return Failure<AnimatorPlayResult>(PlaySchema, "ANIMATOR_NOT_FOUND", "The exact Animator reference is stale or does not identify an Animator component.", target);
            if (animator.runtimeAnimatorController == null)
                return Failure<AnimatorPlayResult>(PlaySchema, "CONTROLLER_REQUIRED", "The selected Animator has no runtime controller.", target);
            if (!animator.isInitialized)
                return Failure<AnimatorPlayResult>(PlaySchema, "ANIMATOR_NOT_INITIALIZED", "The selected Animator is not initialized for runtime evaluation.", target);
            if (string.IsNullOrWhiteSpace(stateName))
                return Failure<AnimatorPlayResult>(PlaySchema, "STATE_NAME_REQUIRED", "A state name is required.", target);
            if (!Finite(normalizedTime))
                return Failure<AnimatorPlayResult>(PlaySchema, "NORMALIZED_TIME_INVALID", "normalizedTime must be finite.", target, stateName);
            if (layer < 0 || layer >= animator.layerCount)
                return Failure<AnimatorPlayResult>(PlaySchema, "LAYER_NOT_FOUND", "The requested layer index is outside the current controller.", target, stateName);
            if (!TryResolveStatePath(animator.runtimeAnimatorController, layer, stateName, out var resolvedPath, out var ambiguous))
                return Failure<AnimatorPlayResult>(PlaySchema, ambiguous ? "STATE_AMBIGUOUS" : "STATE_NOT_FOUND",
                    ambiguous ? "The bare state name matches more than one state on the requested controller layer." :
                    "The exact state is absent from the requested controller layer.", target, stateName);
            var stateHash = Animator.StringToHash(resolvedPath);
            if (!animator.HasState(layer, stateHash))
                return Failure<AnimatorPlayResult>(PlaySchema, "STATE_NOT_FOUND", "The selected Animator does not report that state on the requested layer.", target, stateName);

            animator.Play(stateHash, layer, normalizedTime);
            var state = animator.GetCurrentAnimatorStateInfo(layer);

            return CommandResult<AnimatorPlayResult>.Success(PlaySchema, new AnimatorPlayResult
            {
                Target = Editor.ExactObjectReference.ExactId(animator),
                State = stateName,
                ResolvedStatePath = resolvedPath,
                Layer = layer,
                RequestedNormalizedTime = normalizedTime,
                ActualNormalizedTime = state.normalizedTime,
                FullPathHash = state.fullPathHash,
                ObservedStateMatched = state.fullPathHash == stateHash,
                Applied = true
            });
        }

        internal static bool TryResolveStatePath(RuntimeAnimatorController runtimeController, int layer, string stateName,
            out string resolvedPath, out bool ambiguous)
        {
            resolvedPath = null;
            ambiguous = false;
            var controller = runtimeController as AnimatorController;
            while (controller == null && runtimeController is AnimatorOverrideController over)
            {
                runtimeController = over.runtimeAnimatorController;
                controller = runtimeController as AnimatorController;
            }
            if (controller == null || layer < 0 || layer >= controller.layers.Length) return false;
            var selectedLayer = controller.layers[layer];
            var stateMachine = selectedLayer.stateMachine;
            if (stateMachine == null) return false;
            var fullMatches = new List<string>();
            var bareMatches = new List<string>();
            FindStatePaths(stateMachine, selectedLayer.name, stateName, fullMatches, bareMatches);
            var matches = fullMatches.Count > 0 ? fullMatches : bareMatches;
            ambiguous = matches.Count > 1;
            if (matches.Count != 1) return false;
            resolvedPath = matches[0];
            return true;
        }

        private static void FindStatePaths(AnimatorStateMachine stateMachine, string prefix, string requested,
            List<string> fullMatches, List<string> bareMatches)
        {
            foreach (var child in stateMachine.states)
            {
                if (child.state == null) continue;
                var path = prefix + "." + child.state.name;
                if (string.Equals(path, requested, StringComparison.Ordinal)) fullMatches.Add(path);
                if (string.Equals(child.state.name, requested, StringComparison.Ordinal)) bareMatches.Add(path);
            }
            foreach (var child in stateMachine.stateMachines)
            {
                if (child.stateMachine == null) continue;
                FindStatePaths(child.stateMachine, prefix + "." + child.stateMachine.name,
                    requested, fullMatches, bareMatches);
            }
        }

        private static bool TryParameterType(string value, out AnimatorControllerParameterType type)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "float": type = AnimatorControllerParameterType.Float; return true;
                case "int": type = AnimatorControllerParameterType.Int; return true;
                case "bool": type = AnimatorControllerParameterType.Bool; return true;
                case "trigger": type = AnimatorControllerParameterType.Trigger; return true;
                default: type = default; return false;
            }
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static CommandResult<T> Failure<T>(string schema, string code, string message, string target, string name = null) =>
            CommandResult<T>.Failure(schema, code, message, new Dictionary<string, object>
            { ["target"] = target ?? string.Empty, ["name"] = name ?? string.Empty });

        private static CommandResult<T> PostconditionFailure<T>(string schema, string code, string target, string name, object value) =>
            CommandResult<T>.Failure(schema, code, "The runtime call completed but its typed readback did not match the requested value.",
                new Dictionary<string, object> { ["target"] = target, ["name"] = name, ["actualValue"] = value });
    }

    [Serializable]
    public sealed class AnimatorParameterSetResult
    {
        public string Target { get; set; }
        public string Parameter { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
        public bool Applied { get; set; }
        public bool ReadBack { get; set; }
    }

    [Serializable]
    public sealed class AnimatorPlayResult
    {
        public string Target { get; set; }
        public string State { get; set; }
        public string ResolvedStatePath { get; set; }
        public int Layer { get; set; }
        public float RequestedNormalizedTime { get; set; }
        public float ActualNormalizedTime { get; set; }
        public int FullPathHash { get; set; }
        public bool ObservedStateMatched { get; set; }
        public bool Applied { get; set; }
    }
}
