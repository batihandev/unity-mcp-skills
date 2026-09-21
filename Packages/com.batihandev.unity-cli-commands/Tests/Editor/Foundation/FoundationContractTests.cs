using System;
using System.Collections.Generic;
using System.IO;
using BatihanDev.UnityCliCommands.Foundation;
using NUnit.Framework;

namespace BatihanDev.UnityCliCommands.Tests.Foundation
{
    public sealed class FoundationContractTests
    {
        [Test]
        public void CommandResultPreservesNestedDataAndVersionedTypedError()
        {
            var success = CommandResult<ResultFixture>.Success(
                "unity.foundation.fixture@1",
                new ResultFixture { Label = "quote \" and slash \\", Values = new List<int> { 2, 3, 5 } });

            Assert.That(success.Schema, Is.EqualTo("unity.foundation.fixture@1"));
            Assert.That(success.Ok, Is.True);
            Assert.That(success.Result.Values, Is.EqualTo(new[] { 2, 3, 5 }));
            Assert.That(success.Error, Is.Null);

            var failure = CommandResult<ResultFixture>.Failure(
                "unity.foundation.fixture@1",
                "PATH_TRAVERSAL",
                "Path escapes Assets.",
                new Dictionary<string, object>
                {
                    ["path"] = "Assets/../outside",
                    ["candidates"] = new[] { "Captures/a.raw", "Captures/b.data" }
                });

            Assert.That(failure.Ok, Is.False);
            Assert.That(failure.Result, Is.Null);
            Assert.That(failure.Error.Schema, Is.EqualTo("unity.command.error@1"));
            Assert.That(failure.Error.Code, Is.EqualTo("PATH_TRAVERSAL"));
            Assert.That(failure.Error.Message, Is.EqualTo("Path escapes Assets."));
            Assert.That(failure.Error.Details["path"], Is.EqualTo("Assets/../outside"));
            Assert.That(failure.Error.Details["candidates"], Is.EqualTo(new[]
            {
                "Captures/a.raw", "Captures/b.data"
            }));
        }

        [Test]
        public void CompatibilityAcceptsOnlyTheVerifiedEditorAndPackageTuple()
        {
            var accepted = CompatibilityPolicy.Evaluate(
                "6000.6.2f1",
                "0.7.0-exp.1",
                "1.20.0");

            Assert.That(accepted.Ok, Is.True);
            Assert.That(accepted.Error, Is.Null);

            var unsupportedEditor = CompatibilityPolicy.Evaluate(
                "6000.6.3f1",
                "0.7.0-exp.1",
                "1.20.0");
            Assert.That(unsupportedEditor.Ok, Is.False);
            Assert.That(unsupportedEditor.Error.Code, Is.EqualTo("UNSUPPORTED_UNITY_VERSION"));

            var missingPackage = CompatibilityPolicy.Evaluate(
                "6000.6.2f1",
                "0.7.0-exp.1",
                null);
            Assert.That(missingPackage.Ok, Is.False);
            Assert.That(missingPackage.Error.Code, Is.EqualTo("REQUIRED_PACKAGE_MISSING"));

            var mismatchedPipeline = CompatibilityPolicy.Evaluate(
                "6000.6.2f1",
                "0.6.0-exp.1",
                "1.20.0");
            Assert.That(mismatchedPipeline.Ok, Is.False);
            Assert.That(mismatchedPipeline.Error.Code, Is.EqualTo("REQUIRED_PACKAGE_MISSING"));

            var mismatchedInput = CompatibilityPolicy.Evaluate(
                "6000.6.2f1",
                "0.7.0-exp.1",
                "1.19.0");
            Assert.That(mismatchedInput.Ok, Is.False);
            Assert.That(mismatchedInput.Error.Code, Is.EqualTo("REQUIRED_PACKAGE_MISSING"));
        }

        [TestCase("Assets", "PATH_ROOT_FORBIDDEN")]
        [TestCase("Assets/../Outside.txt", "PATH_TRAVERSAL")]
        [TestCase("Assets/Foo/../../Outside.txt", "PATH_TRAVERSAL")]
        [TestCase("../Assets/Outside.txt", "PATH_TRAVERSAL")]
        public void ProjectPathPolicyRejectsRootAndTraversalWithoutTouchingSentinels(
            string target,
            string expectedCode)
        {
            using (var fixture = new PathFixture())
            {
                var result = ValidatePath(target, fixture.ProjectRoot);

                Assert.That(result.Ok, Is.False);
                Assert.That(result.Error.Code, Is.EqualTo(expectedCode));
                Assert.That(File.ReadAllText(fixture.InsideSentinel), Is.EqualTo("inside"));
                Assert.That(File.ReadAllText(fixture.OutsideSentinel), Is.EqualTo("outside"));
            }
        }

