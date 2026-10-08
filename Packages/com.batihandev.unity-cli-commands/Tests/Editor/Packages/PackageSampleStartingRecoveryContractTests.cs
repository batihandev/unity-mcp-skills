using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BatihanDev.UnityCliCommands.Foundation;
using BatihanDev.UnityCliCommands.Packages;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class PackageSampleStartingRecoveryContractTests
    {
        private const string Name = "com.example.fixture";
        private const string Version = "1.0.0";
        private string root;
        private string id;
        private string path;
        private JObject plan;
        private JObject journal;
        private JObject preview;
        private JObject package;
        private string selectedRoot;
        private string selectedImport;
        private string currentCommandHash;
        private Dictionary<string, string> assetsBefore;
        private Dictionary<string, string> sourceBefore;

        [SetUp] public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "unity-starting-recovery-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "Assets"));
            id = Guid.NewGuid().ToString("N");
            selectedRoot = PackageSampleContractTests.CreateArchive(root, "Assets/ArchiveFixture/hello.txt", entryGuid: Guid.NewGuid().ToString("N"));
            var packageRoot = Path.Combine(root, "Packages", Name);
            selectedImport = "Assets/Samples/Fixture/1.0.0/Archive";
            package = new JObject { ["name"] = Name, ["version"] = Version, ["source"] = "Local", ["resolvedPath"] = packageRoot,
                ["displayName"] = "Filesystem fixture", ["isDirectDependency"] = true, ["dependencies"] = new JArray() };
            assetsBefore = Snapshot(Path.Combine(root, "Assets")); sourceBefore = Snapshot(packageRoot);
            var inventory = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageSampleInventory");
            plan = (JObject)inventory.GetMethod("Prepare", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,
                new object[] { root, selectedRoot, packageRoot, selectedImport, false });
            plan.Remove("planSha256"); plan["name"] = Name; plan["version"] = Version; plan["sampleName"] = "Archive";
            currentCommandHash = (string)typeof(PackageAuthoringCommands).GetMethod("CommandHash", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            plan["originalPackage"] = package.DeepClone(); plan["commandSha256"] = currentCommandHash; RehashPlan();
            preview = (JObject)plan.DeepClone();
            using (var process = Process.GetCurrentProcess()) { preview["nativePid"] = process.Id; preview["nativeProcessStartTicks"] = process.StartTime.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture); }
            Assert.That((bool)plan["archive"], Is.True);
            Assert.That((string)plan["commandSha256"], Does.Match("^[a-f0-9]{64}$"));
            Assert.That(plan["originalPackage"], Is.TypeOf<JObject>());
            Assert.That(Snapshot(Path.Combine(root, "Assets")), Is.EquivalentTo(assetsBefore));
            Assert.That((string)plan["packageRoot"], Is.EqualTo(Path.GetFullPath(packageRoot)));
            Assert.That(Snapshot(packageRoot), Is.EquivalentTo(sourceBefore));
            AssertProcessAbsent(int.MaxValue);
            journal = new JObject {
                ["schema"] = "unity.package.operation@1", ["operationId"] = id, ["project"] = root,
                ["editorPid"] = int.MaxValue, ["nativeProcessStartTicks"] = "1",
                ["editorStartedAt"] = "UNIT-FIXTURE; no native import was started",
                ["commandSha256"] = plan["commandSha256"].DeepClone(), ["name"] = Name, ["version"] = Version,
                ["kind"] = "sample", ["state"] = "starting", ["startedUtc"] = DateTime.UtcNow.ToString("o"),
                ["acknowledged"] = false, ["observerDetached"] = true, ["requestUnresolved"] = false,
                ["sdkRequestCancelled"] = false, ["sampleName"] = "Archive", ["samplePlan"] = plan,
                ["originalPackage"] = plan["originalPackage"].DeepClone(),
                ["unitFixture"] = new JObject { ["label"] = "UNIT-FIXTURE", ["job"] = "starting-recovery-contract", ["nativeImportStarted"] = false }
            };
        }

        [TearDown] public void TearDown()
        {
            try
            {
                if (assetsBefore != null) Assert.That(Snapshot(Path.Combine(root, "Assets")), Is.EquivalentTo(assetsBefore), "Recovery changed Assets bytes or directories.");
                if (sourceBefore != null && preview != null) Assert.That(Snapshot((string)preview["packageRoot"]), Is.EquivalentTo(sourceBefore), "Recovery changed registered sample source.");

            }
            finally
            {
                if (path != null && File.Exists(path))
                {
                    var owned = JObject.Parse(File.ReadAllText(path));
                    Assert.That((string)owned["operationId"], Is.EqualTo(id), "Preserve any receipt that lost unit fixture ownership.");
                    Assert.That((string)owned["unitFixture"]?["label"], Is.EqualTo("UNIT-FIXTURE"));
                    Assert.That((string)owned["unitFixture"]?["job"], Is.EqualTo("starting-recovery-contract"));
                    File.Delete(path);
                }
                if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StartingUnacknowledgedSamplePreviewsNoMutationAfterOriginalProcessAbsent(bool explicitImportFalse)
        {
            if (explicitImportFalse) journal["importAcknowledged"] = false;
            var hash = Publish();
            var bytes = File.ReadAllBytes(path);
            var result = Recover(id, hash, dryRun: true);
            AssertPreserved(bytes);
            Assert.That(result.Ok, Is.True, result.Error?.Code + ": " + result.Error?.Message);
            Assert.That((bool?)result.Result["observedNoMutation"], Is.True);
            Assert.That((bool?)result.Result["dryRun"], Is.True);
            Assert.That((bool?)result.Result["restored"], Is.False);
            Assert.That((string)result.Result["operationId"], Is.EqualTo(id));
            Assert.That((int?)result.Result["nativePid"], Is.EqualTo((int)preview["nativePid"]));
            Assert.That((string)result.Result["nativeProcessStartTicks"], Is.EqualTo((string)preview["nativeProcessStartTicks"]));
            Assert.That(result.Result["nativeStatus"], Is.Null);
            Assert.That(result.Result["nativeResult"], Is.Null);
        }

        [Test] public void OriginalExactEditorAliveRefusesStartingRecovery()
        {
            journal["editorPid"] = preview["nativePid"].DeepClone();
            journal["nativeProcessStartTicks"] = preview["nativeProcessStartTicks"].DeepClone();
            Refuses("LIFECYCLE_BARRIER_REQUIRED");
        }

        [TestCase("missing-pid")]
        [TestCase("zero-pid")]
        [TestCase("negative-pid")]
        [TestCase("missing-ticks")]
        [TestCase("malformed-ticks")]
        [TestCase("zero-ticks")]
        public void MissingOrInvalidOriginalIdentityRefusesStartingRecovery(string fault)
        {
            if (fault == "missing-pid") journal.Remove("editorPid");
            if (fault == "zero-pid") journal["editorPid"] = 0;
            if (fault == "negative-pid") journal["editorPid"] = -1;
            if (fault == "missing-ticks") journal.Remove("nativeProcessStartTicks");
            if (fault == "malformed-ticks") journal["nativeProcessStartTicks"] = "not-ticks";
            if (fault == "zero-ticks") journal["nativeProcessStartTicks"] = "0";
            Refuses("LIFECYCLE_BARRIER_REQUIRED");
        }

        [TestCase("plan-hash", "PLAN_HASH_MISMATCH")]
        [TestCase("command-hash", "JOURNAL_IDENTITY_MISMATCH")]
        [TestCase("package-source", null)]
        [TestCase("package-resolved-root", null)]
        [TestCase("sample-root", null)]
        [TestCase("package-root", null)]
        [TestCase("sample-name", null)]
        public void ChangedRetainedPackageOrPlanBindingRefusesStartingRecovery(string fault, string code)
        {
            if (fault == "plan-hash") plan["planSha256"] = new string('0', 64);
            else if (fault == "command-hash") journal["commandSha256"] = new string('0', 64);
            else
            {
                if (fault == "package-source") plan["originalPackage"]["source"] = "UNIT-FIXTURE-invalid-source";
                if (fault == "package-resolved-root") plan["originalPackage"]["resolvedPath"] = Path.Combine(root, "Packages", "com.example.unit-fixture-unregistered");
                if (fault == "sample-root") plan["sampleRoot"] = root;
                if (fault == "package-root") plan["packageRoot"] = root;
                if (fault == "sample-name") plan["sampleName"] = "UNIT-FIXTURE-unregistered-sample";
                journal["originalPackage"] = plan["originalPackage"].DeepClone();
                RehashPlan();
            }
            Refuses(code);
        }

        [TestCase("file-hash")]
        [TestCase("directory")]
        public void ChangedRetainedSourceInventoryRefusesStartingRecovery(string fault)
        {
            if (fault == "file-hash")
            {
                var first = ((JObject)plan["sourceInventory"]["files"]).Properties().First();
                first.Value["sha256"] = new string('0', 64);
            }
            else ((JArray)plan["sourceInventory"]["directories"]).Add("UNIT-FIXTURE-absent-source-directory");
            RehashPlan();
            Refuses("SAMPLE_SOURCE_CHANGED");
        }

        [TestCase("foreign-file")]
        [TestCase("metadata")]
        [TestCase("directory")]
        [TestCase("parent-directory")]
        public void ChangedRetainedDestinationBeforeRefusesStartingRecovery(string fault)
        {
            var watchedRoot = plan["watched"].Values<string>().First(value => !value.EndsWith(".meta", StringComparison.Ordinal));
            if (fault == "foreign-file" || fault == "metadata")
            {
                var relative = fault == "metadata" ? watchedRoot + ".meta" : watchedRoot + "/UNIT-FIXTURE-absent.txt";
                ((JObject)plan["before"]["files"])[relative] = new JObject {
                    ["exists"] = true, ["sha256"] = Hash(Encoding.UTF8.GetBytes("UNIT-FIXTURE-before")),
                    ["length"] = 19, ["bytesBase64"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("UNIT-FIXTURE-before"))
                };
            }
            else if (fault == "directory") ((JArray)plan["before"]["directories"]).Add(watchedRoot + "/UNIT-FIXTURE-absent");
            else ((JArray)plan["before"]["parentDirectories"]).Add(watchedRoot + "/UNIT-FIXTURE-absent-parent");
            RehashPlan();
            Refuses("SAMPLE_DESTINATION_CHANGED");
        }

        [TestCase("acknowledged")]
        [TestCase("import-acknowledged")]
        [TestCase("unsupported-state")]
        [TestCase("incomplete-after")]
        [TestCase("missing-before")]
        [TestCase("malformed-before")]
        [TestCase("native-result")]
        public void AcknowledgedOrContradictoryStartingReceiptRefusesNoMutationPath(string fault)
        {
            if (fault == "acknowledged") journal["acknowledged"] = true;
            if (fault == "import-acknowledged") journal["importAcknowledged"] = true;
            if (fault == "unsupported-state") journal["state"] = "UNIT-FIXTURE-invalid-state";
            if (fault == "incomplete-after") journal["destinationAfter"] = new JObject();
            if (fault == "native-result") journal["nativeResult"] = new JObject { ["unitFixtureContradiction"] = true };
            if (fault == "missing-before" || fault == "malformed-before")
            {
                if (fault == "missing-before") plan.Remove("before"); else plan["before"] = "UNIT-FIXTURE-invalid-inventory";
                RehashPlan();
            }
            Refuses(null);
        }

        [Test] public void StaleRawReceiptHashRefusesWithoutChangingAnyBytes()
        {
            Publish();
            var bytes = File.ReadAllBytes(path);
            var result = Recover(id, new string('0', 64), dryRun: true);
            AssertPreserved(bytes);
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("JOURNAL_HASH_MISMATCH"));
        }

        [Test] public void ConfirmedNoMutationPublishesOnlyReceiptAndPreservesSdkTruth()
        {
            journal["importAcknowledged"] = false;
            var hash = Publish();
            var result = Recover(id, hash, (int)preview["nativePid"], (string)preview["nativeProcessStartTicks"], confirm: true);
            Assert.That(Snapshot(Path.Combine(root, "Assets")), Is.EquivalentTo(assetsBefore));
            Assert.That(Snapshot((string)preview["packageRoot"]), Is.EquivalentTo(sourceBefore));
            Assert.That(result.Ok, Is.True, result.Error?.Code + ": " + result.Error?.Message);
            Assert.That((bool?)result.Result["restored"], Is.True);
            Assert.That((bool?)result.Result["requiresNativeResolve"], Is.False);
            var saved = JObject.Parse(File.ReadAllText(path));
            Assert.That((string)saved["state"], Is.EqualTo("restored"));
            Assert.That((bool?)saved["observedNoMutation"], Is.True);
            foreach (var field in new[] { "samplePlan", "originalPackage", "editorPid", "nativeProcessStartTicks", "commandSha256", "unitFixture" })
                Assert.That(JToken.DeepEquals(saved[field], journal[field]), Is.True, "Changed original binding: " + field);
            Assert.That((bool?)saved["acknowledged"], Is.False);
            Assert.That((bool?)saved["importAcknowledged"], Is.False);
            Assert.That((bool?)saved["sdkRequestCancelled"], Is.False);
            Assert.That(saved["nativeStatus"], Is.Null);
            Assert.That(saved["nativeResult"], Is.Null);
        }

        [Test] public void ConfirmationRefusesChangedCurrentNativeIdentityAndPreservesReceipt()
        {
            var hash = Publish();
            var bytes = File.ReadAllBytes(path);
            var result = Recover(id, hash, int.MaxValue, "1", confirm: true);
            AssertPreserved(bytes);
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.Not.EqualTo("PACKAGE_OPERATION_FAILED"), result.Error.Message);
            Assert.That(result.Error.Code, Does.Contain("IDENTITY"));
        }

        private void Refuses(string code)
        {
            var hash = Publish();
            var bytes = File.ReadAllBytes(path);
            var result = Recover(id, hash, dryRun: true);
            AssertPreserved(bytes);
            Assert.That(result.Ok, Is.False, "Recovery accepted an unproved starting receipt.");
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.Not.Empty);
            Assert.That(result.Error.Code, Is.Not.EqualTo("PACKAGE_OPERATION_FAILED"), result.Error.Message);
            if (code != null) Assert.That(result.Error.Code, Is.EqualTo(code), result.Error.Message);
        }

        private void AssertPreserved(byte[] bytes)
        {
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes), "Dry-run/refusal changed receipt bytes.");
            Assert.That(Snapshot(Path.Combine(root, "Assets")), Is.EquivalentTo(assetsBefore));
            Assert.That(Snapshot((string)preview["packageRoot"]), Is.EquivalentTo(sourceBefore));

        }

        private string Publish()
        {
            path = (string)CallFiles("JournalPath", root, id);
            var hash = (string)CallFiles("Publish", root, journal, null);
            Assert.That(Hash(File.ReadAllBytes(path)), Is.EqualTo(hash));
            return hash;
        }

        private void RehashPlan()
        {
            plan.Remove("planSha256");
            plan["planSha256"] = CallFiles("HashJson", plan).ToString();
        }

        private static object CallFiles(string method, params object[] args)
        {
            var owner = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageOperationFiles");
            Assert.That(owner, Is.Not.Null);
            var member = owner.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(member, Is.Not.Null);
            try { return member.Invoke(null, args); }
            catch (TargetInvocationException exception) { throw exception.InnerException; }
        }

        private static Dictionary<string, string> Snapshot(string directory)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!Directory.Exists(directory)) { result[directory] = "absent"; return result; }
            result[directory] = "directory";
            foreach (var child in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories)) result[child] = "directory";
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories)) result[file] = Hash(File.ReadAllBytes(file));
            return result;
        }

        private static string Hash(byte[] bytes)
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        private static void AssertProcessAbsent(int pid)
        {
            try { using (var process = Process.GetProcessById(pid)) Assert.That(process.HasExited, Is.True, "Unit fixture PID must actually be absent."); }
            catch (ArgumentException) { }
        }

        private CommandResult<JObject> Recover(string operationId, string expectedJournalSha256,
            int expectedNativePid = 0, string expectedNativeProcessStartTicks = null, bool dryRun = false, bool confirm = false)
        {
            var member = typeof(PackageAuthoringCommands).GetMethod("RecoverStartingSample", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(member, Is.Not.Null);
            return (CommandResult<JObject>)member.Invoke(null, new object[] { root, operationId, expectedJournalSha256, package,
                "Archive", selectedRoot, selectedImport, currentCommandHash, expectedNativePid, expectedNativeProcessStartTicks, dryRun, confirm });
        }
    }
}
