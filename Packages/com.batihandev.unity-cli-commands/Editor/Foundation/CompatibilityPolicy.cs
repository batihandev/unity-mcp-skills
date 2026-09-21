using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BatihanDev.UnityCliCommands.Foundation
{
    public static class CompatibilityPolicy
    {
        public const string UnityVersion = "6000.6.2f1";
        public const string PipelineVersion = "0.7.0-exp.1";
        public const string InputSystemVersion = "1.20.0";
        public const string PackageId = "com.batihandev.unity-cli-commands";
        public const string PackageVersion = "0.1.0";
        public const string Schema = "unity.foundation.compatibility@1";

        public static CommandResult<CompatibilitySnapshot> CheckInstalled()
        {
            return Evaluate(
                Application.unityVersion,
                FindVersion("com.unity.pipeline"),
                PackageInfo.FindForAssembly(typeof(InputSystem).Assembly)?.version);
        }

        internal static CommandResult<CompatibilitySnapshot> Evaluate(
            string unityVersion,
            string pipelineVersion,
            string inputSystemVersion)
        {
            if (!string.Equals(unityVersion, UnityVersion, StringComparison.Ordinal))
            {
                return CommandResult<CompatibilitySnapshot>.Failure(
                    Schema,
                    FoundationErrorCode.UnsupportedUnityVersion,
                    $"Unity Editor {UnityVersion} is required; found {unityVersion ?? "unknown"}.",
                    Versions(unityVersion, pipelineVersion, inputSystemVersion));
            }

            if (!string.Equals(pipelineVersion, PipelineVersion, StringComparison.Ordinal) ||
                !string.Equals(inputSystemVersion, InputSystemVersion, StringComparison.Ordinal))
            {
                return CommandResult<CompatibilitySnapshot>.Failure(
                    Schema,
                    FoundationErrorCode.RequiredPackageMissing,
                    $"Required packages are com.unity.pipeline@{PipelineVersion} and com.unity.inputsystem@{InputSystemVersion}.",
                    Versions(unityVersion, pipelineVersion, inputSystemVersion));
            }

            return CommandResult<CompatibilitySnapshot>.Success(
                Schema,
                new CompatibilitySnapshot
                {
                    PackageId = PackageId,
                    PackageVersion = PackageVersion,
                    Status = "ready",
                    UnityVersion = unityVersion,
                    PipelineVersion = pipelineVersion,
                    InputSystemVersion = inputSystemVersion
                });
        }

        private static string FindVersion(string packageName)
        {
            return PackageInfo.FindForPackageName(packageName)?.version;
        }

        private static Dictionary<string, object> Versions(
            string unityVersion,
            string pipelineVersion,
            string inputSystemVersion)
        {
            return new Dictionary<string, object>
            {
                ["unityVersion"] = unityVersion ?? "missing",
                ["pipelineVersion"] = pipelineVersion ?? "missing",
                ["inputSystemVersion"] = inputSystemVersion ?? "missing"
            };
        }
    }

    [Serializable]
    public sealed class CompatibilitySnapshot
    {
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }
        public string Status { get; set; }
        public string UnityVersion { get; set; }
        public string PipelineVersion { get; set; }
        public string InputSystemVersion { get; set; }
    }
}
