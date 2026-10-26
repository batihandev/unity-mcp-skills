using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class PackageSampleContractTests
    {
        private string root;
        private string source;
        private const string Guid = "1234567890abcdef1234567890abcdef";
        [SetUp] public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "unity-package-contract-" + System.Guid.NewGuid().ToString("N"));
            source = Path.Combine(root, "Packages", "com.example.fixture", "Samples~", "Folder");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(Path.Combine(root, "Assets"));
            File.WriteAllText(Path.Combine(source, "hello.txt"), "sample\n", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(source, "hello.txt.meta"), "fileFormatVersion: 2\nguid: " + Guid + "\n");
        }
        [TearDown] public void TearDown() { if (Directory.Exists(root)) Directory.Delete(root, true); }
        private static Type Owner(string name)
        {
            var type = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages." + name);
            Assert.That(type, Is.Not.Null, "Missing package capability: " + name);
            return type;
        }
        private static object Call(string owner, string method, params object[] args)
        {
            var member = Owner(owner).GetMethod(method, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(member, Is.Not.Null, "Missing package behavior: " + method);
            try { return member.Invoke(null, args); }
            catch (TargetInvocationException exception) { throw exception.InnerException; }
        }
        private JObject Prepare(string target = "Assets/Samples/Fixture/1.0.0/Folder", bool overwrite = false, string selectedSource = null)
            => (JObject)Call("PackageSampleInventory", "Prepare", root, selectedSource ?? source, Path.Combine(root, "Packages", "com.example.fixture"), target, overwrite);
        private void Refuses(string code, Action action)
        {
            Owner("PackageSampleInventory");
            var exception = Assert.Catch<Exception>(() => action());
            Assert.That((string)exception.GetType().GetProperty("Code").GetValue(exception), Is.EqualTo(code), exception.ToString());
        }
        [Test] public void FolderPreviewContainsEveryFileAndMetaWithoutMutation()
        {
            var plan = Prepare();
            Assert.That(plan["entries"].Count(), Is.EqualTo(2));
            Assert.That(plan["entries"].Select(item => (string)item["path"]), Is.EquivalentTo(new[] { "Assets/Samples/Fixture/1.0.0/Folder/hello.txt", "Assets/Samples/Fixture/1.0.0/Folder/hello.txt.meta" }));
            Assert.That(plan["entries"].Single(item => ((string)item["path"]).EndsWith(".meta"))["guid"].Value<string>(), Is.EqualTo(Guid));
            Assert.That(Directory.GetFileSystemEntries(Path.Combine(root, "Assets")), Is.Empty);
            Assert.That((string)plan["planSha256"], Does.Match("^[a-f0-9]{64}$"));
        }
        [TestCase("Assets/../escape", "PATH_TRAVERSAL")]
        [TestCase("Packages/com.example.fixture", "PATH_OUTSIDE_ROOT")]
        [TestCase("Assets", "PATH_ROOT_FORBIDDEN")]
        public void FolderPreviewRefusesUnsafeDestination(string path, string code) => Refuses(code, () => Prepare(path));
        [Test] public void CollisionRequiresBoundOverwriteAndPreviewKeepsOriginalBytes()
        {
            var target = Path.Combine(root, "Assets/Samples/Fixture/1.0.0/Folder");
            Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target, "hello.txt"), "original");
            Refuses("SAMPLE_COLLISION", () => Prepare());
            var plan = Prepare(overwrite: true);
            Assert.That((string)plan["before"]["files"]["Assets/Samples/Fixture/1.0.0/Folder/hello.txt"]["bytesBase64"], Is.EqualTo(Convert.ToBase64String(Encoding.UTF8.GetBytes("original"))));
            Assert.That(File.ReadAllText(Path.Combine(target, "hello.txt")), Is.EqualTo("original"));
        }
        [Test] public void SourceChangedAfterPreviewCannotPassHashBoundValidation()
        {
            var plan = Prepare();
            File.WriteAllText(Path.Combine(source, "hello.txt"), "raced");
            Refuses("SAMPLE_SOURCE_CHANGED", () => Call("PackageSampleInventory", "ValidateCurrent", root, plan));
            Assert.That(Directory.GetFileSystemEntries(Path.Combine(root, "Assets")), Is.Empty);
        }
        [TestCase(false, "add")]
        [TestCase(false, "change")]
        [TestCase(false, "remove")]
        [TestCase(false, "directory")]
        [TestCase(false, "metadata")]
        [TestCase(false, "metadata-change")]
        [TestCase(false, "metadata-remove")]
        [TestCase(false, "directory-remove")]
        [TestCase(true, "add")]
        [TestCase(true, "change")]
        [TestCase(true, "remove")]
        [TestCase(true, "directory")]
        [TestCase(true, "metadata")]
        [TestCase(true, "metadata-change")]
        [TestCase(true, "metadata-remove")]
        [TestCase(true, "directory-remove")]
        public void LateForeignInventoryChangesRefuseWithoutAdoptingOrDeletingBytes(bool archive, string change)
        {
            var target = archive ? "Assets/Archive" : "Assets/Samples/Fixture/1.0.0/Folder";
            var selectedSource = archive ? CreateArchive(root, "Assets/Archive/hello.txt") : source;
            Directory.CreateDirectory(Path.Combine(root, target));
            var prior = Path.Combine(root, target, "prior.txt"); File.WriteAllText(prior, "prior");
            if (change.StartsWith("metadata-", StringComparison.Ordinal)) File.WriteAllText(prior + ".meta", "fileFormatVersion: 2\nguid: abcdef1234567890abcdef1234567890\n");
            if (change == "directory-remove") Directory.CreateDirectory(Path.Combine(root, target, "prior-directory"));
            var plan = Prepare(overwrite: true, selectedSource: selectedSource);
            var retained = (JObject)Call("PackageSampleInventory", "DestinationInventory", root, plan);
            var foreign = Path.Combine(root, target, "foreign.txt");
            if (change == "add") File.WriteAllText(foreign, "foreign");
            if (change == "change") File.WriteAllText(prior, "external");
            if (change == "remove") File.Delete(prior);
            if (change == "directory") Directory.CreateDirectory(Path.Combine(root, target, "foreign-directory"));
            if (change == "metadata") File.WriteAllText(prior + ".meta", "fileFormatVersion: 2\nguid: abcdef1234567890abcdef1234567890\n");
            if (change == "metadata-change") File.AppendAllText(prior + ".meta", "external: changed\n");
            if (change == "metadata-remove") File.Delete(prior + ".meta");
            if (change == "directory-remove") Directory.Delete(Path.Combine(root, target, "prior-directory"));
            var current = (JObject)Call("PackageSampleInventory", "DestinationInventory", root, plan);
            Refuses("SAMPLE_DESTINATION_CHANGED", () => Call("PackageSampleInventory", "ValidateCompletionInventory", root, plan, retained, current, false));
            Assert.That(Call("PackageSampleInventory", "DestinationInventory", root, plan).ToString(), Is.EqualTo(current.ToString()));
            Assert.That((string)plan["before"]["files"][target + "/prior.txt"]["bytesBase64"], Is.EqualTo(Convert.ToBase64String(Encoding.UTF8.GetBytes("prior"))));
        }
        [Test] public void ApprovedPayloadAndAncestorsRemainInsideCompletionFootprint()
        {
            var plan=Prepare(); var target=Path.Combine(root,"Assets/Samples/Fixture/1.0.0/Folder"); Directory.CreateDirectory(target);
            File.Copy(Path.Combine(source,"hello.txt"),Path.Combine(target,"hello.txt")); File.Copy(Path.Combine(source,"hello.txt.meta"),Path.Combine(target,"hello.txt.meta"));
            var current=(JObject)Call("PackageSampleInventory","DestinationInventory",root,plan);
            Call("PackageSampleInventory","ValidateCompletionInventory",root,plan,(JObject)plan["before"],current,false);
            Assert.That(current["files"].Count(),Is.EqualTo(2));
        }
        [Test] public void ApprovedPriorVersionRemovalIsBoundToAcknowledgementInventory()
        {
            var prior=Path.Combine(root,"Assets/Samples/Fixture/0.9.0/Folder"); Directory.CreateDirectory(prior); File.WriteAllText(Path.Combine(prior,"old.txt"),"old");
            var plan=Prepare(overwrite:true); Directory.Delete(Path.Combine(root,"Assets/Samples/Fixture/0.9.0"),true);
            var acknowledged=(JObject)Call("PackageSampleInventory","DestinationInventory",root,plan);
            Call("PackageSampleInventory","ValidateCompletionInventory",root,plan,(JObject)plan["before"],acknowledged,true);
            Call("PackageSampleInventory","ValidateCompletionInventory",root,plan,acknowledged,acknowledged,false);
            Assert.That((string)plan["before"]["files"]["Assets/Samples/Fixture/0.9.0/Folder/old.txt"]["bytesBase64"],Is.EqualTo(Convert.ToBase64String(Encoding.UTF8.GetBytes("old"))));
        }
        [Test] public void RetainedPlanContentHashIsRecomputedBeforeSourceReadback()
        {
            var plan=Prepare(); plan["entries"][0]["sha256"]="changed";
            Refuses("PLAN_HASH_MISMATCH", () => Call("PackageSampleInventory", "ValidatePlanHash", plan));
        }
        [Test] public void DeferredReadbackRefusesMissingDestinationsAsPending()
        {
            var plan = Prepare();
            Refuses("SAMPLE_IMPORT_PENDING", () => Call("PackageSampleInventory", "Readback", root, plan, true));
            Assert.That(Directory.GetFileSystemEntries(Path.Combine(root, "Assets")), Is.Empty);
        }
        [Test] public void DeferredReadbackDoesNotHideWrongExistingBytesBehindMissingMetadata()
        {
            var plan = Prepare();
            var target = Path.Combine(root, "Assets/Samples/Fixture/1.0.0/Folder");
            Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target, "hello.txt"), "external change");
            Refuses("SAMPLE_READBACK_MISMATCH", () => Call("PackageSampleInventory", "Readback", root, plan, true));
            Assert.That(File.ReadAllText(Path.Combine(target, "hello.txt")), Is.EqualTo("external change"));
        }
        [Test] public void DeferredReadbackDoesNotHideWrongMetadataGuidBehindMissingAsset()
        {
            var plan = Prepare();
            var target = Path.Combine(root, "Assets/Samples/Fixture/1.0.0/Folder");
            Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target, "hello.txt.meta"), "fileFormatVersion: 2\nguid: abcdef1234567890abcdef1234567890\n");
            Refuses("SAMPLE_GUID_MISMATCH", () => Call("PackageSampleInventory", "Readback", root, plan, true));
        }
        [Test] public void DeferredReadbackSourceChangeRefusesBeforePendingDestination()
        {
            var plan = Prepare();
            File.AppendAllText(Path.Combine(source, "hello.txt"), "changed");
            Refuses("SAMPLE_SOURCE_CHANGED", () => Call("PackageSampleInventory", "Readback", root, plan, true));
        }
        [Test] public void ImportedReadbackStillRefusesMissingDestinationsAsMismatch()
        {
            var plan = Prepare();
            Refuses("SAMPLE_READBACK_MISMATCH", () => Call("PackageSampleInventory", "Readback", root, plan, false));
        }
        [Test] public void DestinationChangedAfterPreviewCannotPassHashBoundValidation()
        {
            var plan = Prepare();
            var target = Path.Combine(root, "Assets/Samples/Fixture/1.0.0/Folder");
            Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target, "unrelated.txt"), "raced");
            Refuses("SAMPLE_DESTINATION_CHANGED", () => Call("PackageSampleInventory", "ValidateCurrent", root, plan));
            Assert.That(File.ReadAllText(Path.Combine(target, "unrelated.txt")), Is.EqualTo("raced"));
        }
        [Test] public void RecoveryRestoresExactFileMetaAndGuidAndRemovesOwnedNewFiles()
        {
            var target = Path.Combine(root, "Assets/Samples/Fixture/1.0.0/Folder");
            Directory.CreateDirectory(target);
            File.WriteAllBytes(Path.Combine(target, "hello.txt"), new byte[] { 239, 187, 191, 13, 10, 0, 42 });
            var before = File.ReadAllBytes(Path.Combine(target, "hello.txt"));
            var plan = Prepare(overwrite: true);
            File.Copy(Path.Combine(source, "hello.txt"), Path.Combine(target, "hello.txt"), true);
            File.Copy(Path.Combine(source, "hello.txt.meta"), Path.Combine(target, "hello.txt.meta"));
            var after = (JObject)Call("PackageSampleInventory", "DestinationInventory", root, plan);
            Call("PackageSampleInventory", "Restore", root, plan, after);
            Assert.That(File.ReadAllBytes(Path.Combine(target, "hello.txt")), Is.EqualTo(before));
            Assert.That(File.Exists(Path.Combine(target, "hello.txt.meta")), Is.False);
        }
        [Test] public void RecoveryRefusesChangedMetaWithoutDeletingAnyCapturedFile()
        {
            var plan = Prepare();
            var target = Path.Combine(root, "Assets/Samples/Fixture/1.0.0/Folder");
            Directory.CreateDirectory(target);
            File.Copy(Path.Combine(source, "hello.txt"), Path.Combine(target, "hello.txt"));
            File.Copy(Path.Combine(source, "hello.txt.meta"), Path.Combine(target, "hello.txt.meta"));
            var after = (JObject)Call("PackageSampleInventory", "DestinationInventory", root, plan);
            File.AppendAllText(Path.Combine(target, "hello.txt.meta"), "externalChange: true\n");
            Refuses("RECOVERY_HASH_MISMATCH", () => Call("PackageSampleInventory", "Restore", root, plan, after));
            Assert.That(File.ReadAllText(Path.Combine(target, "hello.txt")), Is.EqualTo("sample\n"));
            Assert.That(File.ReadAllText(Path.Combine(target, "hello.txt.meta")), Does.Contain("externalChange"));
        }
        [TestCase('0')]
        [TestCase('\0')]
        public void OfficialAttestationEnvelopeIsBoundToSourceWithoutCreatingTargets(char kind)
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt", envelopeName: "package/.attestation.p7m", envelopeKind: kind);
            var file = Path.Combine(directory, "sample.unitypackage");
            var bytes = File.ReadAllBytes(file);
            var plan = Prepare("Assets/IgnoredByArchive", selectedSource: directory);
            Assert.That(plan["entries"].Select(entry => (string)entry["path"]), Is.EquivalentTo(new[] { "Assets/ArchiveFixture/hello.txt", "Assets/ArchiveFixture/hello.txt.meta" }));
            Assert.That(plan["watched"].Values<string>(), Is.EquivalentTo(new[] { "Assets/ArchiveFixture", "Assets/ArchiveFixture.meta" }));
            Assert.That((long)plan["sourceInventory"]["files"]["NativeArchive/sample.unitypackage"]["length"], Is.EqualTo(bytes.Length));
            using (var sha = System.Security.Cryptography.SHA256.Create())
                Assert.That((string)plan["sourceInventory"]["files"]["NativeArchive/sample.unitypackage"]["sha256"], Is.EqualTo(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant()));
            Assert.That(Directory.GetFileSystemEntries(Path.Combine(root, "Assets")), Is.Empty);
            Assert.That(File.ReadAllBytes(file), Is.EqualTo(bytes));
            var after = (JObject)Call("PackageSampleInventory", "DestinationInventory", root, plan);
            Assert.That(((JObject)after["files"]).Properties().Select(item => item.Name), Does.Not.Contain("package/.attestation.p7m"));
            Call("PackageSampleInventory", "Restore", root, plan, after);
            Assert.That(File.ReadAllBytes(file), Is.EqualTo(bytes));
        }
        [Test] public void AttestationBytesChangedAfterPreviewRefuseBeforeImport()
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt", envelopeName: "package/.attestation.p7m");
            var plan = Prepare(selectedSource: directory);
            CreateArchive(root, "Assets/ArchiveFixture/hello.txt", envelopeName: "package/.attestation.p7m", envelopeBytes: new byte[] { 4, 5, 6 });
            Refuses("SAMPLE_SOURCE_CHANGED", () => Call("PackageSampleInventory", "ValidateCurrent", root, plan));
            Assert.That(Directory.GetFileSystemEntries(Path.Combine(root, "Assets")), Is.Empty);
        }
        [Test] public void DuplicateAttestationEnvelopeRefusesBeforeMutation()
            => Refuses("ARCHIVE_MEMBER_DUPLICATE", () => Prepare(selectedSource: CreateArchive(root, "Assets/ArchiveFixture/hello.txt", envelopeName: "package/.attestation.p7m", duplicateEnvelope: true)));
        [TestCase("Package/.attestation.p7m")]
        [TestCase("package/.Attestation.p7m")]
        [TestCase("package/.attestation.p7m/")]
        [TestCase("package/../.attestation.p7m")]
        [TestCase("../package/.attestation.p7m")]
        [TestCase("/package/.attestation.p7m")]
        [TestCase("package\\.attestation.p7m")]
        [TestCase("package/.attestation.p7m.extra")]
        [TestCase("package/unrelated")]
        [TestCase("other/.attestation.p7m")]
        public void AttestationLookalikeAndUnrelatedMembersRefuseBeforeMutation(string name)
            => Refuses("ARCHIVE_MEMBER_UNSAFE", () => Prepare(selectedSource: CreateArchive(root, "Assets/ArchiveFixture/hello.txt", envelopeName: name)));
        [TestCase('1')]
        [TestCase('2')]
        [TestCase('5')]
        [TestCase('x')]
        public void AttestationEnvelopeMustBeRegularFile(char kind)
            => Refuses("ARCHIVE_MEMBER_UNSAFE", () => Prepare(selectedSource: CreateArchive(root, "Assets/ArchiveFixture/hello.txt", envelopeName: "package/.attestation.p7m", envelopeKind: kind)));
        [TestCase(2147483648L)]
        [TestCase(8589934591L)]
        public void AttestationEnvelopeUnsupportedSizeRefusesBeforeAllocation(long length)
            => Refuses("ARCHIVE_MEMBER_UNSAFE", () => Prepare(selectedSource: CreateArchive(root, "Assets/ArchiveFixture/hello.txt", envelopeName: "package/.attestation.p7m", envelopeLength: length)));
        [Test] public void AttestationEnvelopeTruncatedPayloadRefusesBeforeMutation()
            => Refuses("ARCHIVE_MEMBER_UNSAFE", () => Prepare(selectedSource: CreateArchive(root, "Assets/ArchiveFixture/hello.txt", envelopeName: "package/.attestation.p7m", envelopeLength: 8192)));
        [Test] public void SafeArchivePreviewUsesActualPathnameAndGuidRatherThanImportPath()
        {
            var archive = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            var plan = Prepare("Assets/IgnoredByArchive", selectedSource: archive);
            Assert.That(plan["entries"].Select(entry => (string)entry["path"]), Is.EquivalentTo(new[] { "Assets/ArchiveFixture/hello.txt", "Assets/ArchiveFixture/hello.txt.meta" }));
            Assert.That(plan["entries"].Single(entry => ((string)entry["path"]).EndsWith(".meta"))["guid"].Value<string>(), Is.EqualTo(Guid));
            Assert.That(Directory.GetFileSystemEntries(Path.Combine(root, "Assets")), Is.Empty);
        }
        [TestCase("Assets/../escape.txt")]
        [TestCase("../outside.txt")]
        [TestCase("/absolute.txt")]
        [TestCase("Packages/com.example.fixture/escape.txt")]
        public void ArchivePathEscapeRefusesBeforeMutation(string path)
        {
            Refuses("ARCHIVE_PATH_UNSAFE", () => Prepare(selectedSource: CreateArchive(root, path)));
            Assert.That(Directory.GetFileSystemEntries(Path.Combine(root, "Assets")), Is.Empty);
        }
        [Test] public void ArchiveLinkMemberRefusesBeforeMutation()
            => Refuses("ARCHIVE_MEMBER_UNSAFE", () => Prepare(selectedSource: CreateArchive(root, "Assets/ArchiveFixture/hello.txt", '2')));
        [Test] public void ArchiveDuplicateGuidMemberRefusesBeforeMutation()
            => Refuses("ARCHIVE_MEMBER_DUPLICATE", () => Prepare(selectedSource: CreateArchive(root, "Assets/ArchiveFixture/hello.txt", duplicate: true)));
        [Test] public void ArchiveGuidRelocationToExistingAssetRefusesEvenWithOverwrite()
        {
            File.WriteAllText(Path.Combine(root, "Assets", "outside.txt"), "unrelated");
            File.WriteAllText(Path.Combine(root, "Assets", "outside.txt.meta"), "fileFormatVersion: 2\nguid: " + Guid + "\n");
            Refuses("ARCHIVE_GUID_RELOCATION", () => Prepare(overwrite: true, selectedSource: CreateArchive(root, "Assets/ArchiveFixture/hello.txt")));
            Assert.That(File.ReadAllText(Path.Combine(root, "Assets", "outside.txt")), Is.EqualTo("unrelated"));
        }
        [Test] public void ArchiveGuidRelocationFromRegisteredPackageAssetRefusesBeforeMutation()
        {
            const string packageAsset = "Packages/com.batihandev.unity-cli-commands/Editor/BatihanDev.UnityCliCommands.Editor.asmdef";
            var registeredGuid = AssetDatabase.AssetPathToGUID(packageAsset);
            Assert.That(registeredGuid, Does.Match("^[a-f0-9]{32}$"), "The owned package assembly fixture must have a registered GUID.");
            Assert.That(AssetDatabase.GUIDToAssetPath(registeredGuid), Is.EqualTo(packageAsset), "The prerequisite must be an actual registered Packages asset.");
            var actualRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            var target = "Assets/__Support3467GuidGuard/" + System.Guid.NewGuid().ToString("N") + "/escape.txt";
            Assert.That(Directory.Exists(Path.Combine(actualRoot, "Assets/__Support3467GuidGuard")), Is.False, "The mutation target must be absent.");
            var archive = CreateArchive(root, target, entryGuid: registeredGuid);
            Refuses("ARCHIVE_GUID_RELOCATION", () => Call("PackageSampleInventory", "Prepare", actualRoot, archive, Path.Combine(root, "Packages", "com.example.fixture"), "Assets/IgnoredByArchive", true));
            Assert.That(AssetDatabase.GUIDToAssetPath(registeredGuid), Is.EqualTo(packageAsset));
            Assert.That(Directory.Exists(Path.Combine(actualRoot, "Assets/__Support3467GuidGuard")), Is.False);
        }
        [Test] public void ArchiveGuidMismatchRefusesBeforeMutation()
            => Refuses("ARCHIVE_GUID_INVALID", () => Prepare(selectedSource: CreateArchive(root, "Assets/ArchiveFixture/hello.txt", metaGuid: "abcdef1234567890abcdef1234567890")));
        [Test] public void ArchiveTrailingMemberAfterTerminatorRefusesWithoutMutation()
        {
            var archive = CreateArchive(root, "Assets/ArchiveFixture/hello.txt", trailingMember: true);
            var before = File.ReadAllBytes(Path.Combine(archive, "sample.unitypackage"));
            Refuses("ARCHIVE_FORMAT_INVALID", () => Prepare(selectedSource: archive));
            Assert.That(File.ReadAllBytes(Path.Combine(archive, "sample.unitypackage")), Is.EqualTo(before));
            Assert.That(Directory.GetFileSystemEntries(Path.Combine(root, "Assets")), Is.Empty);
            Assert.That(File.ReadAllText(Path.Combine(source, "hello.txt.meta")), Does.Contain(Guid));
        }
        [Test] public void ArchiveAdditionalZeroPaddingPreservesValidInventory()
        {
            var archive = CreateArchive(root, "Assets/ArchiveFixture/hello.txt", extraZeroPadding: true);
            var before = File.ReadAllBytes(Path.Combine(archive, "sample.unitypackage"));
            var plan = Prepare(selectedSource: archive);
            Assert.That(plan["entries"].Select(entry => (string)entry["path"]), Is.EquivalentTo(new[] { "Assets/ArchiveFixture/hello.txt", "Assets/ArchiveFixture/hello.txt.meta" }));
            Assert.That(File.ReadAllBytes(Path.Combine(archive, "sample.unitypackage")), Is.EqualTo(before));
            Assert.That(Directory.GetFileSystemEntries(Path.Combine(root, "Assets")), Is.Empty);
        }
        [Test] public void NativeArchiveDirectoryPlansActualTargetsBeforeImport()
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            File.WriteAllText(Path.Combine(directory, "README.txt"), "native archive sibling");
            var plan = Prepare("Assets/IgnoredByArchive", selectedSource: directory);
            Assert.That((bool)plan["archive"], Is.True);
            Assert.That((string)plan["sampleRoot"], Is.EqualTo(directory));
            Assert.That((string)plan["source"], Is.EqualTo(Path.Combine(directory, "sample.unitypackage")));
            Assert.That(plan["entries"].Select(entry => (string)entry["path"]), Is.EquivalentTo(new[] { "Assets/ArchiveFixture/hello.txt", "Assets/ArchiveFixture/hello.txt.meta" }));
            Assert.That(plan["sourceInventory"]["files"]["NativeArchive/README.txt"], Is.Not.Null);
            Assert.That(Directory.GetFileSystemEntries(Path.Combine(root, "Assets")), Is.Empty);
        }
        [Test] public void MultipleTopLevelArchivesRefuseWithoutMutation()
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            File.Copy(Path.Combine(directory, "sample.unitypackage"), Path.Combine(directory, "second.unitypackage"));
            Refuses("SAMPLE_ARCHIVE_AMBIGUOUS", () => Prepare(selectedSource: directory));
            Assert.That(Directory.GetFileSystemEntries(Path.Combine(root, "Assets")), Is.Empty);
        }
        [Test] public void ArchiveAddedAfterFolderPreviewRefusesBeforeImport()
        {
            var plan = Prepare();
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            File.Copy(Path.Combine(directory, "sample.unitypackage"), Path.Combine(source, "new.unitypackage"));
            Refuses("SAMPLE_SOURCE_CHANGED", () => Call("PackageSampleInventory", "ValidateCurrent", root, plan));
        }
        [Test] public void SecondArchiveAddedAfterPreviewRefusesBeforeImport()
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            var plan = Prepare(selectedSource: directory);
            File.Copy(Path.Combine(directory, "sample.unitypackage"), Path.Combine(directory, "second.unitypackage"));
            Refuses("SAMPLE_SOURCE_CHANGED", () => Call("PackageSampleInventory", "ValidateCurrent", root, plan));
        }
        [TestCase("change")]
        [TestCase("remove")]
        [TestCase("add")]
        public void ArchiveSiblingChangesRefuseBeforeImport(string mutation)
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            var sibling = Path.Combine(directory, "README.txt");
            File.WriteAllText(sibling, "before");
            var plan = Prepare(selectedSource: directory);
            if (mutation == "remove") File.Delete(sibling);
            else if (mutation == "add") File.WriteAllText(Path.Combine(directory, "new.txt"), "new");
            else File.WriteAllText(sibling, "after");
            Refuses("SAMPLE_SOURCE_CHANGED", () => Call("PackageSampleInventory", "ValidateCurrent", root, plan));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void ArchiveDeletionOrReplacementRefusesBeforeImport(bool replace)
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            var plan = Prepare(selectedSource: directory);
            var archive = Path.Combine(directory, "sample.unitypackage");
            File.Delete(archive);
            if (replace) File.WriteAllText(archive, "replacement");
            Refuses("SAMPLE_SOURCE_CHANGED", () => Call("PackageSampleInventory", "ValidateCurrent", root, plan));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void ArchiveDirectoryRemovalOrNewEmptySubdirectoryRefusesBeforeImport(bool remove)
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            var plan = Prepare(selectedSource: directory);
            if (remove) Directory.Delete(directory, true);
            else Directory.CreateDirectory(Path.Combine(directory, "new-empty"));
            Refuses("SAMPLE_SOURCE_CHANGED", () => Call("PackageSampleInventory", "ValidateCurrent", root, plan));
        }
        [Test] public void NestedArchiveRemainsFolderContent()
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            Directory.CreateDirectory(Path.Combine(source, "Nested"));
            File.Copy(Path.Combine(directory, "sample.unitypackage"), Path.Combine(source, "Nested", "nested.unitypackage"));
            var plan = Prepare();
            Assert.That((bool)plan["archive"], Is.False);
            Assert.That(plan["entries"].Select(entry => (string)entry["path"]), Does.Contain("Assets/Samples/Fixture/1.0.0/Folder/Nested/nested.unitypackage"));
        }
        [Test] public void NativeSourceFileInsteadOfDirectoryRefuses()
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            Refuses("SAMPLE_SOURCE_MISSING", () => Prepare(selectedSource: Path.Combine(directory, "sample.unitypackage")));
        }
        [Test] public void SourceOutsideSelectedPackageRefuses()
        {
            var outside = Path.Combine(root, "OutsideSample");
            Directory.CreateDirectory(outside);
            File.WriteAllText(Path.Combine(outside, "keep.txt"), "unowned");
            Refuses("PATH_OUTSIDE_ROOT", () => Prepare(selectedSource: outside));
            Assert.That(File.ReadAllText(Path.Combine(outside, "keep.txt")), Is.EqualTo("unowned"));
        }
        [Test] public void ArchiveActualTargetCollisionRequiresOverwriteAndCapturesActualBytes()
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            Directory.CreateDirectory(Path.Combine(root, "Assets", "ArchiveFixture"));
            var target = Path.Combine(root, "Assets", "ArchiveFixture", "hello.txt");
            File.WriteAllText(target, "original");
            Refuses("SAMPLE_COLLISION", () => Prepare("Assets/IgnoredByArchive", selectedSource: directory));
            var plan = Prepare("Assets/IgnoredByArchive", overwrite: true, selectedSource: directory);
            Assert.That((string)plan["before"]["files"]["Assets/ArchiveFixture/hello.txt"]["bytesBase64"], Is.EqualTo(Convert.ToBase64String(Encoding.UTF8.GetBytes("original"))));
            Assert.That(File.ReadAllText(target), Is.EqualTo("original"));
        }
        [TestCase("archive")]
        [TestCase("sibling")]
        [TestCase("directory")]
        public void ArchiveDirectoryLinksRefuseBeforePreview(string kind)
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            var linked = kind == "archive" ? Path.Combine(directory, "sample.unitypackage") : kind == "directory" ? directory + "-link" : Path.Combine(directory, "sibling-link");
            var target = kind == "directory" ? directory : Path.Combine(source, "hello.txt");
            var isDirectory = kind == "directory";
            var targetFile = isDirectory ? Path.Combine(target, "sample.unitypackage") : target;
            var originalBytes = File.ReadAllBytes(targetFile);
            if (kind == "archive") File.Delete(linked);
            try
            {
                CreateSourceLink(linked, target, isDirectory);
                Refuses("PATH_REPARSE_POINT", () => Prepare(selectedSource: kind == "directory" ? linked : directory));
            }
            finally
            {
                if (isDirectory) { if (Directory.Exists(linked)) Directory.Delete(linked, false); }
                else if (File.Exists(linked)) File.Delete(linked);
                Assert.That(File.Exists(linked) || Directory.Exists(linked), Is.False, "Source link cleanup failed.");
                Assert.That(File.ReadAllBytes(targetFile), Is.EqualTo(originalBytes), "Source link target changed.");
            }
        }
        [Test] public void ArchiveSiblingLinkAddedAfterPreviewRefusesBeforeImport()
        {
            var directory = CreateArchive(root, "Assets/ArchiveFixture/hello.txt");
            var plan = Prepare(selectedSource: directory);
            var linked = Path.Combine(directory, "late-link");
            var targetFile = Path.Combine(source, "hello.txt");
            var targetMeta = targetFile + ".meta";
            var originalBytes = File.ReadAllBytes(targetFile);
            var originalMetaBytes = File.ReadAllBytes(targetMeta);
            try
            {
                CreateSourceLink(linked, source, true);
                Refuses("PATH_REPARSE_POINT", () => Call("PackageSampleInventory", "ValidateCurrent", root, plan));
            }
            finally
            {
                if (Directory.Exists(linked)) Directory.Delete(linked, false);
                Assert.That(File.Exists(linked) || Directory.Exists(linked), Is.False, "Source link cleanup failed.");
                Assert.That(File.ReadAllBytes(targetFile), Is.EqualTo(originalBytes), "Source link target changed.");
                Assert.That(File.ReadAllBytes(targetMeta), Is.EqualTo(originalMetaBytes), "Source link target metadata changed.");
            }
        }
        private void CreateSourceLink(string linked, string target, bool isDirectory)
        {
            var windows = UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WindowsEditor;
            var arguments = windows
                ? "/d /c mklink " + (isDirectory ? "/D " : "") + "\"" + linked + "\" \"" + target + "\""
                : "-s \"" + target + "\" \"" + linked + "\"";
            var start = new System.Diagnostics.ProcessStartInfo(windows ? "cmd.exe" : "ln", arguments)
            {
                WorkingDirectory = root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using (var process = System.Diagnostics.Process.Start(start))
            {
                Assert.That(process, Is.Not.Null, "Source link command did not start.");
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(10000))
                {
                    process.Kill();
                    Assert.Fail("Source link command exceeded its 10-second deadline.");
                }
                Assert.That(System.Threading.Tasks.Task.WaitAll(new System.Threading.Tasks.Task[] { output, error }, 10000), Is.True, "Source link output capture exceeded its 10-second deadline.");
                Assert.That(process.ExitCode, Is.Zero, "Source link creation requires Unix ln or Windows Developer Mode / symbolic-link privilege. stdout: " + output.Result + " stderr: " + error.Result);
            }
            Assert.That(File.GetAttributes(linked) & FileAttributes.ReparsePoint, Is.EqualTo(FileAttributes.ReparsePoint), "Source fixture must be an actual symbolic link.");
            Assert.That((File.GetAttributes(linked) & FileAttributes.Directory) != 0, Is.EqualTo(isDirectory), "Source link kind differs from its scenario.");
        }
        internal static string CreateArchive(string root, string target, char kind = '0', bool duplicate = false, string metaGuid = null, string entryGuid = null, bool trailingMember = false, bool extraZeroPadding = false, string envelopeName = null, char envelopeKind = '0', bool duplicateEnvelope = false, long? envelopeLength = null, byte[] envelopeBytes = null)
        {
            var directory = Path.Combine(root, "Packages", "com.example.fixture", "Samples~", "NativeArchive");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "sample.unitypackage");
            var memberGuid = entryGuid ?? Guid;
            using (var file = File.Create(path))
            using (var gzip = new GZipStream(file, CompressionMode.Compress))
            {
                if (envelopeName != null)
                {
                    Tar(gzip, envelopeName, envelopeBytes ?? new byte[] { 1, 2, 3 }, envelopeKind, envelopeLength);
                    if (duplicateEnvelope) Tar(gzip, envelopeName, new byte[] { 1, 2, 3 }, envelopeKind);
                }
                Tar(gzip, memberGuid + "/pathname", Encoding.UTF8.GetBytes(target), kind);
                Tar(gzip, memberGuid + "/asset", Encoding.UTF8.GetBytes("archive\n"));
                Tar(gzip, memberGuid + "/asset.meta", Encoding.UTF8.GetBytes("fileFormatVersion: 2\nguid: " + (metaGuid ?? memberGuid) + "\n"));
                if (duplicate) Tar(gzip, memberGuid + "/pathname", Encoding.UTF8.GetBytes(target));
                gzip.Write(new byte[1024], 0, 1024);
                if (trailingMember) Tar(gzip, "abcdef1234567890abcdef1234567890/pathname", Encoding.UTF8.GetBytes("Assets/HiddenArchive/escape.txt"));
                if (extraZeroPadding) gzip.Write(new byte[2048], 0, 2048);
            }
            return directory;
        }
        private static void Tar(Stream stream, string name, byte[] bytes, char type = '0', long? declaredLength = null)
        {
            var header = new byte[512];
            Encoding.ASCII.GetBytes(name).CopyTo(header, 0);
            Encoding.ASCII.GetBytes("0000644\0").CopyTo(header, 100);
            Encoding.ASCII.GetBytes("0000000\0").CopyTo(header, 108);
            Encoding.ASCII.GetBytes("0000000\0").CopyTo(header, 116);
            Encoding.ASCII.GetBytes(Convert.ToString(declaredLength ?? bytes.Length, 8).PadLeft(11, '0') + "\0").CopyTo(header, 124);
            Encoding.ASCII.GetBytes("00000000000\0").CopyTo(header, 136);
            for (var index = 148; index < 156; index++) header[index] = 32;
            header[156] = (byte)type;
            Encoding.ASCII.GetBytes("ustar\0").CopyTo(header, 257);
            Encoding.ASCII.GetBytes(Convert.ToString(header.Sum(value => (int)value), 8).PadLeft(6, '0') + "\0 ").CopyTo(header, 148);
            stream.Write(header, 0, 512);
            stream.Write(bytes, 0, bytes.Length);
            stream.Write(new byte[(512 - bytes.Length % 512) % 512], 0, (512 - bytes.Length % 512) % 512);
        }
    }
}
