using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class PackageRegisteredDestinationLinkContractTests
    {
        [Test]
        public void RegisteredRegistryDestinationPhysicalLinkIsRefusedWithoutChangingPackageState()
        {
            var root = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            var select = typeof(PackagePhysicalDestinationContractTests).GetMethod(
                "RegistryWithoutPhysicalDestination", BindingFlags.Static | BindingFlags.NonPublic);
            var package = (UnityEditor.PackageManager.PackageInfo)select.Invoke(null, new object[] { root });
            var scratch = Path.Combine(root, "Temp", "unity-registered-link-" + Guid.NewGuid().ToString("N"));
            var link = Path.Combine(root, "Packages", package.name);
            var sourcePath = Path.Combine(package.resolvedPath, "package.json");
            var manifestPath = Path.Combine(root, "Packages", "manifest.json");
            var lockPath = Path.Combine(root, "Packages", "packages-lock.json");
            var sourceBefore = File.ReadAllBytes(sourcePath);
            var manifestBefore = File.ReadAllBytes(manifestPath);
            var lockBefore = File.ReadAllBytes(lockPath);
            var linkCreated = false;
            Directory.CreateDirectory(scratch);
            var sentinel = Path.Combine(scratch, "keep.txt");
            File.WriteAllText(sentinel, "registered link scratch sentinel");
            try
            {
                LiteralLink(link, scratch, false);
                linkCreated = true;
                var physical = new DirectoryInfo(Path.Combine(root, "Packages")).GetFileSystemInfos()
                    .Single(entry => string.Equals(entry.Name, package.name, StringComparison.OrdinalIgnoreCase));
                var literalAttributes = physical.Attributes;
                var stringAttributes = File.GetAttributes(link);
                var measurement = new JObject
                {
                    ["name"] = package.name,
                    ["source"] = package.source.ToString(),
                    ["physicalEntryAttributes"] = literalAttributes.ToString(),
                    ["stringPathAttributes"] = stringAttributes.ToString(),
                    ["logicalFullPath"] = Path.GetFullPath(link),
                    ["resolvedSource"] = package.resolvedPath,
                    ["sourceManifestBeforeSha256"] = Hash(sourceBefore),
                    ["projectManifestBeforeSha256"] = Hash(manifestBefore),
                    ["projectLockBeforeSha256"] = Hash(lockBefore)
                };
                TestContext.WriteLine(measurement.ToString());
                Assert.That(literalAttributes & FileAttributes.ReparsePoint, Is.EqualTo(FileAttributes.ReparsePoint),
                    "Physical enumeration must observe the actual registered-name link.");
                var method = typeof(ProjectPathPolicy).GetMethod("ValidatePackageRoot", BindingFlags.Static | BindingFlags.NonPublic);
                var result = (CommandResult<ProjectPathResult>)method.Invoke(null, new object[] { package.name, root });
                Assert.That(result.Ok, Is.False,
                    "A registered registry name cannot hide a physical destination link. " + measurement.ToString());
                Assert.That(result.Error.Code, Is.EqualTo("PATH_REPARSE_POINT"));
            }
            finally
            {
                if (linkCreated) LiteralLink(link, scratch, true);
                Assert.That(Directory.GetFileSystemEntries(Path.Combine(root, "Packages"))
                    .Any(entry => string.Equals(Path.GetFileName(entry), package.name, StringComparison.OrdinalIgnoreCase)), Is.False,
                    "Literal registered-name link cleanup must restore absent physical destination membership.");
                Assert.That(File.ReadAllBytes(sourcePath), Is.EqualTo(sourceBefore), "Registry source manifest changed.");
                Assert.That(File.ReadAllBytes(manifestPath), Is.EqualTo(manifestBefore), "Project manifest changed.");
                Assert.That(File.ReadAllBytes(lockPath), Is.EqualTo(lockBefore), "Project lock changed.");
                TestContext.WriteLine(new JObject
                {
                    ["sourceManifestAfterSha256"] = Hash(File.ReadAllBytes(sourcePath)),
                    ["projectManifestAfterSha256"] = Hash(File.ReadAllBytes(manifestPath)),
                    ["projectLockAfterSha256"] = Hash(File.ReadAllBytes(lockPath)),
                    ["physicalDestinationRestoredAbsent"] = true
                }.ToString());
                Assert.That(File.ReadAllText(sentinel), Is.EqualTo("registered link scratch sentinel"));
                Directory.Delete(scratch, true);
            }
        }

        private static string Hash(byte[] bytes)
        {
            using (var algorithm = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        private static string ProcessArgument(string value)
        {
            var quoted = new System.Text.StringBuilder("\"");
            var slashes = 0;
            foreach (var character in value)
            {
                if (character == '\\') { slashes++; continue; }
                quoted.Append('\\', slashes * (character == '"' ? 2 : 1));
                slashes = 0;
                if (character == '"') quoted.Append('\\');
                quoted.Append(character);
            }
            quoted.Append('\\', slashes * 2);
            return quoted.Append('"').ToString();
        }

        private static void LiteralLink(string link, string target, bool remove)
        {
            System.Diagnostics.ProcessStartInfo start;
            if (UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WindowsEditor)
            {
                Assert.That(link.IndexOfAny(new[] { '"', '%', '\r', '\n' }), Is.LessThan(0));
                Assert.That(target.IndexOfAny(new[] { '"', '%', '\r', '\n' }), Is.LessThan(0));
                var command = remove ? "rmdir \"" + link + "\"" : "mklink /D \"" + link + "\" \"" + target + "\"";
                start = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/d /v:off /s /c \"" + command + "\"");
            }
            else
            {
                start = remove
                    ? new System.Diagnostics.ProcessStartInfo("/usr/bin/unlink", ProcessArgument(link))
                    : new System.Diagnostics.ProcessStartInfo("/bin/ln", "-s " + ProcessArgument(target) + " " + ProcessArgument(link));
            }
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.CreateNoWindow = true;
            using (var process = System.Diagnostics.Process.Start(start))
            {
                Assert.That(process, Is.Not.Null);
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(10000)) { process.Kill(); Assert.Fail("Literal link command exceeded its deadline."); }
                Assert.That(System.Threading.Tasks.Task.WaitAll(new System.Threading.Tasks.Task[] { output, error }, 10000), Is.True);
                Assert.That(process.ExitCode, Is.Zero, output.Result + " " + error.Result);
            }
        }
    }
}