        [Test]
        public void ProjectPathPolicyNormalizesValidNonexistentTargetUsingNearestExistingParent()
        {
            using (var fixture = new PathFixture())
            {
                var result = ValidatePath("Assets\\Existing\\Future\\Target.asset", fixture.ProjectRoot);

                Assert.That(result.Ok, Is.True);
                Assert.That(result.Result.Path, Is.EqualTo("Assets/Existing/Future/Target.asset"));
            }
        }

        [Test]
        public void ProjectPathPolicyKeepsPackagesDisabledUnlessEmbeddedAuthoringIsExplicitlyEnabled()
        {
            using (var fixture = new PathFixture())
            {
                var packageRoot = Path.Combine(fixture.ProjectRoot, "Packages", "com.example.embedded");
                Directory.CreateDirectory(packageRoot);
                File.WriteAllText(Path.Combine(packageRoot, "package.json"), "{}");

                var defaultResult = ProjectPathPolicy.Validate(
                    "Packages/com.example.embedded/Tests/NewTest.cs", fixture.ProjectRoot);
                var embeddedResult = ProjectPathPolicy.Validate(
                    "Packages/com.example.embedded/Tests/NewTest.cs", fixture.ProjectRoot, true);

                Assert.That(defaultResult.Error.Code, Is.EqualTo("PATH_OUTSIDE_ROOT"));
                Assert.That(embeddedResult.Ok, Is.True);
                Assert.That(embeddedResult.Result.Path,
                    Is.EqualTo("Packages/com.example.embedded/Tests/NewTest.cs"));
            }
        }

        [TestCase("Packages", "PATH_ROOT_FORBIDDEN")]
        [TestCase("Packages/com.example.embedded", "PATH_ROOT_FORBIDDEN")]
        [TestCase("Packages/com.example.embedded/package.json", "PATH_ROOT_FORBIDDEN")]
        [TestCase("Packages/com.example.missing/Tests/Test.cs", "PACKAGE_NOT_EMBEDDED")]
        public void EmbeddedPackageAuthoringRejectsRootsAndMissingPackages(string path, string code)
        {
            using (var fixture = new PathFixture())
            {
                var packageRoot = Path.Combine(fixture.ProjectRoot, "Packages", "com.example.embedded");
                Directory.CreateDirectory(packageRoot);
                File.WriteAllText(Path.Combine(packageRoot, "package.json"), "{}");

                var result = ProjectPathPolicy.Validate(path, fixture.ProjectRoot, true);

                Assert.That(result.Ok, Is.False);
                Assert.That(result.Error.Code, Is.EqualTo(code));
            }
        }

        [Test]
        public void EmbeddedPackageAuthoringRejectsReadOnlyManifest()
        {
            using (var fixture = new PathFixture())
            {
                var packageRoot = Path.Combine(fixture.ProjectRoot, "Packages", "com.example.embedded");
                var manifest = Path.Combine(packageRoot, "package.json");
                Directory.CreateDirectory(packageRoot);
                File.WriteAllText(manifest, "{}");
                File.SetAttributes(manifest, File.GetAttributes(manifest) | FileAttributes.ReadOnly);
                try
                {
                    var result = ProjectPathPolicy.Validate(
                        "Packages/com.example.embedded/Tests/NewTest.cs", fixture.ProjectRoot, true);

                    Assert.That(result.Ok, Is.False);
                    Assert.That(result.Error.Code, Is.EqualTo("PATH_NOT_WRITABLE"));
                }
                finally
                {
                    File.SetAttributes(manifest, File.GetAttributes(manifest) & ~FileAttributes.ReadOnly);
                }
            }
        }

        [Test]
        public void ProjectPathPolicyRejectsAbsoluteOutsideTarget()
        {
            using (var fixture = new PathFixture())
            {
                var result = ValidatePath(fixture.OutsideSentinel, fixture.ProjectRoot);

                Assert.That(result.Ok, Is.False);
                Assert.That(result.Error.Code, Is.EqualTo("PATH_OUTSIDE_ROOT"));
            }
        }

