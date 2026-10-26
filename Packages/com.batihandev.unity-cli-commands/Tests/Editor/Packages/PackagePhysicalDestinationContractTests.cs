using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BatihanDev.UnityCliCommands.Foundation;
using BatihanDev.UnityCliCommands.Packages;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class PackagePhysicalDestinationContractTests
    {
        private const string FixtureName = "com.example.physical-destination";

        [Test]
        public void RegisteredRegistrySourceIsNotAnExistingPhysicalEmbedDestination()
        {
            var root = EditorRoot();
            var package = RegistryWithoutPhysicalDestination(root);
            var result = ValidateRoot(package.name, root);
            Assert.That(result.Ok, Is.True, result.Error?.Code + ": " + result.Error?.Message);
            Assert.That(result.Result.Path, Is.EqualTo("Packages/" + package.name));
            Assert.That(result.Result.Exists, Is.False,
                "A registered registry source exposed by Unity VFS is not a physical embedded destination.");
        }

        [Test]
        public void RegisteredRegistrySourceHasEmptyEmbeddedDestinationInventory()
        {
            var root = EditorRoot();
            var package = RegistryWithoutPhysicalDestination(root);
            JObject inventory = null;
            Assert.DoesNotThrow(() => inventory = EmbeddedInventory(root, package.name),
                "An absent physical embedded root must not enumerate the registry cache through Unity VFS.");
            Assert.That(inventory["files"].Count(), Is.Zero);
            Assert.That(inventory["directories"].Count(), Is.Zero);
        }

        [Test]
        public void RegistryEmbedDryRunPlansAbsentPhysicalDestinationWithoutPublishingJournal()
        {
            var root = EditorRoot();
            var package = RegistryWithoutPhysicalDestination(root);
            var manifest = File.ReadAllBytes(Path.Combine(root, "Packages", "manifest.json"));
            var packageLock = File.ReadAllBytes(Path.Combine(root, "Packages", "packages-lock.json"));
            var sourceManifest = File.ReadAllBytes(Path.Combine(package.resolvedPath, "package.json"));
            var operation = Guid.NewGuid().ToString("N");
            var result = PackageAuthoringCommands.Embed(package.name, package.version, operation, dryRun: true);
            Assert.That(File.ReadAllBytes(Path.Combine(root, "Packages", "manifest.json")), Is.EqualTo(manifest));
            Assert.That(File.ReadAllBytes(Path.Combine(root, "Packages", "packages-lock.json")), Is.EqualTo(packageLock));
            Assert.That(File.ReadAllBytes(Path.Combine(package.resolvedPath, "package.json")), Is.EqualTo(sourceManifest));
            var files = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageOperationFiles");
            var journal = (string)files.GetMethod("JournalPath", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { root, operation });
            Assert.That(File.Exists(journal), Is.False, "A dry run must not publish an operation journal.");
            Assert.That(result.Ok, Is.True, result.Error?.Code + ": " + result.Error?.Message);
            Assert.That((bool)result.Result["dryRun"], Is.True);
            Assert.That((bool)result.Result["applied"], Is.False);
            Assert.That((bool)result.Result["acknowledged"], Is.False);
            Assert.That(result.Result["destinationBefore"]["files"].Count(), Is.Zero);
            Assert.That(result.Result["destinationBefore"]["directories"].Count(), Is.Zero);
            Assert.That((string)result.Result["originalPackage"]["source"], Is.EqualTo("Registry"));
            Assert.That((string)result.Result["originalPackage"]["resolvedPath"], Is.EqualTo(package.resolvedPath));
            Assert.That((string)result.Result["planSha256"], Does.Match("^[a-f0-9]{64}$"));
        }

        [TestCase("absent", false)]
        [TestCase("file", true)]
        [TestCase("directory", true)]
        public void PhysicalDestinationMembershipIncludesFilesAndDirectories(string kind, bool exists)
        {
            var root = TemporaryRoot();
            try
            {
                Directory.CreateDirectory(Path.Combine(root, "Packages"));
                var target = Path.Combine(root, "Packages", FixtureName);
                if (kind == "file") File.WriteAllText(target, "physical file");
                if (kind == "directory") Directory.CreateDirectory(target);
                var result = ValidateRoot(FixtureName, root);
                Assert.That(result.Ok, Is.True, result.Error?.Code + ": " + result.Error?.Message);
                Assert.That(result.Result.Exists, Is.EqualTo(exists));
                if (kind == "file") Assert.That(File.ReadAllText(target), Is.EqualTo("physical file"));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [TestCase("../outside")]
        [TestCase("com.example/path")]
        public void InvalidPackageNamesCannotSelectAParentEntry(string name)
        {
            var result = ValidateRoot(name, Path.GetTempPath());
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("PATH_INVALID"));
        }

        [Test]
        public void InvalidPhysicalParentPathReturnsTypedFailure()
        {
            var result = ValidateRoot(FixtureName, "invalid\0root");
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("PATH_INVALID"));
            Assert.That(result.Error.Message, Does.Not.Contain("root"));
        }

        [TestCase("project-root")]
        [TestCase("packages-parent")]
        [TestCase("destination")]
        [TestCase("dangling-destination")]
        public void LinkedPhysicalDestinationOrAncestorIsRefused(string kind)
        {
            var container = TemporaryRoot();
            var root = Path.Combine(container, "Project");
            var outside = Path.Combine(container, "Outside");
            string link = null;
            try
            {
                Directory.CreateDirectory(root);
                Directory.CreateDirectory(outside);
                var sentinel = Path.Combine(outside, "keep.txt");
                File.WriteAllText(sentinel, "outside unchanged");
                if (kind == "project-root")
                {
                    Directory.CreateDirectory(Path.Combine(outside, "Packages"));
                    link = Path.Combine(container, "LinkedProject");
                    CreateDirectoryLink(link, outside);
                    root = link;
                }
                else if (kind == "packages-parent")
                {
                    link = Path.Combine(root, "Packages");
                    CreateDirectoryLink(link, outside);
                }
                else
                {
                    Directory.CreateDirectory(Path.Combine(root, "Packages"));
                    link = Path.Combine(root, "Packages", FixtureName);
                    CreateDirectoryLink(link, kind == "dangling-destination" ? Path.Combine(container, "Missing") : outside);
                }
                var result = ValidateRoot(FixtureName, root);
                Assert.That(result.Ok, Is.False);
                Assert.That(result.Error.Code, Is.EqualTo("PATH_REPARSE_POINT"));
                Assert.That(File.ReadAllText(sentinel), Is.EqualTo("outside unchanged"));
            }
            finally
            {
                if (link != null && (File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0)
                    Directory.Delete(link, false);
                if (Directory.Exists(container)) Directory.Delete(container, true);
            }
        }

        private static string EditorRoot() => Directory.GetParent(UnityEngine.Application.dataPath).FullName;
        private static string TemporaryRoot() => Path.Combine(Path.GetTempPath(), "unity-physical-destination-" + Guid.NewGuid().ToString("N"));

        private static UnityEditor.PackageManager.PackageInfo RegistryWithoutPhysicalDestination(string root)
        {
            var physicalNames = Directory.GetFileSystemEntries(Path.Combine(root, "Packages"))
                .Select(Path.GetFileName).ToArray();
            var comparison = UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WindowsEditor
                ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var package = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .Where(item => item.source == PackageSource.Registry && !physicalNames.Contains(item.name, comparison))
                .OrderBy(item => item.name, StringComparer.Ordinal).FirstOrDefault();
            Assert.That(package, Is.Not.Null, "The native fixture must contain a registry package without a physical embedded child.");
            Assert.That(Directory.Exists(package.resolvedPath), Is.True, "The selected registry source must exist.");
            return package;
        }

        private static CommandResult<ProjectPathResult> ValidateRoot(string name, string root)
        {
            var method = typeof(ProjectPathPolicy).GetMethod("ValidatePackageRoot", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (CommandResult<ProjectPathResult>)method.Invoke(null, new object[] { name, root });
        }

        private static JObject EmbeddedInventory(string root, string name)
        {
            var method = typeof(PackageAuthoringCommands).GetMethod("EmbeddedInventory", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            try { return (JObject)method.Invoke(null, new object[] { root, name }); }
            catch (TargetInvocationException exception) { throw exception.InnerException; }
        }

        private static void CreateDirectoryLink(string linked, string target)
        {
            var windows = UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WindowsEditor;
            var arguments = windows ? "/d /c mklink /D \"" + linked + "\" \"" + target + "\""
                : "-s \"" + target + "\" \"" + linked + "\"";
            var start = new System.Diagnostics.ProcessStartInfo(windows ? "cmd.exe" : "ln", arguments)
            { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            using (var process = System.Diagnostics.Process.Start(start))
            {
                Assert.That(process, Is.Not.Null);
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(10000)) { process.Kill(); Assert.Fail("Link fixture exceeded its deadline."); }
                Assert.That(System.Threading.Tasks.Task.WaitAll(new System.Threading.Tasks.Task[] { output, error }, 10000), Is.True);
                Assert.That(process.ExitCode, Is.Zero, output.Result + " " + error.Result);
            }
            Assert.That(File.GetAttributes(linked) & FileAttributes.ReparsePoint, Is.EqualTo(FileAttributes.ReparsePoint));
        }
    }
}
