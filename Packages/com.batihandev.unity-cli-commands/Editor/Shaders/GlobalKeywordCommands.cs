using System;
using System.Collections.Generic;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.ShaderAuthoring
{
    public static class GlobalKeywordCommands
    {
        private const string Schema = "unity.shader.global-keyword-set@1";

        [CliCommand("shader.global-keyword-set", "Preview or set one global shader keyword's enabled state.",
            Tags = new[] { "unity-cli-commands", "shader" })]
        public static CommandResult<GlobalKeywordResult> Set(
            [CliArg("name", "Exact global keyword name.", Required = true)] string name,
            [CliArg("enabled", "Requested enabled state.")] bool enabled = true,
            [CliArg("confirm", "Apply the requested global state.")] bool confirm = false,
            [CliArg("dryRun", "Preview without mutation; wins over confirmation.")] bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<GlobalKeywordResult>.Failure(Schema, compatibility.Error);
            if (string.IsNullOrWhiteSpace(name) || HasControl(name))
                return Failure("KEYWORD_NAME_INVALID", "A non-whitespace keyword name without control characters is required.", name);

            var before = Shader.IsKeywordEnabled(name);
            if (!dryRun && !confirm)
                return Failure("CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.", name);
            if (!dryRun)
            {
                if (enabled) Shader.EnableKeyword(name);
                else Shader.DisableKeyword(name);
            }
            var after = Shader.IsKeywordEnabled(name);
            if (!dryRun && after != enabled)
                return Failure("READBACK_MISMATCH", "The global keyword state did not match the request.", name);

            return CommandResult<GlobalKeywordResult>.Success(Schema, new GlobalKeywordResult
            {
                Name = name,
                Before = before,
                Requested = enabled,
                After = after,
                Applied = !dryRun,
                DryRun = dryRun,
                Undoable = false
            });
        }

        private static bool HasControl(string value)
        {
            foreach (var character in value)
                if (char.IsControl(character)) return true;
            return false;
        }

        private static CommandResult<GlobalKeywordResult> Failure(string code, string message, string name) =>
            CommandResult<GlobalKeywordResult>.Failure(Schema, code, message,
                new Dictionary<string, object> { ["name"] = name });
    }

    [Serializable]
    public sealed class GlobalKeywordResult
    {
        public string Name { get; set; }
        public bool Before { get; set; }
        public bool Requested { get; set; }
        public bool After { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public bool Undoable { get; set; }
    }
}