        [Test]
        public void ProjectPathPolicyReturnsTypedFailureForMalformedAuthoringPath()
        {
            using (var fixture = new PathFixture())
            {
                CommandResult<ProjectPathResult> result = null;

                Assert.DoesNotThrow(() => result = ValidatePath("Assets/invalid\0.asset", fixture.ProjectRoot));
                Assert.That(result.Error.Code, Is.EqualTo("PATH_INVALID"));
                Assert.That(result.Error.Details["path"], Is.EqualTo("<invalid-path>"));
            }
        }

        [Test]
        public void ProjectFilePolicyConfinesRegularCaptureFilesWithoutRequiringAssets()
        {
            var root = Path.Combine(Path.GetTempPath(), "UnityCliCapturePath-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "Captures"));
            File.WriteAllText(Path.Combine(root, "Captures", "valid.raw"), "capture");
            try
            {
                var valid = ProjectPathPolicy.ValidateProjectFile(
                    "Captures/valid.raw", root, new[] { ".raw", ".data" });
                Assert.That(valid.Ok, Is.True);
                Assert.That(valid.Result.Path, Is.EqualTo("Captures/valid.raw"));

                Assert.That(ProjectPathPolicy.ValidateProjectFile(
                    "Captures", root, new[] { ".raw", ".data" }).Error.Code, Is.EqualTo("NOT_REGULAR_FILE"));
                Assert.That(ProjectPathPolicy.ValidateProjectFile(
                    "Captures/missing.raw", root, new[] { ".raw", ".data" }).Error.Code, Is.EqualTo("FILE_NOT_FOUND"));
                Assert.That(ProjectPathPolicy.ValidateProjectFile(
                    "Captures/valid.txt", root, new[] { ".raw", ".data" }).Error.Code,
                    Is.EqualTo("CAPTURE_EXTENSION_UNSUPPORTED"));
                Assert.That(ProjectPathPolicy.ValidateProjectFile(
                    "Captures/../valid.raw", root, new[] { ".raw", ".data" }).Error.Code,
                    Is.EqualTo("PATH_TRAVERSAL"));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void ProjectFilePolicyReturnsTypedSanitizedFailuresForRootedInvalidAndUnreadablePaths()
        {
            using (var fixture = new PathFixture())
            {
                var rooted = ProjectPathPolicy.ValidateProjectFile(
                    fixture.OutsideSentinel, fixture.ProjectRoot, new[] { ".raw", ".data" });
                Assert.That(rooted.Error.Code, Is.EqualTo("PATH_OUTSIDE_ROOT"));
                Assert.That(rooted.Error.Details["path"], Is.EqualTo("<rooted-path>"));

                CommandResult<ProjectPathResult> invalid = null;
                Assert.DoesNotThrow(() => invalid = ProjectPathPolicy.ValidateProjectFile(
                    "Captures/invalid\0.raw", fixture.ProjectRoot, new[] { ".raw", ".data" }));
                Assert.That(invalid.Error.Code, Is.EqualTo("PATH_INVALID"));
                Assert.That(invalid.Error.Details["path"], Is.EqualTo("<invalid-path>"));

                var capture = Path.Combine(fixture.ProjectRoot, "capture.raw");
                File.WriteAllText(capture, "capture");
                var denied = ProjectPathPolicy.ValidateProjectFile(
                    "capture.raw", fixture.ProjectRoot, new[] { ".raw", ".data" },
                    _ => throw new UnauthorizedAccessException());
                Assert.That(denied.Error.Code, Is.EqualTo("PATH_ACCESS_DENIED"));
                Assert.That(denied.Error.Message, Does.Not.Contain(fixture.ProjectRoot));
            }
        }

        [Test]
        public void ProjectFilePolicyRejectsCaptureJunctions()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Ignore("Windows junction semantics are verified on the claimed Windows Editor line.");

            using (var fixture = new PathFixture())
            {
                File.WriteAllText(Path.Combine(fixture.OutsideRoot, "outside.raw"), "capture");
                fixture.CreateJunction("Captures", fixture.OutsideRoot);

                var result = ProjectPathPolicy.ValidateProjectFile(
                    "Captures/outside.raw", fixture.ProjectRoot, new[] { ".raw", ".data" });

                Assert.That(result.Error.Code, Is.EqualTo("PATH_REPARSE_POINT"));
            }
        }

        [Test]
        public void ProjectPathPolicyRejectsIntermediateAndFinalReparsePoints()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Ignore("Windows junction semantics are verified on the claimed Windows Editor line.");

            using (var fixture = new PathFixture())
            {
                fixture.CreateJunction("Assets/Linked", fixture.OutsideRoot);
                var intermediate = ValidatePath("Assets/Linked/Future.asset", fixture.ProjectRoot);
                Assert.That(intermediate.Error.Code, Is.EqualTo("PATH_REPARSE_POINT"));

                fixture.CreateJunction("Assets/FinalLink", fixture.OutsideRoot);
                var final = ValidatePath("Assets/FinalLink", fixture.ProjectRoot);
                Assert.That(final.Error.Code, Is.EqualTo("PATH_REPARSE_POINT"));
                Assert.That(File.ReadAllText(fixture.OutsideSentinel), Is.EqualTo("outside"));
            }
        }

        [Test]
        public void ProjectPathPolicyRejectsProjectRootAndAncestorReparsePoints()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Ignore("Windows junction semantics are verified on the claimed Windows Editor line.");

            var root = Path.Combine(Path.GetTempPath(), "UnityCliFoundation-" + Guid.NewGuid().ToString("N"));
            var realProject = Path.Combine(root, "RealProject");
            var linkedProject = Path.Combine(root, "LinkedProject");
            var realAncestor = Path.Combine(root, "RealAncestor");
            var nestedProject = Path.Combine(realAncestor, "Project");
            var linkedAncestor = Path.Combine(root, "LinkedAncestor");
            Directory.CreateDirectory(Path.Combine(realProject, "Assets", "Existing"));
            Directory.CreateDirectory(Path.Combine(nestedProject, "Assets", "Existing"));

            try
            {
                CreateJunction(linkedProject, realProject);
                var projectRoot = ValidatePath("Assets/Existing/Future.asset", linkedProject);
                Assert.That(projectRoot.Error.Code, Is.EqualTo("PATH_REPARSE_POINT"));

                CreateJunction(linkedAncestor, realAncestor);
                var ancestor = ValidatePath(
                    "Assets/Existing/Future.asset",
                    Path.Combine(linkedAncestor, "Project"));
                Assert.That(ancestor.Error.Code, Is.EqualTo("PATH_REPARSE_POINT"));
            }
            finally
            {
                if (Directory.Exists(linkedProject))
                    Directory.Delete(linkedProject);
                if (Directory.Exists(linkedAncestor))
                    Directory.Delete(linkedAncestor);
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        private static CommandResult<ProjectPathResult> ValidatePath(string target, string projectRoot)
        {
            return ProjectPathPolicy.Validate(target, projectRoot);
        }

        private static void CreateJunction(string link, string target)
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/d /c mklink /J \"{link}\" \"{target}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process.WaitForExit();
            Assert.That(process.ExitCode, Is.Zero, "Expected Windows junction fixture creation to succeed.");
        }

        [Serializable]
        private sealed class ResultFixture
        {
            public string Label { get; set; }
            public List<int> Values { get; set; }
        }

        private sealed class PathFixture : IDisposable
        {
            public PathFixture()
            {
                Root = Path.Combine(Path.GetTempPath(), "UnityCliFoundation-" + Guid.NewGuid().ToString("N"));
                ProjectRoot = Path.Combine(Root, "Project");
                OutsideRoot = Path.Combine(Root, "Outside");
                Directory.CreateDirectory(Path.Combine(ProjectRoot, "Assets", "Existing"));
                Directory.CreateDirectory(OutsideRoot);
                InsideSentinel = Path.Combine(ProjectRoot, "Assets", "inside.txt");
                OutsideSentinel = Path.Combine(OutsideRoot, "outside.txt");
                File.WriteAllText(InsideSentinel, "inside");
                File.WriteAllText(OutsideSentinel, "outside");
            }

            public string Root { get; }
            public string ProjectRoot { get; }
            public string OutsideRoot { get; }
            public string InsideSentinel { get; }
            public string OutsideSentinel { get; }

            public void CreateJunction(string projectRelativePath, string target)
            {
                var link = Path.Combine(ProjectRoot, projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
                FoundationContractTests.CreateJunction(link, target);
            }

            public void Dispose()
            {
                foreach (var link in new[] { "Assets/Linked", "Assets/FinalLink", "Captures" })
                {
                    var fullPath = Path.Combine(ProjectRoot, link.Replace('/', Path.DirectorySeparatorChar));
                    if (Directory.Exists(fullPath))
                        Directory.Delete(fullPath);
                }

                if (Directory.Exists(Root))
                    Directory.Delete(Root, true);
            }
        }
    }
}
