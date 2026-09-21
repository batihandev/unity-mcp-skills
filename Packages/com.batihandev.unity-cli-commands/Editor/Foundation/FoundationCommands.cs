using Unity.Pipeline.Commands;

namespace BatihanDev.UnityCliCommands.Foundation
{
    public static class FoundationCommands
    {
        [CliCommand(
            "unity_cli_commands_smoke",
            "Return exact package and compatibility state.",
            Tags = new[] { "unity-cli-commands", "foundation" })]
        public static CommandResult<CompatibilitySnapshot> Smoke()
        {
            return CompatibilityPolicy.CheckInstalled();
        }

        [CliCommand(
            "foundation.path.validate",
            "Validate one project-relative authoring path without mutating Unity.",
            Tags = new[] { "unity-cli-commands", "foundation" })]
        public static CommandResult<ProjectPathResult> ValidatePath(
            [CliArg("path", "Project-relative Assets or explicitly enabled embedded-package path.", Required = true)] string path,
            [CliArg("allowEmbeddedPackages", "Allow an existing project-contained embedded package whose root and manifest are not marked read-only or linked.")]
            bool allowEmbeddedPackages = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<ProjectPathResult>.Failure(ProjectPathPolicy.Schema, compatibility.Error);
            return ProjectPathPolicy.Validate(path, allowEmbeddedPackages);
        }
    }
}
