using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class PackageLifecycleRecoveryContractTests
    {
        private string root;
        private JObject journal;
        private JObject package;
        private const string Name = "com.example.lifecycle-fixture";
        [SetUp] public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "unity-package-lifecycle-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root,"Packages",Name));
            Directory.CreateDirectory(Path.Combine(root,"Assets"));
            File.WriteAllText(Path.Combine(root,"Packages",Name,"package.json"),"{\"name\":\""+Name+"\",\"version\":\"1.0.0\"}",new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root,"Packages",Name,"keep.txt"),"captured-source\n",new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root,"Packages",Name,"keep.txt.meta"),"fileFormatVersion: 2\nguid: 1234567890abcdef1234567890abcdef\n",new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root,"Packages/manifest.json"),"{\"dependencies\":{\""+Name+"\":\"1.0.0\"}}",new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root,"Packages/packages-lock.json"),"{\"dependencies\":{\""+Name+"\":{\"version\":\"1.0.0\",\"depth\":0,\"source\":\"registry\",\"dependencies\":{},\"url\":\"https://packages.unity.com\"}}}",new UTF8Encoding(false));
            var sourceFiles = new JObject();
            foreach(var path in Directory.GetFiles(Path.Combine(root,"Packages",Name))) sourceFiles[Path.GetFileName(path)] = State(path,false);
            journal = new JObject { ["schema"]="unity.package.operation@1",["project"]=root,["kind"]="embed",["name"]=Name,["version"]="1.0.0",["operationId"]=Guid.NewGuid().ToString("N"),["state"]="interrupted",["requestUnresolved"]=true,["observerDetached"]=true,["editorPid"]=int.MaxValue,["nativeProcessStartTicks"]="1",
                ["destinationBefore"]=new JObject { ["files"]=new JObject(),["directories"]=new JArray() },["sourceInventory"]=new JObject { ["files"]=sourceFiles,["directories"]=new JArray(".") },["manifestBefore"]=State(Path.Combine(root,"Packages/manifest.json")),["lockBefore"]=State(Path.Combine(root,"Packages/packages-lock.json")) };
            File.WriteAllText(Path.Combine(root,"Packages/packages-lock.json"),"{\"dependencies\":{\""+Name+"\":{\"version\":\"file:"+Name+"\",\"depth\":0,\"source\":\"embedded\",\"dependencies\":{}}}}",new UTF8Encoding(false));
            package=new JObject { ["name"]=Name,["version"]="1.0.0",["source"]="Embedded",["resolvedPath"]=Path.Combine(root,"Packages",Name) };
        }
        [TearDown] public void TearDown() { if(Directory.Exists(root)) Directory.Delete(root,true); }
        private static JObject State(string path,bool bytes=true)
        {
            var content=File.ReadAllBytes(path);string hash;using(var algorithm=System.Security.Cryptography.SHA256.Create()) hash=BitConverter.ToString(algorithm.ComputeHash(content)).Replace("-","").ToLowerInvariant();
            var state=new JObject { ["exists"]=true,["sha256"]=hash,["length"]=content.Length };if(bytes) state["bytesBase64"]=Convert.ToBase64String(content);return state;
        }
        private static MethodInfo Planner()
        {
            var owner=typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageAuthoringCommands");
            Assert.That(owner,Is.Not.Null,"Missing Package authoring owner");
            var method=owner.GetMethod("PreviewLifecycleInverse",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
            Assert.That(method,Is.Not.Null,"Missing lifecycle-bound observed-state inverse planner");return method;
        }
        private JObject Preview()
        {
            var method=Planner();try { return (JObject)method.Invoke(null,new object[] {root,journal,package}); } catch(TargetInvocationException exception) { throw exception.InnerException; }
        }
        private void Refuses(string code)
        {
            Planner();var exception=Assert.Catch<Exception>(()=>Preview());Assert.That(exception.GetType().GetProperty("Code"),Is.Not.Null,exception.ToString());Assert.That((string)exception.GetType().GetProperty("Code").GetValue(exception),Is.EqualTo(code));
        }
        [Test] public void LifecycleInversePreviewPreservesUnresolvedSdkTruthAndCompleteInventory()
        {
            var before=journal.ToString();var plan=Preview();Assert.That((bool)plan["observedStateInverse"],Is.True);Assert.That((string)plan["planSha256"],Does.Match("^[a-f0-9]{64}$"));Assert.That(plan["destinationAfter"]["files"].Count(),Is.EqualTo(3));Assert.That(journal.ToString(),Is.EqualTo(before));Assert.That((bool)journal["requestUnresolved"],Is.True);Assert.That(journal["nativeStatus"],Is.Null);
        }
        [Test] public void OriginalExactEditorStillAliveCannotAuthorizeLifecycleInverse()
        {
            using(var process=Process.GetCurrentProcess()) { journal["editorPid"]=process.Id;journal["nativeProcessStartTicks"]=process.StartTime.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture); }
            Refuses("LIFECYCLE_BARRIER_REQUIRED");Assert.That(File.ReadAllText(Path.Combine(root,"Packages",Name,"keep.txt")),Is.EqualTo("captured-source\n"));
        }
        [Test] public void ExternalEmbeddedBytesCannotBeAuthorizedByLifecycleBarrier()
        {
            File.AppendAllText(Path.Combine(root,"Packages",Name,"keep.txt"),"external\n");Refuses("RECOVERY_SOURCE_MISMATCH");Assert.That(File.ReadAllText(Path.Combine(root,"Packages",Name,"keep.txt")),Does.Contain("external"));
        }
        [TestCase("metadata")]
        [TestCase("missing")]
        [TestCase("added")]
        [TestCase("directory")]
        public void CompleteEmbeddedSourceOwnershipRejectsForeignInventory(string mutation)
        {
            var destination=Path.Combine(root,"Packages",Name);
            if(mutation=="metadata") File.AppendAllText(Path.Combine(destination,"keep.txt.meta"),"external: true\n");
            else if(mutation=="missing") File.Delete(Path.Combine(destination,"keep.txt"));
            else if(mutation=="added") File.WriteAllText(Path.Combine(destination,"foreign.txt"),"foreign");
            else Directory.CreateDirectory(Path.Combine(destination,"foreign-empty"));
            var before=journal.ToString(); Refuses("RECOVERY_SOURCE_MISMATCH"); Assert.That(journal.ToString(),Is.EqualTo(before));
        }
        [Test] public void EmbeddedOwnershipRequiresOriginallyAbsentDestination()
        {
            journal["destinationBefore"]["directories"]=new JArray("Packages/"+Name);
            var before=journal.ToString(); Refuses("RECOVERY_OWNERSHIP_MISMATCH"); Assert.That(journal.ToString(),Is.EqualTo(before));
        }
        [Test] public void OriginalManifestByteChangeCannotBeAuthorized()
        {
            var path=Path.Combine(root,"Packages/manifest.json");File.AppendAllText(path,"\n");var before=File.ReadAllBytes(path);
            Refuses("RECOVERY_MANIFEST_CHANGED");Assert.That(File.ReadAllBytes(path),Is.EqualTo(before));
        }
        [Test] public void RegisteredEmbeddedPackageWithRegistryLockRefusesPrematureSeal()
        {
            File.WriteAllBytes(Path.Combine(root,"Packages/packages-lock.json"),Convert.FromBase64String((string)journal["lockBefore"]["bytesBase64"]));
            Refuses("RECOVERY_LOCK_CHANGED");
        }
        [Test] public void UnrelatedLockChangeCannotBeAuthorizedByLifecycleBarrier()
        {
            var path=Path.Combine(root,"Packages/packages-lock.json");var value=JObject.Parse(File.ReadAllText(path));value["dependencies"]["com.example.external"]=new JObject { ["version"]="9.0.0" };File.WriteAllText(path,value.ToString());var before=File.ReadAllBytes(path);Refuses("RECOVERY_LOCK_CHANGED");Assert.That(File.ReadAllBytes(path),Is.EqualTo(before));
        }
        private static Type AuthoringOwner => typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageAuthoringCommands");
        private static object Call(Type owner, string method, params object[] arguments)
        {
            var entry = owner.GetMethod(method, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(entry, Is.Not.Null, "The public command must share its canonical " + method + " owner.");
            try { return entry.Invoke(null, arguments); }
            catch (TargetInvocationException exception) { throw exception.InnerException; }
        }
        private string ReceiptPath => (string)Call(typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageOperationFiles"), "JournalPath", root, (string)journal["operationId"]);
        private static string Hash(byte[] bytes)
        {
            using (var algorithm = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        private void TerminalReceipt()
        {
            using (var process = Process.GetCurrentProcess())
            {
                journal["editorPid"] = process.Id;
                journal["nativeProcessStartTicks"] = process.StartTime.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            journal["state"] = "awaiting-readback"; journal["nativeCompleted"] = true;
            journal["nativeStatus"] = "Success"; journal["requestUnresolved"] = false;
            journal["acknowledged"] = true; journal["observerDetached"] = true;
            journal["sdkRequestCancelled"] = false; journal["nativeResult"] = package.DeepClone();
            journal["commandSha256"] = (string)Call(AuthoringOwner, "CommandHash");
            journal["destination"] = "Packages/" + Name;
            SaveReceipt();
        }
        private void SaveReceipt()
        {
            var owner = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageOperationFiles");
            Call(owner, "Publish", root, journal, File.Exists(ReceiptPath) ? Hash(File.ReadAllBytes(ReceiptPath)) : null);
        }
        private JObject SavedReceipt() => JObject.Parse(File.ReadAllText(ReceiptPath));
        private CommandResult<JObject> Readback(bool preview = true, string receiptHash = null, string planHash = null, int? pid = null, string ticks = null)
        {
            Func<string, string, JObject> registered = (name, version) =>
            {
                Assert.That(name, Is.EqualTo(Name)); Assert.That(version, Is.EqualTo("1.0.0"));
                return (JObject)package.DeepClone();
            };
            return (CommandResult<JObject>)Call(AuthoringOwner, "ReconcileForProject", root, registered,
                (string)journal["operationId"], receiptHash ?? Hash(File.ReadAllBytes(ReceiptPath)), "completed-readback",
                planHash, pid ?? (int)journal["editorPid"], ticks ?? (string)journal["nativeProcessStartTicks"], preview, !preview);
        }
        private void ReadbackRefuses(string code, string hash = null)
        {
            var before = File.ReadAllBytes(ReceiptPath);
            var result = Readback(receiptHash: hash);
            Assert.That(result.Ok, Is.False); Assert.That(result.Error.Code, Is.EqualTo(code), result.Error.Message);
            Assert.That(File.ReadAllBytes(ReceiptPath), Is.EqualTo(before), "Refusal must preserve the exact receipt bytes.");
        }
        private JObject Seal()
        {
            var before = File.ReadAllBytes(ReceiptPath); var preview = Readback();
            Assert.That(preview.Ok, Is.True, preview.Error?.Code + ": " + preview.Error?.Message);
            Assert.That((string)preview.Result["mode"], Is.EqualTo("completed-readback"));
            Assert.That(File.ReadAllBytes(ReceiptPath), Is.EqualTo(before));
            var result = Readback(false, planHash: (string)preview.Result["planSha256"]);
            Assert.That(result.Ok, Is.True, result.Error?.Code + ": " + result.Error?.Message);
            Assert.That((bool)result.Result["completed"], Is.True);
            Assert.That((string)result.Result["journalSha256"], Is.EqualTo(Hash(File.ReadAllBytes(ReceiptPath))));
            return SavedReceipt();
        }
        [Test] public void CompletedReadbackPublishesSourceProvedSealAcceptedByRecoveryPreview()
        {
            TerminalReceipt(); var before = (JObject)journal.DeepClone(); var receipt = Seal();
            Assert.That((string)receipt["state"], Is.EqualTo("completed"));
            foreach (var field in new[] { "nativeCompleted", "nativeStatus", "nativeResult", "requestUnresolved", "acknowledged", "observerDetached", "sdkRequestCancelled", "sourceInventory", "manifestBefore", "lockBefore", "destinationBefore", "editorPid", "nativeProcessStartTicks" })
                Assert.That(JToken.DeepEquals(receipt[field], before[field]), Is.True, field + " must preserve observed SDK truth and before-evidence.");
            var files = (JObject)receipt["destinationAfter"]["files"];
            Assert.That(files.Properties().Select(item => item.Name), Is.EquivalentTo(new[] { "Packages/"+Name+"/package.json", "Packages/"+Name+"/keep.txt", "Packages/"+Name+"/keep.txt.meta" }));
            Assert.That((string)receipt["completedReadbackProof"]["mode"], Is.EqualTo("completed-readback"));
            var saved = File.ReadAllBytes(ReceiptPath);
            Call(AuthoringOwner, "RecoverEmbeddedForPackage", root, receipt, package, true, (int)journal["editorPid"], (string)journal["nativeProcessStartTicks"]);
            Assert.That(File.ReadAllBytes(ReceiptPath), Is.EqualTo(saved));
            Assert.That(File.ReadAllText(Path.Combine(root,"Packages",Name,"keep.txt")), Is.EqualTo("captured-source\n"));
        }
        [Test] public void CompletedReadbackExactCompletedSealIsByteIdempotent()
        {
            TerminalReceipt(); Seal(); var before = File.ReadAllBytes(ReceiptPath);
            var preview = Readback(); Assert.That(preview.Ok, Is.True, preview.Error?.Message);
            var result = Readback(false, planHash: (string)preview.Result["planSha256"]);
            Assert.That(result.Ok, Is.True, result.Error?.Message); Assert.That((bool)result.Result["idempotent"], Is.True);
            Assert.That((string)result.Result["journalSha256"], Is.EqualTo(Hash(before)));
            Assert.That(File.ReadAllBytes(ReceiptPath), Is.EqualTo(before));
        }
        [TestCase("file", "RECOVERY_SOURCE_MISMATCH")]
        [TestCase("metadata", "RECOVERY_SOURCE_MISMATCH")]
        [TestCase("missing", "RECOVERY_SOURCE_MISMATCH")]
        [TestCase("added", "RECOVERY_SOURCE_MISMATCH")]
        [TestCase("directory", "RECOVERY_SOURCE_MISMATCH")]
        [TestCase("directory-missing", "RECOVERY_SOURCE_MISMATCH")]
        [TestCase("metadata-missing", "RECOVERY_SOURCE_MISMATCH")]
        [TestCase("metadata-added", "RECOVERY_SOURCE_MISMATCH")]
        [TestCase("manifest", "RECOVERY_MANIFEST_CHANGED")]
        [TestCase("unrelated-lock", "RECOVERY_LOCK_CHANGED")]
        [TestCase("selected-dependencies", "RECOVERY_LOCK_CHANGED")]
        [TestCase("registry-lock", "RECOVERY_LOCK_CHANGED")]
        public void CompletedReadbackRefusesForeignStateWithoutMutatingReceipt(string mutation, string code)
        {
            PrepareOwnedDirectory(mutation); TerminalReceipt(); MutateReadbackState(mutation); ReadbackRefuses(code);
        }
        [TestCase("file")]
        [TestCase("metadata")]
        [TestCase("missing")]
        [TestCase("added")]
        [TestCase("directory")]
        [TestCase("directory-missing")]
        [TestCase("metadata-missing")]
        [TestCase("metadata-added")]
        [TestCase("manifest")]
        [TestCase("unrelated-lock")]
        public void CompletedReadbackNeverResealsExternalChanges(string mutation)
        {
            PrepareOwnedDirectory(mutation); TerminalReceipt(); Seal(); MutateReadbackState(mutation);
            var before = File.ReadAllBytes(ReceiptPath); var result = Readback();
            Assert.That(result.Ok, Is.False); Assert.That(result.Error.Code, Does.StartWith("RECOVERY_"));
            Assert.That(File.ReadAllBytes(ReceiptPath), Is.EqualTo(before));
        }
        private void PrepareOwnedDirectory(string mutation)
        {
            if (mutation != "directory-missing") return;
            Directory.CreateDirectory(Path.Combine(root,"Packages",Name,"retained-empty"));
            journal["sourceInventory"]["directories"] = new JArray(".","retained-empty");
        }
        private void MutateReadbackState(string mutation)
        {
            var destination = Path.Combine(root,"Packages",Name);
            if (mutation == "file") File.AppendAllText(Path.Combine(destination,"keep.txt"),"external\n");
            else if (mutation == "metadata") File.AppendAllText(Path.Combine(destination,"keep.txt.meta"),"external: true\n");
            else if (mutation == "metadata-missing") File.Delete(Path.Combine(destination,"keep.txt.meta"));
            else if (mutation == "metadata-added") File.WriteAllText(Path.Combine(destination,"foreign.txt.meta"),"fileFormatVersion: 2\n");
            else if (mutation == "directory-missing") Directory.Delete(Path.Combine(destination,"retained-empty"));
            else if (mutation == "missing") File.Delete(Path.Combine(destination,"keep.txt"));
            else if (mutation == "added") File.WriteAllText(Path.Combine(destination,"foreign.txt"),"foreign");
            else if (mutation == "directory") Directory.CreateDirectory(Path.Combine(destination,"foreign-empty"));
            else if (mutation == "manifest") File.AppendAllText(Path.Combine(root,"Packages/manifest.json"),"\n");
            else if (mutation == "registry-lock") File.WriteAllBytes(Path.Combine(root,"Packages/packages-lock.json"),Convert.FromBase64String((string)journal["lockBefore"]["bytesBase64"]));
            else
            {
                var path = Path.Combine(root,"Packages/packages-lock.json"); var value = JObject.Parse(File.ReadAllText(path));
                if (mutation == "unrelated-lock") value["dependencies"]["com.example.external"] = new JObject { ["version"]="9.0.0" };
                else value["dependencies"][Name]["dependencies"] = new JObject { ["com.example.external"]="9.0.0" };
                File.WriteAllText(path,value.ToString());
            }
        }
        [Test] public void CompletedReadbackStaleReceiptHashRefusesWithoutPublication()
        { TerminalReceipt(); ReadbackRefuses("JOURNAL_HASH_MISMATCH", new string('0',64)); }
        [Test] public void CompletedReadbackCommandSourceDriftRefusesWithoutPublication()
        { TerminalReceipt(); journal["commandSha256"] = new string('0',64); SaveReceipt(); ReadbackRefuses("JOURNAL_IDENTITY_MISMATCH"); }
        [TestCase("pid")]
        [TestCase("ticks")]
        public void CompletedReadbackOriginalNativeIdentityCannotBeReplaced(string field)
        {
            TerminalReceipt(); if (field == "pid") journal["editorPid"] = int.MaxValue; else journal["nativeProcessStartTicks"] = "1";
            SaveReceipt(); ReadbackRefuses("EDITOR_IDENTITY_MISMATCH");
        }
        [Test] public void CompletedReadbackConfirmationBindsCurrentNativeIdentity()
        {
            TerminalReceipt(); var preview = Readback(); Assert.That(preview.Ok, Is.True, preview.Error?.Message);
            var before = File.ReadAllBytes(ReceiptPath);
            var result = Readback(false, planHash: (string)preview.Result["planSha256"], pid: int.MaxValue);
            Assert.That(result.Ok, Is.False); Assert.That(result.Error.Code, Is.EqualTo("EDITOR_IDENTITY_MISMATCH"));
            Assert.That(File.ReadAllBytes(ReceiptPath), Is.EqualTo(before));
        }
        [Test] public void CompletedReadbackStalePlanRefusesWithoutPublication()
        {
            TerminalReceipt(); var before = File.ReadAllBytes(ReceiptPath);
            var result = Readback(false, planHash: new string('0',64));
            Assert.That(result.Ok, Is.False); Assert.That(result.Error.Code, Is.EqualTo("PLAN_HASH_MISMATCH"));
            Assert.That(File.ReadAllBytes(ReceiptPath), Is.EqualTo(before));
        }
        [TestCase("name")]
        [TestCase("version")]
        [TestCase("path")]
        [TestCase("source")]
        public void CompletedReadbackRequiresExactRegisteredEmbeddedIdentity(string field)
        {
            TerminalReceipt();
            if (field == "name") package["name"] = "com.example.foreign";
            else if (field == "version") package["version"] = "9.0.0";
            else if (field == "path") package["resolvedPath"] = Path.Combine(root,"Library/PackageCache",Name);
            else package["source"] = "Registry";
            ReadbackRefuses("RECOVERY_PACKAGE_MISMATCH");
        }
        [TestCase("nativeCompleted")]
        [TestCase("nativeStatus")]
        [TestCase("requestUnresolved")]
        [TestCase("acknowledged")]
        [TestCase("observerDetached")]
        [TestCase("nativeResult")]
        public void CompletedReadbackRequiresPersistedExactSdkTerminalEvidence(string field)
        {
            TerminalReceipt();
            if (field == "nativeStatus") journal[field] = "InProgress";
            else if (field == "nativeResult") journal[field]["resolvedPath"] = Path.Combine(root,"Library/PackageCache",Name);
            else journal[field] = field == "requestUnresolved";
            SaveReceipt(); ReadbackRefuses("EMBED_NOT_TERMINAL");
        }
        [TestCase("pending", true)]
        [TestCase("awaiting-readback", false)]
        public void CompletedReadbackUnrelatedUnfinishedJournalBlocksSealing(string state, bool unresolved)
        {
            TerminalReceipt(); var other = (JObject)journal.DeepClone(); other["operationId"] = Guid.NewGuid().ToString("N");
            other["state"] = state; other["requestUnresolved"] = unresolved;
            Call(typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageOperationFiles"),"Publish",root,other,null);
            ReadbackRefuses("PACKAGE_REQUEST_UNRESOLVED");
        }
        [TestCase(true,false,false)]
        [TestCase(false,true,false)]
        [TestCase(false,false,true)]
        public void CompletedReadbackEditorReadinessOwnerRefusesNonIdleState(bool playing, bool compiling, bool updating)
        {
            TerminalReceipt(); var before = File.ReadAllBytes(ReceiptPath);
            var error = Assert.Catch<Exception>(() => Call(AuthoringOwner,"RequireStoppedState",playing,compiling,updating));
            Assert.That((string)error.GetType().GetProperty("Code").GetValue(error),Is.EqualTo("EDITOR_NOT_READY"));
            Assert.That(File.ReadAllBytes(ReceiptPath),Is.EqualTo(before));
        }
        [Test] public void CompletedReadbackRequiresAbsentBeforeDestinationWithoutPublication()
        {
            TerminalReceipt(); journal["destinationBefore"]["directories"] = new JArray("Packages/"+Name);
            SaveReceipt(); ReadbackRefuses("RECOVERY_OWNERSHIP_MISMATCH");
        }
        [Test] public void CompletedReadbackWaitsForRegisteredEmbeddedAndCanonicalLockBeforeSealing()
        {
            TerminalReceipt(); var finalLock = File.ReadAllBytes(Path.Combine(root,"Packages/packages-lock.json"));
            package["source"] = "Registry";
            File.WriteAllBytes(Path.Combine(root,"Packages/packages-lock.json"),Convert.FromBase64String((string)journal["lockBefore"]["bytesBase64"]));
            ReadbackRefuses("RECOVERY_PACKAGE_MISMATCH");
            package["source"] = "Embedded"; ReadbackRefuses("RECOVERY_LOCK_CHANGED");
            Assert.That(SavedReceipt()["destinationAfter"],Is.Null);
            File.WriteAllBytes(Path.Combine(root,"Packages/packages-lock.json"),finalLock);
            Seal();
        }
        [Test] public void CompletedReadbackKnownStartupTerminalReceiptFinishesWithoutRequestHandle()
        {
            TerminalReceipt(); var before = File.ReadAllBytes(ReceiptPath);
            var owner = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageEmbedObserver");
            Call(owner,"RecoverInterruptedJournalsForProject",root);
            Assert.That(File.ReadAllBytes(ReceiptPath),Is.EqualTo(before));
            Assert.That((bool)SavedReceipt()["requestUnresolved"],Is.False);
            Seal();
        }
        [Test] public void CompletedReadbackActiveObserverRefusesWithoutPublication()
        {
            var owner = typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageEmbedObserver");
            var pending = new JObject { ["schema"]="unity.package.operation@1", ["project"]=root, ["operationId"]=Guid.NewGuid().ToString("N"),
                ["kind"]="embed", ["state"]="pending", ["acknowledged"]=true, ["observerDetached"]=false, ["requestUnresolved"]=true };
            Func<JObject> sdk = () => new JObject { ["completed"]=false,["status"]="InProgress" };
            var observer = Call(owner,"StartForRequest",root,pending,sdk,30.0);
            try { TerminalReceipt(); ReadbackRefuses("EMBED_ACTIVE"); }
            finally
            {
                try { owner.GetMethod("Interrupt",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(observer,new object[] {"unit-fixture-cleanup"}); }
                catch(TargetInvocationException exception) { throw exception.InnerException; }
            }
        }
        [TestCase("destinationAfter")]
        [TestCase("manifestAfter")]
        [TestCase("lockAfter")]
        public void CompletedReadbackCompletedSnapshotMismatchRefusesResealing(string field)
        {
            TerminalReceipt(); journal = Seal();
            if (field == "destinationAfter") journal[field]["files"]["Packages/"+Name+"/keep.txt"]["sha256"] = new string('0',64);
            else journal[field]["sha256"] = new string('0',64);
            SaveReceipt(); ReadbackRefuses("RECOVERY_HASH_MISMATCH");
        }
    }
}
