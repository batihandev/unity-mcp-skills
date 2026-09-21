using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Build;

namespace BatihanDev.UnityCliCommands.Project
{
    public static class ProjectCommands
    {
        private const string Schema = "unity.project.defines@1";

        [CliCommand("project.defines", "Read or guardedly replace scripting defines for one exact build target group.",
            Tags = new[] { "unity-cli-commands", "project" })]
        public static CommandResult<ProjectDefinesResult> Defines(
            [CliArg("group", "Exact BuildTargetGroup name; required for replacement.")] string group = null,
            [CliArg("defines", "Full semicolon-delimited replacement; omitted reads and empty clears.")] string defines = null,
            [CliArg("dryRun", "Preview without writing; wins over confirmation.")] bool dryRun = false,
            [CliArg("confirm", "Apply the supplied full replacement.")] bool confirm = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<ProjectDefinesResult>.Failure(Schema, compatibility.Error);

            var isReplacement = defines != null;
            if (isReplacement && string.IsNullOrWhiteSpace(group))
                return Failure("BUILD_TARGET_GROUP_REQUIRED", "Replacement requires an explicit build target group.", group);
            if (!TryResolveGroup(group, out var resolvedGroup))
                return Failure("BUILD_TARGET_GROUP_INVALID", "The build target group is not valid.", group);
            if (isReplacement && !dryRun && !confirm)
                return Failure("CONFIRMATION_REQUIRED", "Define replacement requires confirm=true or dryRun=true.", group);

            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(resolvedGroup);
            if (namedTarget == NamedBuildTarget.Unknown)
                return Failure("NAMED_BUILD_TARGET_UNAVAILABLE", "The build target group has no named build target.", group);

            try
            {
                var before = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
                if (isReplacement && !dryRun)
                    PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
                var after = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
                if (isReplacement && !dryRun && !string.Equals(after, defines, StringComparison.Ordinal))
                    return Failure("READBACK_MISMATCH", "The effective define string did not match the replacement.", group);
                return CommandResult<ProjectDefinesResult>.Success(Schema, new ProjectDefinesResult
                {
                    Group = resolvedGroup.ToString(),
                    NamedBuildTarget = namedTarget.TargetName,
                    SelectedGroup = EditorUserBuildSettings.selectedBuildTargetGroup.ToString(),
                    ActiveBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                    Before = before,
                    Requested = isReplacement ? defines : null,
                    After = after,
                    Applied = isReplacement && !dryRun,
                    DryRun = dryRun,
                    RequiresRecompile = isReplacement && !string.Equals(before, defines, StringComparison.Ordinal),
                    Undoable = false
                });
            }
            catch (Exception exception)
            {
                return CommandResult<ProjectDefinesResult>.Failure(Schema,
                    "SCRIPTING_DEFINES_UNAVAILABLE", "Scripting defines are unavailable for the selected target.",
                    new Dictionary<string, object>
                    {
                        ["group"] = resolvedGroup.ToString(),
                        ["exceptionType"] = exception.GetType().Name
                    });
            }
        }

        private static bool TryResolveGroup(string group, out BuildTargetGroup result)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                result = EditorUserBuildSettings.selectedBuildTargetGroup;
                return result != BuildTargetGroup.Unknown;
            }
            result = BuildTargetGroup.Unknown;
            return Enum.GetNames(typeof(BuildTargetGroup)).Contains(group, StringComparer.Ordinal) &&
                Enum.TryParse(group, false, out result) && result != BuildTargetGroup.Unknown &&
                Enum.IsDefined(typeof(BuildTargetGroup), result);
        }

        private static CommandResult<ProjectDefinesResult> Failure(string code, string message, string group) =>
            CommandResult<ProjectDefinesResult>.Failure(Schema, code, message,
                new Dictionary<string, object> { ["group"] = group ?? string.Empty });
    }

    [Serializable]
    public sealed class ProjectDefinesResult
    {
        public string Group { get; set; }
        public string NamedBuildTarget { get; set; }
        public string SelectedGroup { get; set; }
        public string ActiveBuildTarget { get; set; }
        public string Before { get; set; }
        public string Requested { get; set; }
        public string After { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public bool RequiresRecompile { get; set; }
        public bool Undoable { get; set; }
    }
}
