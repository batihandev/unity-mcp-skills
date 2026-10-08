using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BatihanDev.UnityCliCommands.Packages;
using NUnit.Framework;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class PackageOperationPublicationContractTests
    {
        private string directory;
        private string target;
        private string staged;

        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "unity-package-publication-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            target = Path.Combine(directory, "journal.json");
            staged = Path.Combine(directory, "journal.stage");
        }

        [TearDown] public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test] public void ExistingJournalPublicationKeepsEveryConcurrentReadPresentAndWhole()
        {
            var first = Payload(0x35);
            var second = Payload(0xa7);
            var firstHash = Sha256(first);
            var secondHash = Sha256(second);
            WriteStaged(target, first);
            var done = false;
            var reads = 0;
            var missing = 0;
            var errors = 0;
            var partial = 0;
            Exception firstError = null;
            using (var started = new ManualResetEventSlim())
            {
                var reader = Task.Run(() =>
                {
                    while (!Volatile.Read(ref done))
                    {
                        try
                        {
                            byte[] bytes;
                            using (var file = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                            using (var contents = new MemoryStream())
                            {
                                file.CopyTo(contents);
                                bytes = contents.ToArray();
                            }
                            var hash = Sha256(bytes);
                            if (bytes.Length != first.Length || (hash != firstHash && hash != secondHash)) partial++;
                            reads++;
                        }
                        catch (FileNotFoundException exception) { missing++; if (firstError == null) firstError = exception; }
                        catch (Exception exception) { errors++; if (firstError == null) firstError = exception; }
                        finally { started.Set(); }
                    }
                });
                try
                {
                    Assert.That(started.Wait(TimeSpan.FromSeconds(10)), Is.True, "Concurrent reader did not start.");
                    var expected = firstHash;
                    for (var index = 0; index < 500; index++)
                    {
                        var next = index % 2 == 0 ? second : first;
                        var nextHash = index % 2 == 0 ? secondHash : firstHash;
                        WriteStaged(staged, next);
                        PackageOperationFiles.PublishStaged(staged, target, expected);
                        Assert.That(File.Exists(staged), Is.False, "Publication must consume the staged file.");
                        Assert.That(File.Exists(target), Is.True, "Publication must retain the target name.");
                        Assert.That(Sha256(File.ReadAllBytes(target)), Is.EqualTo(nextHash));
                        expected = nextHash;
                    }
                }
                finally
                {
                    Volatile.Write(ref done, true);
                    reader.GetAwaiter().GetResult();
                }
            }
            var evidence = "reads=" + reads + ", missing=" + missing + ", errors=" + errors + ", partial=" + partial + ", firstError=" + firstError;
            TestContext.WriteLine(evidence);
            Assert.That(reads, Is.GreaterThan(0), evidence);
            Assert.That(missing, Is.Zero, evidence);
            Assert.That(errors, Is.Zero, evidence);
            Assert.That(partial, Is.Zero, evidence);
            Assert.That(Sha256(File.ReadAllBytes(target)), Is.EqualTo(firstHash));
        }

        [TestCase("changed")]
        [TestCase("missing")]
        public void ExistingPublicationRefusesChangedOrMissingTargetAndPreservesStage(string change)
        {
            var original = Payload(0x35);
            var foreign = Payload(0x61);
            var replacement = Payload(0xa7);
            WriteStaged(target, original);
            WriteStaged(staged, replacement);
            if (change == "missing") File.Delete(target); else File.WriteAllBytes(target, foreign);
            var error = Assert.Throws<PackageOperationException>(() => PackageOperationFiles.PublishStaged(staged, target, Sha256(original)));
            Assert.That(error.Code, Is.EqualTo("JOURNAL_CHANGED"));
            Assert.That(File.ReadAllBytes(staged), Is.EqualTo(replacement));
            if (change == "missing") Assert.That(File.Exists(target), Is.False);
            else Assert.That(File.ReadAllBytes(target), Is.EqualTo(foreign));
        }

        [Test] public void InitialPublicationRefusesForeignTargetAndPreservesBothFiles()
        {
            var foreign = Payload(0x61);
            var replacement = Payload(0xa7);
            WriteStaged(target, foreign);
            WriteStaged(staged, replacement);
            var error = Assert.Throws<PackageOperationException>(() => PackageOperationFiles.PublishStaged(staged, target, null));
            Assert.That(error.Code, Is.EqualTo("JOURNAL_CHANGED"));
            Assert.That(File.ReadAllBytes(target), Is.EqualTo(foreign));
            Assert.That(File.ReadAllBytes(staged), Is.EqualTo(replacement));
        }

        [Test] public void MissingStagedFileCannotRemoveExistingJournal()
        {
            var original = Payload(0x35);
            WriteStaged(target, original);
            var error = Assert.Throws<PackageOperationException>(() => PackageOperationFiles.PublishStaged(staged, target, Sha256(original)));
            Assert.That(error.Code, Is.EqualTo("JOURNAL_CHANGED"));
            Assert.That(File.ReadAllBytes(target), Is.EqualTo(original));
            Assert.That(File.Exists(staged), Is.False);
        }

        [Test] public void InitialPublicationConsumesOnlyItsStageAndPublishesExactBytes()
        {
            var replacement = Payload(0xa7);
            var other = Path.Combine(directory, "other.json");
            var foreign = Payload(0x61);
            WriteStaged(staged, replacement);
            WriteStaged(other, foreign);
            PackageOperationFiles.PublishStaged(staged, target, null);
            Assert.That(File.ReadAllBytes(target), Is.EqualTo(replacement));
            Assert.That(File.ReadAllBytes(other), Is.EqualTo(foreign));
            Assert.That(File.Exists(staged), Is.False);
        }

        private static byte[] Payload(byte value)
        {
            var bytes = new byte[361085];
            for (var index = 0; index < bytes.Length; index++) bytes[index] = value;
            return bytes;
        }

        private static string Sha256(byte[] bytes)
        {
            using (var algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        private static void WriteStaged(string path, byte[] bytes)
        {
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes, 0, bytes.Length);
                file.Flush(true);
            }
        }
    }
}
