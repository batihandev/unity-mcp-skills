using System;
using System.IO;
using System.Reflection;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class PackageEmbedObserverContractTests
    {
        private string root;
        private object observer;
        private JObject journal;
        [SetUp] public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "unity-embed-contract-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            journal = new JObject { ["schema"] = "unity.package.operation@1", ["operationId"] = Guid.NewGuid().ToString("N"), ["project"] = root,
                ["kind"] = "embed", ["state"] = "pending", ["acknowledged"] = true, ["observerDetached"] = false };
        }
        [TestCase("delete")]
        [TestCase("change")]
        public void PublicationFinalizationRefusesLostOrChangedExistingJournal(string race)
        {
            var owner = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageOperationFiles");
            var publish = owner.GetMethod("Publish", BindingFlags.Static | BindingFlags.NonPublic);
            var expected = (string)publish.Invoke(null, new object[] { root, journal, null });
            var path = JournalPath((string)journal["operationId"]);
            var staged = path + ".test.tmp"; File.WriteAllText(staged, "new");
            if (race == "delete") File.Delete(path); else File.WriteAllText(path, "foreign");
            var finish = owner.GetMethod("PublishStaged", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(finish, Is.Not.Null);
            var error = Assert.Catch<TargetInvocationException>(() => finish.Invoke(null, new object[] { staged, path, expected }));
            Assert.That((string)error.InnerException.GetType().GetProperty("Code").GetValue(error.InnerException), Is.EqualTo("JOURNAL_CHANGED"));
            Assert.That(File.ReadAllText(staged), Is.EqualTo("new"));
            if (race == "delete") Assert.That(File.Exists(path), Is.False); else Assert.That(File.ReadAllText(path), Is.EqualTo("foreign"));
        }
        [TearDown] public void TearDown()
        {
            if (observer != null) Invoke("Interrupt", "test-finally");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        private void Start(Func<JObject> native, double timeout = 30)
        {
            var type = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageEmbedObserver");
            Assert.That(type, Is.Not.Null, "Missing bounded public EmbedRequest observer");
            var method = type.GetMethod("StartForRequest", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing request-bound observer start");
            try { observer = method.Invoke(null, new object[] { root, journal, native, timeout }); }
            catch (TargetInvocationException exception) { throw exception.InnerException; }
        }
        private void Invoke(string method, params object[] args)
        {
            try { observer.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(observer, args); }
            catch (TargetInvocationException exception) { throw exception.InnerException; }
        }
        private JObject Read() => JObject.Parse(File.ReadAllText(JournalPath((string)journal["operationId"])));
        private string JournalPath(string id)
        {
            var owner = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageOperationFiles");
            var method = owner.GetMethod("JournalPath", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(null, new object[] { root, id });
        }
        [Test] public void ActualPublicationSurvivesRemovalOfOnlyUnitFixtureTempDirectory()
        {
            var owner = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageOperationFiles");
            var method = owner.GetMethod("Publish", BindingFlags.Static | BindingFlags.NonPublic);
            var hash = (string)method.Invoke(null, new object[] { root, journal, null });
            var actual = JournalPath((string)journal["operationId"]);
            var bytes = File.ReadAllBytes(actual);
            var temp = Path.Combine(root, "Temp");
            Directory.CreateDirectory(temp);
            File.WriteAllText(Path.Combine(temp, "UNIT-FIXTURE-temp-marker.txt"), "unit fixture only");
            Directory.Delete(temp, true);
            var expected = Path.Combine(root, "Library/unity-cli-package-operations", (string)journal["operationId"] + ".json");
            Assert.That(File.Exists(expected), Is.True, "Published package journal must survive Editor Temp removal in Library.");
            Assert.That(Path.GetFullPath(actual), Is.EqualTo(Path.GetFullPath(expected)));
            Assert.That(File.ReadAllBytes(expected), Is.EqualTo(bytes));
            using (var algorithm = System.Security.Cryptography.SHA256.Create())
                Assert.That(BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(expected))).Replace("-", "").ToLowerInvariant(), Is.EqualTo(hash));
            Assert.That(File.Exists(Path.Combine(root, "Temp/unity-cli-package-operations", (string)journal["operationId"] + ".json")), Is.False);
        }
        [TestCase(false)]
        [TestCase(true)]
        public void PersistentJournalAndStagedPublicationPathsAreAccepted(bool staged)
        {
            var relative = "Library/unity-cli-package-operations/" + (string)journal["operationId"] + ".json";
            if (staged) relative += ".1234567890abcdef1234567890abcdef.tmp";
            var result = ValidateOperationPath(relative);
            Assert.That(result.Ok, Is.True, result.Error?.Code + ": " + result.Error?.Message);
        }
        [Test] public void VolatileTempJournalPathIsRefused()
        {
            var result = ValidateOperationPath("Temp/unity-cli-package-operations/" + (string)journal["operationId"] + ".json");
            Assert.That(result.Ok, Is.False, "Temp is regenerable Editor storage and cannot own package recovery journals.");
            Assert.That(result.Error.Code, Is.EqualTo("PATH_OUTSIDE_ROOT"));
        }
        private CommandResult<ProjectPathResult> ValidateOperationPath(string relative)
        {
            var method = typeof(ProjectPathPolicy).GetMethod("ValidatePackageOperationFile", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (CommandResult<ProjectPathResult>)method.Invoke(null, new object[] { relative, root });
        }
        private static MethodInfo ReconciliationMethod()
        {
            var type = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageEmbedObserver");
            Assert.That(type, Is.Not.Null, "Missing bounded Embed observer owner");
            var method = type.GetMethod("ReconcileForRequest", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing exact same-process SDK terminal reconciliation");
            return method;
        }
        private object Reconcile(string expectedHash = null)
        {
            var method = ReconciliationMethod();
            var bytes = File.ReadAllBytes(JournalPath((string)journal["operationId"]));
            string hash; using (var algorithm = System.Security.Cryptography.SHA256.Create()) hash = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            try { return method.Invoke(null, new object[] { root, (string)journal["operationId"], expectedHash ?? hash }); }
            catch (TargetInvocationException exception) { throw exception.InnerException; }
        }
        [TestCase("Success")]
        [TestCase("Failure")]
        public void DetachedRequestReconcilesOnlyActualSdkTerminalAndRetainsDetachEvidence(string status)
        {
            var completed = false;
            Start(() => new JObject { ["completed"] = completed, ["status"] = completed ? status : "InProgress",
                ["result"] = completed && status == "Success" ? new JObject { ["name"] = "com.example.fixture", ["version"] = "1.0.0", ["source"] = "Embedded" } : null,
                ["error"] = completed && status == "Failure" ? new JObject { ["code"] = 7, ["message"] = "SDK terminal after detach" } : null });
            Invoke("Interrupt", "host-interruption");
            var original = Read();
            Assert.That((bool)original["requestUnresolved"], Is.True);
            completed = true;
            Reconcile();
            var terminal = Read();
            Assert.That((bool)terminal["requestUnresolved"], Is.False);
            Assert.That((string)terminal["nativeStatus"], Is.EqualTo(status));
            Assert.That((string)terminal["detachedEvidence"]["state"], Is.EqualTo("interrupted"));
            Assert.That((string)terminal["detachedEvidence"]["detachReason"], Is.EqualTo("host-interruption"));
            Assert.That((bool)terminal["sdkRequestCancelled"], Is.False);
            if (status == "Failure") Assert.That((string)terminal["nativeError"]["message"], Is.EqualTo("SDK terminal after detach"));
            else Assert.That((string)terminal["nativeResult"]["name"], Is.EqualTo("com.example.fixture"));
        }
        [Test] public void ReconciliationRefusesPendingSdkWithoutChangingReceipt()
        {
            Start(() => new JObject { ["completed"] = false, ["status"] = "InProgress" });
            Invoke("Interrupt", "host-interruption");
            var before = Read().ToString();
            ReconciliationMethod();
            var exception = Assert.Catch<Exception>(() => Reconcile());
            Assert.That(exception.GetType().GetProperty("Code"), Is.Not.Null, "Reconciliation must exist and refuse the actual pending SDK request.");
            Assert.That((string)exception.GetType().GetProperty("Code").GetValue(exception), Is.EqualTo("REQUEST_UNRESOLVED"));
            Assert.That(Read().ToString(), Is.EqualTo(before));
        }
        [Test] public void ReconciliationRefusesStaleReceiptHashWithoutChangingJournal()
        {
            Start(() => new JObject { ["completed"] = true, ["status"] = "Success" });
            Invoke("Interrupt", "host-interruption");
            var before = Read().ToString();
            ReconciliationMethod();
            var exception = Assert.Catch<Exception>(() => Reconcile(new string('0', 64)));
            Assert.That(exception.GetType().GetProperty("Code"), Is.Not.Null, "Reconciliation must exist and bind the exact saved receipt hash.");
            Assert.That((string)exception.GetType().GetProperty("Code").GetValue(exception), Is.EqualTo("JOURNAL_HASH_MISMATCH"));
            Assert.That(Read().ToString(), Is.EqualTo(before));
        }
        [Test] public void ExactPrepublishedRequestJournalCanAttachItsOwnObserver()
        {
            journal["requestUnresolved"] = true;
            var directory = Path.GetDirectoryName(JournalPath((string)journal["operationId"]));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, (string)journal["operationId"] + ".json");
            File.WriteAllText(path, journal.ToString(Newtonsoft.Json.Formatting.None), new System.Text.UTF8Encoding(false));
            Start(() => new JObject { ["completed"] = false, ["status"] = "InProgress" });
            Invoke("Poll");
            Assert.That((string)Read()["state"], Is.EqualTo("pending"));
            Assert.That((bool)Read()["observerDetached"], Is.False);
        }
        [Test] public void PendingRequestDoesNotBecomeSuccessFromAcknowledgement()
        {
            Start(() => new JObject { ["completed"] = false, ["status"] = "InProgress" });
            Invoke("Poll");
            var state = Read();
            Assert.That((string)state["state"], Is.EqualTo("pending"));
            Assert.That((bool)state["observerDetached"], Is.False);
            Assert.That(state["nativeResult"], Is.Null);
        }
        [Test] public void TerminalSdkSuccessPersistsExactResultAndDetachesObserver()
        {
            Start(() => new JObject { ["completed"] = true, ["status"] = "Success", ["result"] = new JObject { ["name"] = "com.example.fixture", ["version"] = "1.0.0", ["source"] = "Embedded" } });
            Invoke("Poll");
            var state = Read();
            Assert.That((string)state["state"], Is.EqualTo("awaiting-readback"));
            Assert.That((bool)state["nativeCompleted"], Is.True);
            Assert.That((bool)state["requestUnresolved"], Is.False);
            Assert.That(state["destinationAfter"], Is.Null);
            Assert.That(state["manifestAfter"], Is.Null);
            Assert.That(state["lockAfter"], Is.Null);
            Assert.That((string)state["nativeStatus"], Is.EqualTo("Success"));
            Assert.That((string)state["nativeResult"]["name"], Is.EqualTo("com.example.fixture"));
            Assert.That((bool)state["observerDetached"], Is.True);
            Assert.That((bool)state["sdkRequestCancelled"], Is.False);
        }
        [Test] public void SdkSuccessBeforeEmbeddedRegistrationDoesNotCaptureRegistryLockOrDestination()
        {
            journal["name"] = "com.example.fixture"; journal["destination"] = "Packages/com.example.fixture";
            Directory.CreateDirectory(Path.Combine(root, "Packages"));
            File.WriteAllText(Path.Combine(root, "Packages/manifest.json"), "{\"dependencies\":{}}");
            File.WriteAllText(Path.Combine(root, "Packages/packages-lock.json"), "{\"dependencies\":{}}");
            Start(() => new JObject { ["completed"] = true, ["status"] = "Success", ["result"] = new JObject { ["name"] = "com.example.fixture", ["version"] = "1.0.0", ["source"] = "Embedded" } });
            Invoke("Poll");
            var beforeReload = File.ReadAllBytes(JournalPath((string)journal["operationId"]));
            Assert.That((string)Read()["state"], Is.EqualTo("awaiting-readback"));
            Assert.That(Read()["destinationAfter"], Is.Null);
            Assert.That(Read()["manifestAfter"], Is.Null);
            Assert.That(Read()["lockAfter"], Is.Null);
            Invoke("Reload");
            Assert.That(File.ReadAllBytes(JournalPath((string)journal["operationId"])), Is.EqualTo(beforeReload));
            Assert.That((bool)Read()["requestUnresolved"], Is.False);
            Assert.That((string)Read()["nativeStatus"], Is.EqualTo("Success"));
        }
        [Test] public void NativeFailurePreservesSdkCodeMessageAndDetaches()
        {
            Start(() => new JObject { ["completed"] = true, ["status"] = "Failure", ["error"] = new JObject { ["code"] = 7, ["message"] = "SDK exact error" } });
            Invoke("Poll");
            var state = Read();
            Assert.That((string)state["state"], Is.EqualTo("failed"));
            Assert.That((int)state["nativeError"]["code"], Is.EqualTo(7));
            Assert.That((string)state["nativeError"]["message"], Is.EqualTo("SDK exact error"));
            Assert.That((bool)state["observerDetached"], Is.True);
        }
        [TestCase("assembly-reload")]
        [TestCase("editor-quitting")]
        [TestCase("host-interruption")]
        public void InterruptedObserverRecordsUnresolvedRequestWithoutClaimingCancellation(string reason)
        {
            Start(() => new JObject { ["completed"] = false, ["status"] = "InProgress" });
            Invoke("Interrupt", reason);
            var state = Read();
            Assert.That((string)state["state"], Is.EqualTo("interrupted"));
            Assert.That((string)state["detachReason"], Is.EqualTo(reason));
            Assert.That((bool)state["observerDetached"], Is.True);
            Assert.That((bool)state["sdkRequestCancelled"], Is.False);
            Assert.That((bool)state["requestUnresolved"], Is.True);
        }
        [Test] public void DeadlineDetachesObserverAndRetainsUnresolvedRequest()
        {
            Start(() => new JObject { ["completed"] = false, ["status"] = "InProgress" }, 0.001);
            System.Threading.Thread.Sleep(10);
            Invoke("Poll");
            var state = Read();
            Assert.That((string)state["state"], Is.EqualTo("timed-out"));
            Assert.That((bool)state["observerDetached"], Is.True);
            Assert.That((bool)state["requestUnresolved"], Is.True);
        }
        [Test] public void ObservationExceptionDetachesAndKeepsFailureEvidence()
        {
            Start(() => throw new InvalidOperationException("observation error"));
            Invoke("Poll");
            var state = Read();
            Assert.That((string)state["state"], Is.EqualTo("observer-error"));
            Assert.That((string)state["observerError"], Does.Contain("observation error"));
            Assert.That((bool)state["observerDetached"], Is.True);
            Assert.That((bool)state["requestUnresolved"], Is.True);
        }
        [TestCase("deadline")]
        [TestCase("assembly-reload")]
        [TestCase("host-interruption")]
        [TestCase("observer-error")]
        public void DetachedUnresolvedRequestRefusesAnotherOperation(string reason)
        {
            Start(reason == "observer-error" ? (Func<JObject>)(() => throw new InvalidOperationException("native observation lost")) :
                () => new JObject { ["completed"] = false, ["status"] = "InProgress" }, reason == "deadline" ? 0.001 : 30);
            if (reason == "deadline") { System.Threading.Thread.Sleep(10); Invoke("Poll"); }
            else if (reason == "observer-error") Invoke("Poll");
            else Invoke("Interrupt", reason);
            Assert.That((bool)Read()["requestUnresolved"], Is.True);
            journal = (JObject)journal.DeepClone(); journal["operationId"] = Guid.NewGuid().ToString("N");
            var error = Assert.Catch<Exception>(() => Start(() => new JObject { ["completed"] = false, ["status"] = "InProgress" }));
            Assert.That(error, Is.Not.Null, "An unresolved detached SDK request must block a new package operation.");
            Assert.That((string)error.GetType().GetProperty("Code").GetValue(error), Is.EqualTo("PACKAGE_REQUEST_UNRESOLVED"));
        }
        [Test] public void UnsealedTerminalSdkSuccessBlocksAnotherPackageOperation()
        {
            Start(() => new JObject { ["completed"] = true, ["status"] = "Success", ["result"] = new JObject { ["name"] = "com.example.fixture", ["version"] = "1.0.0", ["source"] = "Embedded" } });
            Invoke("Poll");
            Assert.That((bool)Read()["requestUnresolved"], Is.False);
            journal = (JObject)journal.DeepClone(); journal["operationId"] = Guid.NewGuid().ToString("N");
            var error = Assert.Catch<Exception>(() => Start(() => new JObject { ["completed"] = false, ["status"] = "InProgress" }));
            Assert.That((string)error.GetType().GetProperty("Code").GetValue(error), Is.EqualTo("PACKAGE_REQUEST_UNRESOLVED"));
        }
        [Test] public void AnotherActiveEmbedRefusesWithoutReplacingFirstJournal()
        {
            Start(() => new JObject { ["completed"] = false, ["status"] = "InProgress" });
            var original = observer;
            var id = (string)journal["operationId"];
            journal = (JObject)journal.DeepClone(); journal["operationId"] = Guid.NewGuid().ToString("N");
            var error = Assert.Catch<Exception>(() => Start(() => new JObject { ["completed"] = false, ["status"] = "InProgress" }));
            observer = original;
            Assert.That((string)error.GetType().GetProperty("Code").GetValue(error), Is.EqualTo("EMBED_ACTIVE"));
            Assert.That(File.Exists(JournalPath((string)journal["operationId"])), Is.False);
            Assert.That((string)JObject.Parse(File.ReadAllText(JournalPath(id)))["state"], Is.EqualTo("pending"));
        }
        [Test] public void StartupRecoveryPreservesKnownTerminalAwaitingReadbackBytesAndSdkTruth()
        {
            journal["state"] = "awaiting-readback"; journal["nativeCompleted"] = true;
            journal["nativeStatus"] = "Success"; journal["requestUnresolved"] = false;
            journal["observerDetached"] = true; journal["nativeResult"] = new JObject { ["name"] = "com.example.fixture", ["version"] = "1.0.0", ["source"] = "Embedded" };
            var files = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageOperationFiles");
            files.GetMethod("Publish",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[] {root,journal,null});
            var before = File.ReadAllBytes(JournalPath((string)journal["operationId"]));
            StartupRecovery();
            Assert.That(File.ReadAllBytes(JournalPath((string)journal["operationId"])), Is.EqualTo(before));
            Assert.That((bool)Read()["nativeCompleted"],Is.True); Assert.That((string)Read()["nativeStatus"],Is.EqualTo("Success"));
            Assert.That((bool)Read()["requestUnresolved"],Is.False); Assert.That(Read()["destinationAfter"],Is.Null);
        }
        [TestCase("starting")]
        [TestCase("pending")]
        public void StartupRecoveryWithoutTerminalSdkEvidenceRetainsUnresolvedTruth(string state)
        {
            journal["state"] = state; journal["requestUnresolved"] = true;
            var files = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageOperationFiles");
            files.GetMethod("Publish",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[] {root,journal,null});
            StartupRecovery();
            Assert.That((string)Read()["state"],Is.EqualTo("interrupted"));
            Assert.That((bool)Read()["observerDetached"],Is.True); Assert.That((bool)Read()["requestUnresolved"],Is.True);
            Assert.That((bool)Read()["sdkRequestCancelled"],Is.False);
            Assert.That((string)Read()["detachReason"],Is.EqualTo("observer-unavailable-after-startup"));
            Assert.That(Read()["nativeResult"],Is.Null); Assert.That(Read()["nativeStatus"],Is.Null);
        }
        private void StartupRecovery()
        {
            var owner = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageEmbedObserver");
            var entry = owner.GetMethod("RecoverInterruptedJournalsForProject",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
            Assert.That(entry,Is.Not.Null,"Startup callback must share its canonical recovery owner.");
            try { entry.Invoke(null,new object[] {root}); }
            catch (TargetInvocationException exception) { throw exception.InnerException; }
        }
    }
}
