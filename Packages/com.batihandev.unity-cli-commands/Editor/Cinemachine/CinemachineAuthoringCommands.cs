using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Cinemachine;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BatihanDev.UnityCliCommands.CinemachineAuthoring
{
    [Serializable]
    public sealed class CinemachineStageResult
    {
        public string Camera { get; set; }
        public string Stage { get; set; }
        public string PreviousType { get; set; }
        public string Component { get; set; }
        public string ComponentType { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public bool SceneDirty { get; set; }
    }

    [Serializable]
    public sealed class CinemachineGroupResult
    {
        public string Group { get; set; }
        public string Target { get; set; }
        public string Action { get; set; }
        public string[] OrderedTargets { get; set; }
        public float Weight { get; set; }
        public float Radius { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public bool SceneDirty { get; set; }
    }

    [Serializable]
    public sealed class CinemachineImpulseResult
    {
        public string Source { get; set; }
        public float[] Velocity { get; set; }
        public bool Generated { get; set; }
        public bool DryRun { get; set; }
    }

    public static class CinemachineAuthoringCommands
    {
        private const string StageSchema = "unity.cinemachine.stage-set@1";
        private const string GroupSchema = "unity.cinemachine.group-member@1";
        private const string ImpulseSchema = "unity.cinemachine.impulse-generate@1";

        private static CommandResult<T> Fail<T>(string schema, string code, string message) =>
            CommandResult<T>.Failure(schema, code, message);

        private static bool RegularScene(UnityEngine.Component component)
        {
            if (component == null || EditorUtility.IsPersistent(component)) return false;
            var scene = component.gameObject.scene;
            return scene.IsValid() && scene.isLoaded && !EditorSceneManager.IsPreviewSceneObject(component) &&
                PrefabStageUtility.GetPrefabStage(component.gameObject) == null;
        }

        private static bool FiniteNonnegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;

        [CliCommand("cinemachine.stage-set", "Set one camera pipeline stage using an exact camera and concrete component type.", Tags = new[] { "unity-cli-commands", "cinemachine" })]
        public static CommandResult<CinemachineStageResult> SetStage(
            [CliArg("camera", "Exact loaded CinemachineCamera component.", Required = true)] string camera,
            [CliArg("stage", "Body, Aim, or Noise.", Required = true)] string stage,
            [CliArg("componentType", "Concrete full CinemachineComponentBase type name or None.", Required = true)] string componentType,
            bool confirm = false, bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<CinemachineStageResult>.Failure(StageSchema, compatibility.Error);
            var selected = Editor.ExactObjectReference.Resolve<CinemachineCamera>(camera);
            if (!RegularScene(selected)) return Fail<CinemachineStageResult>(StageSchema, "CAMERA_SCENE_REQUIRED", "Select an exact CinemachineCamera in a loaded regular scene.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return Fail<CinemachineStageResult>(StageSchema, "EDIT_MODE_REQUIRED", "Stage changes require Edit mode.");
            if (!Enum.TryParse(stage, true, out CinemachineCore.Stage selectedStage) ||
                (selectedStage != CinemachineCore.Stage.Body && selectedStage != CinemachineCore.Stage.Aim && selectedStage != CinemachineCore.Stage.Noise) ||
                !Enum.GetNames(typeof(CinemachineCore.Stage)).Any(n => string.Equals(n, stage, StringComparison.OrdinalIgnoreCase)))
                return Fail<CinemachineStageResult>(StageSchema, "STAGE_INVALID", "Use Body, Aim, or Noise.");

            var remove = string.Equals(componentType, "None", StringComparison.OrdinalIgnoreCase);
            Type replacementType = null;
            if (!remove)
            {
                replacementType = typeof(CinemachineCamera).Assembly.GetTypes()
                    .FirstOrDefault(t => string.Equals(t.FullName, componentType, StringComparison.Ordinal));
                if (replacementType == null || !replacementType.IsClass || replacementType.IsAbstract ||
                    !replacementType.IsPublic || !typeof(CinemachineComponentBase).IsAssignableFrom(replacementType))
                    return Fail<CinemachineStageResult>(StageSchema, "COMPONENT_TYPE_INVALID", "Supply one public concrete Cinemachine pipeline component full type name.");
                var attribute = Attribute.GetCustomAttribute(replacementType, typeof(CameraPipelineAttribute), true) as CameraPipelineAttribute;
                if (attribute == null || attribute.Stage != selectedStage)
                    return Fail<CinemachineStageResult>(StageSchema, "STAGE_TYPE_MISMATCH", "The selected component type does not belong to the requested pipeline stage.");
            }
            var existing = selected.GetComponents<CinemachineComponentBase>().Where(c => c != null && c.Stage == selectedStage).ToArray();
            if (existing.Length > 1)
                return Fail<CinemachineStageResult>(StageSchema, "STAGE_AMBIGUOUS", "More than one component occupies this stage; select and remove duplicates explicitly.");
            var previous = existing.SingleOrDefault();
            var result = new CinemachineStageResult
            {
                Camera = Editor.ExactObjectReference.ExactId(selected), Stage = selectedStage.ToString(),
                PreviousType = previous == null ? null : previous.GetType().FullName,
                Component = Editor.ExactObjectReference.ExactId(previous), ComponentType = previous == null ? null : previous.GetType().FullName,
                DryRun = dryRun, SceneDirty = selected.gameObject.scene.isDirty
            };
            if (dryRun) return CommandResult<CinemachineStageResult>.Success(StageSchema, result);
            if (!confirm) return Fail<CinemachineStageResult>(StageSchema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.");
            if ((remove && previous == null) || (previous != null && previous.GetType() == replacementType))
                return CommandResult<CinemachineStageResult>.Success(StageSchema, result);

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Set Cinemachine Stage");
            try
            {
                if (previous != null)
                {
                    var removed = BatihanDev.UnityCliCommands.Component.ComponentAuthoringCommands.Remove(Editor.ExactObjectReference.ExactId(previous));
                    if (!removed.Ok)
                    {
                        Undo.RevertAllDownToGroup(undoGroup);
                        return Fail<CinemachineStageResult>(StageSchema, removed.Error.Code, removed.Error.Message);
                    }
                }
                if (!remove)
                {
                    var added = Undo.AddComponent(selected.gameObject, replacementType) as CinemachineComponentBase;
                    if (added == null || added.Stage != selectedStage)
                        throw new InvalidOperationException("Unity did not add the requested pipeline stage component.");
                }
                var actual = selected.GetComponents<CinemachineComponentBase>().Where(c => c != null && c.Stage == selectedStage).ToArray();
                if (actual.Length != (remove ? 0 : 1) || (!remove && actual[0].GetType() != replacementType))
                    throw new InvalidOperationException("The requested pipeline stage was not applied.");
                PrefabUtility.RecordPrefabInstancePropertyModifications(selected);
                EditorSceneManager.MarkSceneDirty(selected.gameObject.scene);
                Undo.CollapseUndoOperations(undoGroup);
                result.Component = actual.Length == 0 ? null : Editor.ExactObjectReference.ExactId(actual[0]);
                result.ComponentType = actual.Length == 0 ? null : actual[0].GetType().FullName;
                result.Applied = true;
                result.SceneDirty = selected.gameObject.scene.isDirty;
                return CommandResult<CinemachineStageResult>.Success(StageSchema, result);
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                return Fail<CinemachineStageResult>(StageSchema, "STAGE_SET_FAILED", exception.Message);
            }
        }

        [CliCommand("cinemachine.group-member", "Upsert or remove one exact target group member.", Tags = new[] { "unity-cli-commands", "cinemachine" })]
        public static CommandResult<CinemachineGroupResult> GroupMember(
            [CliArg("group", "Exact loaded CinemachineTargetGroup component.", Required = true)] string group,
            [CliArg("target", "Exact loaded Transform or GameObject.", Required = true)] string target,
            [CliArg("action", "upsert or remove.", Required = true)] string action,
            float weight = 1, float radius = 1, bool confirm = false, bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<CinemachineGroupResult>.Failure(GroupSchema, compatibility.Error);
            var selected = Editor.ExactObjectReference.Resolve<CinemachineTargetGroup>(group);
            var targetTransform = Editor.ExactObjectReference.Resolve<UnityEngine.Transform>(target) ??
                Editor.ExactObjectReference.Resolve<GameObject>(target)?.transform;
            if (!RegularScene(selected) || !RegularScene(targetTransform) || selected.gameObject.scene != targetTransform.gameObject.scene)
                return Fail<CinemachineGroupResult>(GroupSchema, "TARGET_SCENE_REQUIRED", "Group and target must be exact objects in the same loaded regular scene.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return Fail<CinemachineGroupResult>(GroupSchema, "EDIT_MODE_REQUIRED", "Group authoring requires Edit mode.");
            var upsert = string.Equals(action, "upsert", StringComparison.OrdinalIgnoreCase);
            if (!upsert && !string.Equals(action, "remove", StringComparison.OrdinalIgnoreCase))
                return Fail<CinemachineGroupResult>(GroupSchema, "ACTION_INVALID", "Use upsert or remove.");
            if (!FiniteNonnegative(weight) || !FiniteNonnegative(radius))
                return Fail<CinemachineGroupResult>(GroupSchema, "MEMBER_VALUE_INVALID", "Weight and radius must be finite and nonnegative.");
            var matches = selected.Targets.Count(t => t != null && t.Object == targetTransform);
            if (matches > 1)
                return Fail<CinemachineGroupResult>(GroupSchema, "MEMBER_AMBIGUOUS", "The target occurs more than once in the group.");
            var result = new CinemachineGroupResult
            {
                Group = Editor.ExactObjectReference.ExactId(selected), Target = Editor.ExactObjectReference.ExactId(targetTransform),
                Action = upsert ? "upsert" : "remove", Weight = weight, Radius = radius,
                OrderedTargets = selected.Targets.Select(t => Editor.ExactObjectReference.ExactId(t?.Object)).ToArray(),
                DryRun = dryRun, SceneDirty = selected.gameObject.scene.isDirty
            };
            if (dryRun) return CommandResult<CinemachineGroupResult>.Success(GroupSchema, result);
            if (!confirm) return Fail<CinemachineGroupResult>(GroupSchema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.");
            if (!upsert && matches == 0) return CommandResult<CinemachineGroupResult>.Success(GroupSchema, result);
            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Set Cinemachine Group Member");
            try
            {
                Undo.RecordObject(selected, "Set Cinemachine Group Member");
                if (matches == 1) selected.RemoveMember(targetTransform);
                if (upsert) selected.AddMember(targetTransform, weight, radius);
                EditorUtility.SetDirty(selected);
                PrefabUtility.RecordPrefabInstancePropertyModifications(selected);
                EditorSceneManager.MarkSceneDirty(selected.gameObject.scene);
                Undo.CollapseUndoOperations(undoGroup);
                result.OrderedTargets = selected.Targets.Select(t => Editor.ExactObjectReference.ExactId(t?.Object)).ToArray();
                result.Applied = true;
                result.SceneDirty = selected.gameObject.scene.isDirty;
                return CommandResult<CinemachineGroupResult>.Success(GroupSchema, result);
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                return Fail<CinemachineGroupResult>(GroupSchema, "GROUP_MEMBER_FAILED", exception.Message);
            }
        }

        [CliCommand("cinemachine.impulse-generate", "Broadcast one transient impulse from an exact scene source.", Tags = new[] { "unity-cli-commands", "cinemachine" })]
        public static CommandResult<CinemachineImpulseResult> GenerateImpulse(
            [CliArg("source", "Exact loaded CinemachineImpulseSource component.", Required = true)] string source,
            float velocityX = 0, float velocityY = -1, float velocityZ = 0,
            bool confirm = false, bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<CinemachineImpulseResult>.Failure(ImpulseSchema, compatibility.Error);
            var selected = Editor.ExactObjectReference.Resolve<CinemachineImpulseSource>(source);
            if (!RegularScene(selected))
                return Fail<CinemachineImpulseResult>(ImpulseSchema, "SOURCE_SCENE_REQUIRED", "Select an exact impulse source in a loaded regular scene.");
            if (float.IsNaN(velocityX) || float.IsInfinity(velocityX) || float.IsNaN(velocityY) || float.IsInfinity(velocityY) ||
                float.IsNaN(velocityZ) || float.IsInfinity(velocityZ))
                return Fail<CinemachineImpulseResult>(ImpulseSchema, "VELOCITY_INVALID", "Velocity components must be finite.");
            var result = new CinemachineImpulseResult
            {
                Source = Editor.ExactObjectReference.ExactId(selected),
                Velocity = new[] { velocityX, velocityY, velocityZ }, DryRun = dryRun
            };
            if (dryRun) return CommandResult<CinemachineImpulseResult>.Success(ImpulseSchema, result);
            if (!confirm) return Fail<CinemachineImpulseResult>(ImpulseSchema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.");
            if (selected.ImpulseDefinition == null)
                return Fail<CinemachineImpulseResult>(ImpulseSchema, "IMPULSE_DEFINITION_MISSING", "The source has no impulse definition.");
            try
            {
                var impulse = selected.ImpulseDefinition.CreateAndReturnEvent(selected.transform.position,
                    new Vector3(velocityX, velocityY, velocityZ));
                if (impulse == null)
                    return Fail<CinemachineImpulseResult>(ImpulseSchema, "IMPULSE_EVENT_NOT_CREATED", "The impulse definition did not create an event; inspect duration, shape and dissipation.");
                result.Generated = true;
                return CommandResult<CinemachineImpulseResult>.Success(ImpulseSchema, result);
            }
            catch (Exception exception)
            {
                return Fail<CinemachineImpulseResult>(ImpulseSchema, "IMPULSE_GENERATION_FAILED", exception.Message);
            }
        }
    }
}
