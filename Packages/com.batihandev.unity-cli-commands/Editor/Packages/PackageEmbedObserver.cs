using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using UnityEditor;

namespace BatihanDev.UnityCliCommands.Packages
{
    [InitializeOnLoad]
    internal sealed class PackageEmbedObserver
    {
        private static PackageEmbedObserver active;
        private readonly string root;
        private readonly JObject journal;
        private readonly Func<JObject> snapshot;
        private readonly double deadline;
        private string journalHash;
        private bool detached;
        static PackageEmbedObserver() { EditorApplication.delayCall += RecoverInterruptedJournals; }
        private PackageEmbedObserver(string root, JObject journal, Func<JObject> snapshot, double timeout)
        { this.root = root; this.journal = journal; this.snapshot = snapshot; deadline = EditorApplication.timeSinceStartup + timeout; }
        internal static void RequireIdle(string root = null)
            => CheckIdle(root, null);
        internal static void RequireIdleForRecovery(string root, JObject journal)
            => CheckIdle(root, null, journal);
        internal static void RequireIdleForReadback(string root, JObject journal)
            => CheckIdle(root, null, journal, true);
        private static void CheckIdle(string root, JObject attaching, JObject recovering = null, bool readback = false)
        {
            if (active != null && !active.detached) throw new PackageOperationException("EMBED_ACTIVE", "An owned Embed observer is active; inspect its exact journal before starting another operation.");
            root = root ?? Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(root)) throw new PackageOperationException("PACKAGE_PROJECT_UNAVAILABLE", "The exact project root is unavailable.");
            var directory = PackageOperationFiles.JournalDirectory(root);
            if (!Directory.Exists(directory)) return;
            PackageOperationFiles.JournalPath(root, new string('0', 32));
            foreach (var path in Directory.GetFiles(directory, "*.json"))
            {
                var id = Path.GetFileNameWithoutExtension(path);
                if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^[a-f0-9]{32}$")) continue;
                PackageOperationFiles.JournalPath(root, id);
                JObject recorded;
                try { recorded = JObject.Parse(File.ReadAllText(path)); }
                catch (Exception exception) { throw new PackageOperationException("PACKAGE_JOURNAL_INVALID", "An owned package journal cannot be read: " + exception.GetBaseException().Message); }
                if ((string)recorded["schema"] != PackageOperationFiles.JournalSchema || (string)recorded["operationId"] != id ||
                    !string.Equals(Path.GetFullPath((string)recorded["project"] ?? ""), Path.GetFullPath(root), ProjectPathPolicy.PathComparison()))
                    throw new PackageOperationException("PACKAGE_JOURNAL_INVALID", "An owned package journal does not identify its exact operation and project.");
                if ((string)recorded["kind"] != "embed") continue;
                if ((string)recorded["state"] == "restored" && PackageAuthoringCommands.HasLifecycleInverseProof(recorded)) continue;
                if (recovering != null && id == (string)recovering["operationId"] && PackageOperationFiles.HashJson(recorded) == PackageOperationFiles.HashJson(recovering) && PackageAuthoringCommands.HasLifecycleInverseProof(recorded)) continue;
                if (attaching != null && id == (string)attaching["operationId"] &&
                    PackageOperationFiles.HashJson(recorded) == PackageOperationFiles.HashJson(attaching) &&
                    (string)recorded["state"] == "pending" && (bool?)recorded["acknowledged"] == true && (bool?)recorded["observerDetached"] == false) continue;
                if (readback && recovering != null && id == (string)recovering["operationId"] && PackageOperationFiles.HashJson(recorded) == PackageOperationFiles.HashJson(recovering)) continue;
                if ((bool?)recorded["requestUnresolved"] == true || new[] { "starting", "pending", "awaiting-readback" }.Contains((string)recorded["state"]))
                    throw new PackageOperationException("PACKAGE_REQUEST_UNRESOLVED", "An owned Embed SDK request remains unresolved after observer detachment; retain its journal until actual terminal evidence permits reconciliation.");
            }
        }
        internal static PackageEmbedObserver StartForRequest(string root, JObject journal, Func<JObject> snapshot, double timeout)
        {
            CheckIdle(root, journal);
            if (timeout <= 0 || double.IsNaN(timeout) || double.IsInfinity(timeout)) throw new PackageOperationException("INVALID_TIMEOUT", "Select a finite positive Embed observer timeout.");
            var observer = new PackageEmbedObserver(root, journal, snapshot, timeout);
            try
            {
                var path = PackageOperationFiles.JournalPath(root, (string)journal["operationId"]);
                observer.journalHash = PackageOperationFiles.Publish(root, journal, File.Exists(path) ? PackageOperationFiles.HashJson(journal) : null);
                active = observer;
                EditorApplication.update += observer.Poll;
                AssemblyReloadEvents.beforeAssemblyReload += observer.Reload;
                EditorApplication.quitting += observer.Quit;
                return observer;
            }
            catch
            {
                observer.Detach("startup-exception");
                throw;
            }
        }
        internal void Poll()
        {
            if (detached) return;
            try
            {
                var native = snapshot();
                journal["nativeStatus"] = native["status"]?.DeepClone();
                journal["nativeCompleted"] = native["completed"]?.DeepClone();
                if ((bool?)native["completed"] == true)
                {
                    journal["nativeResult"] = native["result"]?.DeepClone();
                    journal["nativeError"] = native["error"]?.DeepClone();
                    journal["requestUnresolved"] = false;
                    journal["state"] = (string)native["status"] == "Success" ? "awaiting-readback" : "failed";
                    if ((string)native["status"] != "Success") CaptureAfter();
                    Finish("native-terminal");
                }
                else if (EditorApplication.timeSinceStartup >= deadline)
                { journal["state"] = "timed-out"; journal["requestUnresolved"] = true; CaptureAfter(); Finish("deadline"); }
                else journalHash = PackageOperationFiles.Publish(root, journal, journalHash);
            }
            catch (Exception exception)
            {
                journal["state"] = "observer-error"; journal["observerError"] = exception.GetBaseException().Message;
                journal["requestUnresolved"] = (bool?)journal["nativeCompleted"] != true;
                Detach("observer-exception");
                try { journalHash = PackageOperationFiles.Publish(root, journal, journalHash); }
                catch (Exception publication) { UnityEngine.Debug.LogError("Package observer detached; journal publication failed: " + publication.GetBaseException().Message); }
            }
        }
        internal void Interrupt(string reason)
        {
            if (detached) return;
            journal["state"] = "interrupted"; journal["requestUnresolved"] = true;
            try { CaptureAfter(); }
            finally { Finish(reason); }
        }
        internal static bool DetachOperation(string id, string reason)
        {
            if (active == null || (string)active.journal["operationId"] != id) return false;
            active.Interrupt(reason); return true;
        }
        internal static JObject ReconcileForRequest(string root, string id, string expectedHash)
        {
            var observer = active;
            if (observer == null || !observer.detached || (string)observer.journal["operationId"] != id ||
                !string.Equals(Path.GetFullPath(observer.root), Path.GetFullPath(root), ProjectPathPolicy.PathComparison()))
                throw new PackageOperationException("REQUEST_HANDLE_UNAVAILABLE", "The original same-process SDK request is unavailable; use the explicit lifecycle inverse barrier.");
            var bytes = File.ReadAllBytes(PackageOperationFiles.JournalPath(root, id));
            if (PackageOperationFiles.Hash(bytes) != expectedHash || observer.journalHash != expectedHash)
                throw new PackageOperationException("JOURNAL_HASH_MISMATCH", "Bind reconciliation to the exact current saved request receipt.");
            var native = observer.snapshot();
            if ((bool?)native["completed"] != true)
                throw new PackageOperationException("REQUEST_UNRESOLVED", "The actual SDK request is still pending; detachment did not cancel it.");
            var status = (string)native["status"];
            if (status != "Success" && status != "Failure")
                throw new PackageOperationException("REQUEST_TERMINAL_INVALID", "The actual completed SDK request has no recognized terminal status.");
            if (observer.journal["detachedEvidence"] == null) observer.journal["detachedEvidence"] = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            observer.journal["nativeCompleted"] = true; observer.journal["nativeStatus"] = status;
            observer.journal["nativeResult"] = native["result"]?.DeepClone(); observer.journal["nativeError"] = native["error"]?.DeepClone();
            observer.journal["requestUnresolved"] = false; observer.journal["state"] = status == "Success" ? "awaiting-readback" : "failed";
            observer.journal["reconciledUtc"] = DateTime.UtcNow.ToString("o");
            if (status != "Success") observer.CaptureAfter(); observer.Finish("sdk-terminal-after-detach");
            return new JObject { ["reconciled"] = true, ["operationId"] = id, ["nativeStatus"] = status, ["journalSha256"] = observer.journalHash };
        }
        private void CaptureAfter()
        {
            if (journal["destination"] == null) return;
            var name = (string)journal["name"];
            journal["destinationAfter"] = PackageAuthoringCommands.EmbeddedInventory(root, name);
            journal["manifestAfter"] = PackageOperationFiles.FileState(root, "Packages/manifest.json");
            journal["lockAfter"] = PackageOperationFiles.FileState(root, "Packages/packages-lock.json");
        }
        private void Finish(string reason)
        {
            Detach(reason);
            journal["finishedUtc"] = DateTime.UtcNow.ToString("o");
            journalHash = PackageOperationFiles.Publish(root, journal, journalHash);
        }
        private void Detach(string reason)
        {
            detached = true;
            EditorApplication.update -= Poll;
            AssemblyReloadEvents.beforeAssemblyReload -= Reload;
            EditorApplication.quitting -= Quit;
            if (active == this && (bool?)journal["requestUnresolved"] != true) active = null;
            journal["observerDetached"] = true; journal["sdkRequestCancelled"] = false; journal["detachReason"] = reason;
        }
        private void Reload() => Interrupt("assembly-reload");
        private void Quit() => Interrupt("editor-quitting");
        private static void RecoverInterruptedJournals()
            => RecoverInterruptedJournalsForProject(Directory.GetParent(UnityEngine.Application.dataPath)?.FullName);
        internal static void RecoverInterruptedJournalsForProject(string project)
        {
            if (string.IsNullOrEmpty(project)) return;
            var directory = PackageOperationFiles.JournalDirectory(project);
            if (!Directory.Exists(directory)) return;
            try
            {
                PackageOperationFiles.JournalPath(project, new string('0', 32));
                foreach (var path in Directory.GetFiles(directory, "*.json"))
                {
                    var id = Path.GetFileNameWithoutExtension(path);
                    PackageOperationFiles.JournalPath(project, id);
                    var bytes = File.ReadAllBytes(path); var journal = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
                    if ((string)journal["schema"] != PackageOperationFiles.JournalSchema || (string)journal["kind"] != "embed" ||
                        (string)journal["project"] != project || (string)journal["operationId"] != id ||
                        !new[] { "starting", "pending" }.Contains((string)journal["state"])) continue;
                    if (active != null && (string)active.journal["operationId"] == id) continue;
                    journal["state"] = "interrupted"; journal["observerDetached"] = true; journal["sdkRequestCancelled"] = false;
                    journal["requestUnresolved"] = true; journal["detachReason"] = "observer-unavailable-after-startup";
                    journal["finishedUtc"] = DateTime.UtcNow.ToString("o");
                    PackageOperationFiles.Publish(project, journal, PackageOperationFiles.Hash(bytes));
                }
            }
            catch (Exception exception) { UnityEngine.Debug.LogError("Package recovery journals require inspection: " + exception.GetBaseException().Message); }
        }
    }
}
