using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace BatihanDev.UnityCliCommands.Packages
{
    public static class PackageAuthoringCommands
    {
        private const string ImplementationPackage = "com.batihandev.unity-cli-commands";
        private const string Schema = "unity.package.authoring@1";
        [CliCommand("package_embed", "Preview or acknowledge one exact public UPM Embed request; completion requires its exact journal.", Tags = new[] { "unity-cli-commands", "package" })]
        public static CommandResult<JObject> Embed(string name, string version, string operationId,
            string expectedPlanSha256 = null, int expectedNativePid = 0, string expectedNativeProcessStartTicks = null,
            string editorStartedAt = null, bool dryRun = false, bool confirm = false, double timeoutSeconds = 120)
        {
            JObject journal = null; string journalHash = null;
            try
            {
                var root = Root(); var package = ExactPackage(name, version);
                GuardOperation(root, operationId);
                var destination = ProjectPathPolicy.ValidatePackageRoot(name, root);
                PackageOperationFiles.Require(destination);
                if (name == ImplementationPackage) throw new PackageOperationException("IMPLEMENTATION_PACKAGE_PROTECTED", "Choose a package other than the operation implementation.");
                if (package.source == PackageSource.Embedded || package.source == PackageSource.BuiltIn)
                    throw new PackageOperationException("PACKAGE_SOURCE_UNSUPPORTED", "Select an installed registry, Git, or local package that is not embedded.");
                if (destination.Result.Exists)
                    throw new PackageOperationException("EMBED_DESTINATION_EXISTS", "The exact embedded package destination must be absent.");
                var plan = new JObject { ["name"] = name, ["version"] = version, ["originalPackage"] = PackageData(package),
                    ["sourceInventory"] = SourceInventory(package), ["manifestBefore"] = ManifestState(root, "manifest.json"),
                    ["lockBefore"] = ManifestState(root, "packages-lock.json"), ["destination"] = "Packages/" + name,
                    ["destinationBefore"] = EmbeddedInventory(root, name), ["commandSha256"] = CommandHash() };
                plan["planSha256"] = PackageOperationFiles.HashJson(plan);
                var preview = Preview(plan, operationId, dryRun);
                if (dryRun) return CommandResult<JObject>.Success(Schema, preview);
                if (!confirm) throw new PackageOperationException("CONFIRMATION_REQUIRED", "Confirm the exact reviewed Embed plan hash.");
                BindPlan(plan, expectedPlanSha256); BindIdentity(expectedNativePid, expectedNativeProcessStartTicks);
                RequireStopped(); PackageEmbedObserver.RequireIdle(root);
                if (double.IsNaN(timeoutSeconds) || double.IsInfinity(timeoutSeconds) || timeoutSeconds <= 0)
                    throw new PackageOperationException("INVALID_TIMEOUT", "Select a finite positive observer timeout.");
                journal = Journal(root, operationId, "embed", name, version, editorStartedAt);
                foreach (var property in plan.Properties()) journal[property.Name] = property.Value.DeepClone();
                journalHash = PackageOperationFiles.Publish(root, journal);
                // Revalidate identity, destination, source, manifest and lock at the SDK mutation boundary.
                BindIdentity(expectedNativePid, expectedNativeProcessStartTicks);
                destination = ProjectPathPolicy.ValidatePackageRoot(name, root);
                PackageOperationFiles.Require(destination);
                if (destination.Result.Exists) throw new PackageOperationException("EMBED_DESTINATION_EXISTS", "The Embed destination appeared after preview.");
                if (!PackageOperationFiles.SameInventory((JObject)plan["sourceInventory"], SourceInventory(ExactPackage(name, version))) ||
                    !JToken.DeepEquals(plan["manifestBefore"], ManifestState(root, "manifest.json")) ||
                    !JToken.DeepEquals(plan["lockBefore"], ManifestState(root, "packages-lock.json")))
                    throw new PackageOperationException("EMBED_PLAN_CHANGED", "The package source, manifest, or lock changed after preview.");
                var request = Client.Embed(name);
                if (request == null) throw new PackageOperationException("EMBED_START_FAILED", "UPM returned no Embed request.");
                journal["acknowledged"] = true; journal["state"] = "pending"; journal["requestUnresolved"] = true;
                journalHash = PackageOperationFiles.Publish(root, journal, journalHash);
                PackageEmbedObserver.StartForRequest(root, journal, () =>
                {
                    var native = new JObject { ["completed"] = request.IsCompleted, ["status"] = request.Status.ToString() };
                    if (request.IsCompleted)
                    {
                        if (request.Error != null) native["error"] = new JObject { ["code"] = (int)request.Error.errorCode, ["message"] = request.Error.message };
                        if (request.Status == StatusCode.Success && request.Result != null) native["result"] = PackageData(request.Result);
                    }
                    return native;
                }, timeoutSeconds);
                preview["dryRun"] = false; preview["applied"] = true; preview["acknowledged"] = true;
                preview["journal"] = PackageOperationFiles.JournalRelativePath(operationId);
                preview["state"] = "pending";
                return CommandResult<JObject>.Success(Schema, preview);
            }
            catch (Exception exception)
            {
                if (journal != null)
                {
                    journal["state"] = "startup-error"; journal["observerDetached"] = true; journal["sdkRequestCancelled"] = false;
                    journal["requestUnresolved"] = (bool?)journal["acknowledged"] == true; journal["observerError"] = exception.GetBaseException().Message;
                    try { PackageOperationFiles.Publish((string)journal["project"], journal, journalHash); } catch { }
                }
                return Failure(exception);
            }
        }

        [CliCommand("package_sample", "Preview or import one exact public UPM sample with confined path/GUID inventory and hash-bound recovery.", Tags = new[] { "unity-cli-commands", "package", "asset" })]
        public static CommandResult<JObject> ImportSample(string name, string version, string sampleName, string operationId,
            string expectedPlanSha256 = null, int expectedNativePid = 0, string expectedNativeProcessStartTicks = null,
            string editorStartedAt = null, bool allowOverwrite = false, bool dryRun = false, bool confirm = false)
        {
            JObject journal = null; string journalHash = null;
            try
            {
                var root = Root(); var package = ExactPackage(name, version); GuardOperation(root, operationId);
                var matches = Sample.FindByPackage(name, version).Where(sample => sample.displayName == sampleName).ToArray();
                if (matches.Length == 0) throw new PackageOperationException("SAMPLE_NOT_FOUND", "No sample matches the exact installed package version and display name.");
                if (matches.Length != 1) throw new PackageOperationException("SAMPLE_AMBIGUOUS", "More than one sample has that exact display name; select an unambiguous package sample.");
                var selected = matches[0];
                PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(selected.resolvedPath, package.resolvedPath));
                var importPath = Path.GetRelativePath(root, selected.importPath).Replace('\\', '/');
                var plan = PackageSampleInventory.Prepare(root, selected.resolvedPath, package.resolvedPath, importPath, allowOverwrite);
                plan.Remove("planSha256"); plan["name"] = name; plan["version"] = version; plan["sampleName"] = sampleName;
                plan["originalPackage"] = PackageData(package); plan["commandSha256"] = CommandHash();
                plan["planSha256"] = PackageOperationFiles.HashJson(plan);
                var preview = Preview(plan, operationId, dryRun);
                if (dryRun) return CommandResult<JObject>.Success(Schema, preview);
                if (!confirm) throw new PackageOperationException("CONFIRMATION_REQUIRED", "Confirm the reviewed sample plan hash before importing.");
                BindPlan(plan, expectedPlanSha256); BindIdentity(expectedNativePid, expectedNativeProcessStartTicks);
                RequireStopped(); PackageEmbedObserver.RequireIdle(root);
                journal = Journal(root, operationId, "sample", name, version, editorStartedAt);
                journal["samplePlan"] = plan; journal["sampleName"] = sampleName;
                journalHash = PackageOperationFiles.Publish(root, journal);
                BindIdentity(expectedNativePid, expectedNativeProcessStartTicks);
                PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(selected.resolvedPath, ExactPackage(name, version).resolvedPath));
                PackageSampleInventory.ValidateCurrent(root, plan);
                // ImportPath is ignored by archives; the approved entries are the archive's actual targets.
                foreach (var entry in plan["entries"]) PackageSampleInventory.ValidateAsset(root, (string)entry["path"]);
                var options = Sample.ImportOptions.HideImportWindow;
                if (allowOverwrite) options |= Sample.ImportOptions.OverridePreviousImports;
                journal["importAcknowledged"] = selected.Import(options);
                journal["acknowledged"] = true; journal["state"] = "awaiting-readback";
                journal["destinationAfter"] = PackageSampleInventory.DestinationInventory(root, plan);
                PackageSampleInventory.ValidateCompletionInventory(root, plan, (JObject)plan["before"], (JObject)journal["destinationAfter"], true);
                journalHash = PackageOperationFiles.Publish(root, journal, journalHash);
                if ((bool)journal["importAcknowledged"] == false) throw new PackageOperationException("SAMPLE_IMPORT_FAILED", "The public Sample.Import call returned false; inspect the captured inverse before retrying.");
                preview["acknowledged"] = true; preview["applied"] = false; preview["dryRun"] = false; preview["state"] = "awaiting-readback";
                preview["importAcknowledged"] = true; preview["journal"] = PackageOperationFiles.JournalRelativePath(operationId);
                return CommandResult<JObject>.Success(Schema, preview);
            }
            catch (Exception exception)
            {
                if (journal != null)
                {
                    try
                    {
                        journal["state"] = "sample-error"; journal["observerError"] = exception.GetBaseException().Message;
                        var observed = PackageSampleInventory.DestinationInventory((string)journal["project"], (JObject)journal["samplePlan"]);
                        PackageSampleInventory.ValidateCompletionInventory((string)journal["project"], (JObject)journal["samplePlan"], (JObject)journal["samplePlan"]["before"], observed, true);
                        journal["destinationAfter"] = observed;
                        PackageOperationFiles.Publish((string)journal["project"], journal, journalHash);
                    }
                    catch (Exception publication) { journal["recoveryPublicationError"] = publication.GetBaseException().Message; }
                }
                return Failure(exception, journal);
            }
        }

        [CliCommand("package_operation_recover", "Restore the exact captured Embed or sample state only while journal and destination hashes match.", Tags = new[] { "unity-cli-commands", "package" })]
        public static CommandResult<JObject> Recover(string operationId, string expectedJournalSha256,
            int expectedNativePid = 0, string expectedNativeProcessStartTicks = null, bool dryRun = false, bool confirm = false)
        {
            try
            {
                var root = Root(); var path = PackageOperationFiles.JournalPath(root, operationId);
                var bytes = File.ReadAllBytes(path); var hash = PackageOperationFiles.Hash(bytes);
                if (hash != expectedJournalSha256) throw new PackageOperationException("JOURNAL_HASH_MISMATCH", "The exact captured journal hash is required for recovery.");
                var journal = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
                if ((string)journal["schema"] != PackageOperationFiles.JournalSchema || (string)journal["project"] != root || (string)journal["operationId"] != operationId ||
                    (string)journal["commandSha256"] != CommandHash())
                    throw new PackageOperationException("JOURNAL_IDENTITY_MISMATCH", "The journal is not bound to this exact project and command source.");
                if ((string)journal["name"] == ImplementationPackage) throw new PackageOperationException("IMPLEMENTATION_PACKAGE_PROTECTED", "The operation implementation cannot be removed by recovery.");
                if ((bool?)journal["requestUnresolved"] == true && !HasLifecycleInverseProof(journal)) throw new PackageOperationException("REQUEST_UNRESOLVED", "Observer detachment did not cancel UPM; reconcile its actual SDK request or bind an explicit lifecycle inverse before restoring files.");
                PackageEmbedObserver.RequireIdleForRecovery(root, journal);
                if ((string)journal["kind"] == "sample")
                {
                    var package = ExactPackage((string)journal["name"], (string)journal["version"]);
                    if ((string)journal["state"] == "starting" || journal["destinationAfter"] == null)
                    {
                        var matches = Sample.FindByPackage(package.name, package.version).Where(sample => sample.displayName == (string)journal["sampleName"]).ToArray();
                        if (matches.Length != 1) throw new PackageOperationException("SAMPLE_BINDING_MISMATCH", "Recovery requires one exact SDK-selected package sample.");
                        return RecoverStartingSample(root, operationId, expectedJournalSha256, PackageData(package), matches[0].displayName,
                            matches[0].resolvedPath, Path.GetRelativePath(root, matches[0].importPath).Replace('\\', '/'), CommandHash(),
                            expectedNativePid, expectedNativeProcessStartTicks, dryRun, confirm);
                    }
                    var plan = (JObject)journal["samplePlan"];
                    if (!PackageOperationFiles.SameInventory((JObject)journal["destinationAfter"], PackageSampleInventory.DestinationInventory(root, plan)))
                        throw new PackageOperationException("RECOVERY_HASH_MISMATCH", "The destination inventory changed after import.");
                    if (!dryRun && confirm)
                    {
                        BindIdentity(expectedNativePid, expectedNativeProcessStartTicks); RequireStopped();
                        PackageSampleInventory.Restore(root, plan, (JObject)journal["destinationAfter"]);
                        foreach (var entry in ((JObject)plan["before"]["files"]).Properties().Where(item => !item.Name.EndsWith(".meta", StringComparison.Ordinal)))
                        { PackageSampleInventory.ValidateAsset(root, entry.Name); AssetDatabase.ImportAsset(entry.Name, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport); }
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        if (!PackageOperationFiles.SameInventory((JObject)plan["before"], PackageSampleInventory.DestinationInventory(root, plan)))
                            throw new PackageOperationException("RECOVERY_READBACK_MISMATCH", "Unity import altered the restored sample inventory.");
                    }
                }
                else if ((string)journal["kind"] == "embed")
                {
                    if ((bool?)journal["observedNoMutation"] == true)
                    {
                        var current = PreviewLifecycleInverse(root, journal, PackageData(ExactPackage((string)journal["name"], (string)journal["version"])));
                        if ((bool?)current["observedNoMutation"] != true || (string)current["planSha256"] != (string)journal["lifecycleInverseProof"]["planSha256"])
                            throw new PackageOperationException("RECOVERY_HASH_MISMATCH", "The complete original state changed after its observed no-mutation proof.");
                        if (!dryRun && confirm) { BindIdentity(expectedNativePid, expectedNativeProcessStartTicks); RequireStopped(); }
                    }
                    else RecoverEmbedded(root, journal, dryRun || !confirm, expectedNativePid, expectedNativeProcessStartTicks);
                }
                else throw new PackageOperationException("JOURNAL_KIND_INVALID", "The journal kind is unsupported.");
                if (dryRun) return CommandResult<JObject>.Success(Schema, new JObject { ["dryRun"] = true, ["restored"] = false, ["operationId"] = operationId, ["nativePid"] = Process.GetCurrentProcess().Id, ["nativeProcessStartTicks"] = StartTicks() });
                if (!confirm) throw new PackageOperationException("CONFIRMATION_REQUIRED", "Confirm hash-bound recovery before restoring project files.");
                journal["state"] = "restored"; journal["restoredUtc"] = DateTime.UtcNow.ToString("o");
                PackageOperationFiles.Publish(root, journal, hash);
                return CommandResult<JObject>.Success(Schema, new JObject { ["restored"] = true, ["operationId"] = operationId, ["requiresNativeResolve"] = (string)journal["kind"] == "embed" && (bool?)journal["observedNoMutation"] != true,
                    ["originalPackage"] = journal["originalPackage"]?.DeepClone() });
            }
            catch (Exception exception) { return Failure(exception); }
        }
        internal static CommandResult<JObject> RecoverStartingSample(string root, string operationId, string expectedHash,
            JObject package, string sampleName, string sampleRoot, string importPath, string commandHash,
            int expectedNativePid = 0, string expectedNativeProcessStartTicks = null, bool dryRun = false, bool confirm = false)
        {
            try
            {
                var path = PackageOperationFiles.JournalPath(root, operationId);
                var raw = File.ReadAllBytes(path); var hash = PackageOperationFiles.Hash(raw);
                if (hash != expectedHash) throw new PackageOperationException("JOURNAL_HASH_MISMATCH", "The exact captured journal hash is required for recovery.");
                JObject journal;
                try { journal = JObject.Parse(System.Text.Encoding.UTF8.GetString(raw)); }
                catch (Newtonsoft.Json.JsonException) { throw new PackageOperationException("PACKAGE_JOURNAL_INVALID", "The retained starting receipt must be a valid JSON object."); }
                ValidateStartingSample(root, journal, operationId, package, sampleName, sampleRoot, importPath, commandHash);
                RequireStopped(); PackageEmbedObserver.RequireIdleForRecovery(root, journal);
                var proof = new JObject { ["observedNoMutation"] = true, ["restored"] = false, ["operationId"] = operationId,
                    ["nativePid"] = Process.GetCurrentProcess().Id, ["nativeProcessStartTicks"] = StartTicks(), ["requiresNativeResolve"] = false };
                if (dryRun) { proof["dryRun"] = true; return CommandResult<JObject>.Success(Schema, proof); }
                if (!confirm) throw new PackageOperationException("CONFIRMATION_REQUIRED", "Confirm the exact no-mutation recovery receipt.");
                BindIdentity(expectedNativePid, expectedNativeProcessStartTicks);
                RequireStopped(); PackageEmbedObserver.RequireIdleForRecovery(root, journal);
                ValidateStartingSample(root, journal, operationId, package, sampleName, sampleRoot, importPath, commandHash);
                journal["state"] = "restored"; journal["observedNoMutation"] = true;
                journal["restoredUtc"] = DateTime.UtcNow.ToString("o");
                PackageOperationFiles.Publish(root, journal, hash);
                proof["restored"] = true; proof["originalPackage"] = package.DeepClone();
                return CommandResult<JObject>.Success(Schema, proof);
            }
            catch (Exception exception) { return Failure(exception); }
        }
        private static void ValidateStartingSample(string root, JObject journal, string operationId, JObject package,
            string sampleName, string sampleRoot, string importPath, string commandHash)
        {
            if (new[] { "schema", "project", "operationId", "commandSha256", "kind", "state", "name", "version", "sampleName" }.Any(field => journal[field]?.Type != JTokenType.String))
                throw new PackageOperationException("SAMPLE_STARTING_PROOF_INVALID", "The starting receipt requires complete scalar identity fields.");
            if ((string)journal["schema"] != PackageOperationFiles.JournalSchema || (string)journal["project"] != root ||
                (string)journal["operationId"] != operationId || (string)journal["commandSha256"] != commandHash)
                throw new PackageOperationException("JOURNAL_IDENTITY_MISMATCH", "The journal must bind the exact project, operation and current command source.");
            if ((string)journal["name"] == ImplementationPackage) throw new PackageOperationException("IMPLEMENTATION_PACKAGE_PROTECTED", "The implementation package is protected.");
            if ((string)journal["kind"] != "sample" || (string)journal["state"] != "starting" ||
                journal["acknowledged"]?.Type != JTokenType.Boolean || (bool)journal["acknowledged"] ||
                journal["importAcknowledged"] != null && (journal["importAcknowledged"].Type != JTokenType.Boolean || (bool)journal["importAcknowledged"]) ||
                journal["requestUnresolved"]?.Type != JTokenType.Boolean || (bool)journal["requestUnresolved"] ||
                journal["sdkRequestCancelled"]?.Type != JTokenType.Boolean || (bool)journal["sdkRequestCancelled"] ||
                new[] { "destinationAfter", "nativeStatus", "nativeResult", "readback", "finishedUtc", "restoredUtc", "observedNoMutation", "lifecycleInverseProof" }.Any(field => journal[field] != null))
                throw new PackageOperationException("SAMPLE_STARTING_PROOF_INVALID", "The receipt does not prove an unacknowledged starting sample without completion evidence.");
            RequireOriginalProcessAbsent(journal);
            if (journal["samplePlan"] is not JObject plan || plan["before"] is not JObject before ||
                before["files"] is not JObject || before["directories"] is not JArray || before["parentDirectories"] is not JArray ||
                plan["sourceInventory"] is not JObject || plan["entries"] is not JArray || plan["watched"] is not JArray)
                throw new PackageOperationException("SAMPLE_STARTING_PROOF_INVALID", "Recovery requires a complete retained source and destination inventory.");
            if (new[] { "source", "sampleRoot", "packageRoot", "importPath", "name", "version", "sampleName", "commandSha256", "planSha256" }.Any(field => plan[field]?.Type != JTokenType.String) ||
                plan["archive"]?.Type != JTokenType.Boolean || plan["allowOverwrite"]?.Type != JTokenType.Boolean ||
                plan["collisions"] is not JArray || ((JArray)plan["watched"]).Count == 0 || ((JArray)plan["entries"]).Count == 0 ||
                ((JArray)plan["watched"]).Any(item => item.Type != JTokenType.String) ||
                ((JArray)plan["entries"]).Any(item => item is not JObject || item["path"]?.Type != JTokenType.String))
                throw new PackageOperationException("SAMPLE_STARTING_PROOF_INVALID", "The retained sample plan requires its complete selected source and mutation footprint.");
            foreach (var inventory in new[] { before, (JObject)plan["sourceInventory"] })
            {
                if (inventory["files"] is not JObject files || inventory["directories"] is not JArray directories ||
                    directories.Any(item => item.Type != JTokenType.String) ||
                    files.Properties().Any(item => item.Value is not JObject || item.Value["exists"]?.Type != JTokenType.Boolean ||
                        (bool)item.Value["exists"] != true || item.Value["sha256"]?.Type != JTokenType.String || item.Value["length"]?.Type != JTokenType.Integer))
                    throw new PackageOperationException("SAMPLE_STARTING_PROOF_INVALID", "The retained inventory requires complete file and directory evidence.");
            }
            if (((JArray)before["parentDirectories"]).Any(item => item.Type != JTokenType.String))
                throw new PackageOperationException("SAMPLE_STARTING_PROOF_INVALID", "The retained destination requires complete parent-directory evidence.");
            PackageSampleInventory.ValidatePlanHash(plan);
            if (new[] { "name", "version", "sampleName", "commandSha256" }.Any(field => !JToken.DeepEquals(plan[field], journal[field])) ||
                (string)package["name"] != (string)journal["name"] || (string)package["version"] != (string)journal["version"] ||
                !JToken.DeepEquals(plan["originalPackage"], package) ||
                journal["originalPackage"] != null && !JToken.DeepEquals(journal["originalPackage"], package) ||
                (string)plan["packageRoot"] != Path.GetFullPath((string)package["resolvedPath"]) ||
                (string)plan["sampleName"] != sampleName || (string)plan["sampleRoot"] != Path.GetFullPath(sampleRoot) ||
                (string)plan["importPath"] != importPath)
                throw new PackageOperationException("SAMPLE_BINDING_MISMATCH", "The complete retained plan must match the current SDK package and selected sample.");
            PackageSampleInventory.ValidateCurrent(root, plan);
            var prepared = PackageSampleInventory.Prepare(root, sampleRoot, (string)package["resolvedPath"], importPath, (bool)plan["allowOverwrite"]);
            foreach (var field in new[] { "source", "archive", "entries", "watched", "collisions" })
                if (!JToken.DeepEquals(plan[field], prepared[field])) throw new PackageOperationException("SAMPLE_BINDING_MISMATCH", "The retained sample footprint differs from the current selected sample.");
        }
        [CliCommand("package_embed_reconcile", "Reconcile a detached SDK request, seal terminal Success after registered readback, or authorize a lifecycle inverse.", Tags = new[] { "unity-cli-commands", "package" })]
        public static CommandResult<JObject> Reconcile(string operationId, string expectedJournalSha256, string mode = "sdk-terminal",
            string expectedPlanSha256 = null, int expectedNativePid = 0, string expectedNativeProcessStartTicks = null,
            bool dryRun = false, bool confirm = false)
        {
            try
            {
                return ReconcileForProject(Root(), (name, version) => PackageData(ExactPackage(name, version)), operationId, expectedJournalSha256,
                    mode, expectedPlanSha256, expectedNativePid, expectedNativeProcessStartTicks, dryRun, confirm);
            }
            catch (Exception exception) { return Failure(exception); }
        }
        internal static CommandResult<JObject> ReconcileForProject(string root, Func<string, string, JObject> registeredPackage,
            string operationId, string expectedJournalSha256, string mode = "sdk-terminal", string expectedPlanSha256 = null,
            int expectedNativePid = 0, string expectedNativeProcessStartTicks = null, bool dryRun = false, bool confirm = false)
        {
            try
            {
                var path = PackageOperationFiles.JournalPath(root, operationId); var bytes = File.ReadAllBytes(path);
                var hash = PackageOperationFiles.Hash(bytes);
                if (hash != expectedJournalSha256) throw new PackageOperationException("JOURNAL_HASH_MISMATCH", "Bind reconciliation to the exact saved receipt hash.");
                var journal = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
                if ((string)journal["schema"] != PackageOperationFiles.JournalSchema || (string)journal["project"] != root ||
                    (string)journal["operationId"] != operationId || (string)journal["kind"] != "embed" || (string)journal["commandSha256"] != CommandHash())
                    throw new PackageOperationException("JOURNAL_IDENTITY_MISMATCH", "The receipt must identify this exact project, Embed operation, and command source.");
                var knownTerminalReadback = (string)journal["state"] == "awaiting-readback" && (bool?)journal["nativeCompleted"] == true &&
                    (string)journal["nativeStatus"] == "Success" && (bool?)journal["requestUnresolved"] == false;
                if (mode != "completed-readback" && (((bool?)journal["requestUnresolved"] != true && !(mode == "lifecycle-inverse" && knownTerminalReadback)) || (bool?)journal["observerDetached"] != true))
                    throw new PackageOperationException("RECONCILIATION_NOT_REQUIRED", "Select a detached unresolved Embed receipt.");
                if ((string)journal["name"] == ImplementationPackage) throw new PackageOperationException("IMPLEMENTATION_PACKAGE_PROTECTED", "The implementation package cannot be reconciled for removal.");
                JObject plan;
                if (mode == "sdk-terminal") plan = new JObject { ["mode"] = mode, ["operationId"] = operationId, ["journalSha256"] = hash };
                else if (mode == "lifecycle-inverse") plan = PreviewLifecycleInverse(root, journal, registeredPackage((string)journal["name"], (string)journal["version"]));
                else if (mode == "completed-readback")
                {
                    BindIdentity((int?)journal["editorPid"] ?? 0, (string)journal["nativeProcessStartTicks"]); RequireStopped();
                    PackageEmbedObserver.RequireIdleForReadback(root, journal);
                    plan = PreviewCompletedReadback(root, journal, registeredPackage((string)journal["name"], (string)journal["version"]));
                }
                else throw new PackageOperationException("RECONCILIATION_MODE_INVALID", "Select sdk-terminal, completed-readback, or lifecycle-inverse.");
                if (dryRun) return CommandResult<JObject>.Success(Schema, Preview(plan, operationId, true));
                if (!confirm) throw new PackageOperationException("CONFIRMATION_REQUIRED", "Confirm the exact reconciliation receipt and native Editor identity.");
                BindIdentity(expectedNativePid, expectedNativeProcessStartTicks); RequireStopped();
                if (mode == "sdk-terminal") return CommandResult<JObject>.Success(Schema, PackageEmbedObserver.ReconcileForRequest(root, operationId, hash));
                BindPlan(plan, expectedPlanSha256);
                if (mode == "completed-readback")
                {
                    if ((string)journal["state"] == "completed")
                        return CommandResult<JObject>.Success(Schema, new JObject { ["completed"] = true, ["idempotent"] = true, ["operationId"] = operationId, ["journalSha256"] = hash });
                    foreach (var field in new[] { "destinationAfter", "manifestAfter", "lockAfter" }) journal[field] = plan[field].DeepClone();
                    journal["state"] = "completed"; journal["completedReadbackProof"] = plan.DeepClone();
                    journal["readbackUtc"] = DateTime.UtcNow.ToString("o");
                    var sealedHash = PackageOperationFiles.Publish(root, journal, hash);
                    return CommandResult<JObject>.Success(Schema, new JObject { ["completed"] = true, ["operationId"] = operationId, ["journalSha256"] = sealedHash });
                }
                journal["detachedEvidence"] = journal.DeepClone();
                journal["observedStateInverse"] = true; journal["state"] = "observed-inverse-ready";
                journal["observedNoMutation"] = (bool?)plan["observedNoMutation"] == true;
                journal["lifecycleInverseProof"] = plan.DeepClone();
                foreach (var field in new[] { "destinationAfter", "manifestAfter", "lockAfter" }) journal[field] = plan[field].DeepClone();
                journal["reconciledUtc"] = DateTime.UtcNow.ToString("o");
                var updated = PackageOperationFiles.Publish(root, journal, hash);
                return CommandResult<JObject>.Success(Schema, new JObject { ["observedStateInverse"] = true, ["requestUnresolved"] = journal["requestUnresolved"]?.DeepClone(), ["operationId"] = operationId, ["journalSha256"] = updated });
            }
            catch (Exception exception) { return Failure(exception); }
        }
        internal static JObject PreviewLifecycleInverse(string root, JObject journal, JObject package)
        {
            RequireOriginalProcessAbsent(journal);
            var name = (string)journal["name"]; var version = (string)journal["version"];
            if (name == ImplementationPackage) throw new PackageOperationException("IMPLEMENTATION_PACKAGE_PROTECTED", "The implementation package cannot be removed.");
            var destination = EmbeddedInventory(root, name);
            var manifest = ManifestState(root, "manifest.json"); var packageLock = ManifestState(root, "packages-lock.json");
            if ((string)package["source"] != "Embedded")
            {
                if (!JToken.DeepEquals(journal["originalPackage"], package) ||
                    journal["destinationBefore"] is not JObject before || ((JObject)before["files"]).Count != 0 || before["directories"].Any() ||
                    !PackageOperationFiles.SameInventory(before, destination))
                    throw new PackageOperationException("RECOVERY_PACKAGE_MISMATCH", "Observed no-mutation inverse requires the exact original registered package and absent original destination.");
                var sourceRoot = Path.GetFullPath((string)package["resolvedPath"]);
                var source = PackageOperationFiles.Inventory(sourceRoot, new[] { "." }, false,
                    relative => PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(Path.Combine(sourceRoot, relative), sourceRoot)));
                if (!PackageOperationFiles.SameInventory((JObject)journal["sourceInventory"], source))
                    throw new PackageOperationException("RECOVERY_SOURCE_MISMATCH", "The complete original package source changed after capture.");
                if (!JToken.DeepEquals(journal["manifestBefore"], manifest) || !JToken.DeepEquals(journal["lockBefore"], packageLock))
                    throw new PackageOperationException("RECOVERY_HASH_MISMATCH", "Observed no-mutation inverse requires byte-identical original manifest and lock.");
                return LifecyclePlan(journal, package, destination, manifest, packageLock, true);
            }
            var observed = ValidateEmbeddedOwnership(root, journal, package);
            return LifecyclePlan(journal, package, (JObject)observed["destinationAfter"], (JObject)observed["manifestAfter"], (JObject)observed["lockAfter"], false);
        }
        private static JObject PreviewCompletedReadback(string root, JObject journal, JObject package)
        {
            if (!new[] { "awaiting-readback", "completed" }.Contains((string)journal["state"]) ||
                (bool?)journal["nativeCompleted"] != true || (string)journal["nativeStatus"] != "Success" ||
                (bool?)journal["requestUnresolved"] != false || (bool?)journal["acknowledged"] != true || (bool?)journal["observerDetached"] != true ||
                journal["nativeResult"] is not JObject native || (string)native["name"] != (string)journal["name"] ||
                (string)native["version"] != (string)journal["version"] || (string)native["source"] != "Embedded" ||
                !string.Equals(Path.GetFullPath((string)native["resolvedPath"] ?? ""), Path.GetFullPath(Path.Combine(root, "Packages", (string)journal["name"])), ProjectPathPolicy.PathComparison()))
                throw new PackageOperationException("EMBED_NOT_TERMINAL", "Readback requires the exact persisted terminal SDK Success and detached observer.");
            var plan = ValidateEmbeddedOwnership(root, journal, package);
            if ((string)journal["state"] == "completed")
            {
                foreach (var field in new[] { "destinationAfter", "manifestAfter", "lockAfter" })
                    if (!JToken.DeepEquals(journal[field], plan[field])) throw new PackageOperationException("RECOVERY_HASH_MISMATCH", "The completed seal differs from current bytes; retain external changes.");
            }
            plan["mode"] = "completed-readback"; plan["operationId"] = journal["operationId"]?.DeepClone();
            plan["planSha256"] = PackageOperationFiles.HashJson(plan); return plan;
        }
        private static JObject ValidateEmbeddedOwnership(string root, JObject journal, JObject package)
        {
            var name = (string)journal["name"]; var version = (string)journal["version"];
            if (journal["destinationBefore"] is not JObject before || before["files"] is not JObject beforeFiles || beforeFiles.Count != 0 ||
                before["directories"] is not JArray beforeDirectories || beforeDirectories.Count != 0)
                throw new PackageOperationException("RECOVERY_OWNERSHIP_MISMATCH", "Readback requires a destination absent before the exact operation.");
            var destination = EmbeddedInventory(root, name);
            var manifest = ManifestState(root, "manifest.json"); var packageLock = ManifestState(root, "packages-lock.json");
            if ((string)package["name"] != name || (string)package["version"] != version || (string)package["source"] != "Embedded" ||
                !string.Equals(Path.GetFullPath((string)package["resolvedPath"]), Path.GetFullPath(Path.Combine(root, "Packages", name)), ProjectPathPolicy.PathComparison()))
                throw new PackageOperationException("RECOVERY_PACKAGE_MISMATCH", "Lifecycle inverse requires the exact registered embedded destination.");
            var prefix = "Packages/" + name;
            var files = new JObject(); foreach (var item in ((JObject)destination["files"]).Properties()) files[item.Name.Substring(prefix.Length + 1)] = item.Value.DeepClone();
            var normalized = new JObject { ["files"] = files, ["directories"] = new JArray(destination["directories"].Values<string>().Select(path => path == prefix ? "." : path.Substring(prefix.Length + 1)).OrderBy(path => path, StringComparer.Ordinal)) };
            if (!PackageOperationFiles.SameInventory((JObject)journal["sourceInventory"], normalized))
                throw new PackageOperationException("RECOVERY_SOURCE_MISMATCH", "The complete embedded bytes, metadata, and directories differ from the captured source.");
            if (!JToken.DeepEquals(journal["manifestBefore"], manifest)) throw new PackageOperationException("RECOVERY_MANIFEST_CHANGED", "Lifecycle inverse requires unchanged original manifest bytes.");
            JObject Decode(JToken state) => JObject.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String((string)state["bytesBase64"])));
            var expectedLock = Decode(journal["lockBefore"]); var originalNode = expectedLock["dependencies"]?[name];
            if (originalNode == null) throw new PackageOperationException("RECOVERY_LOCK_CHANGED", "The captured lock does not identify the selected package.");
            expectedLock["dependencies"][name] = new JObject { ["version"] = "file:" + name, ["depth"] = 0, ["source"] = "embedded", ["dependencies"] = originalNode["dependencies"]?.DeepClone() ?? new JObject() };
            if (!JToken.DeepEquals(expectedLock, Decode(packageLock))) throw new PackageOperationException("RECOVERY_LOCK_CHANGED", "The lock changes exceed the exact selected package's embedded representation.");
            return new JObject { ["package"] = package.DeepClone(), ["destinationAfter"] = destination, ["manifestAfter"] = manifest, ["lockAfter"] = packageLock };
        }
        private static JObject LifecyclePlan(JObject journal, JObject package, JObject destination, JObject manifest, JObject packageLock, bool noMutation)
        {
            var plan = new JObject { ["observedStateInverse"] = true, ["observedNoMutation"] = noMutation, ["oldNativePid"] = journal["editorPid"]?.DeepClone(), ["oldNativeProcessStartTicks"] = journal["nativeProcessStartTicks"]?.DeepClone(),
                ["package"] = package.DeepClone(), ["destinationAfter"] = destination, ["manifestAfter"] = manifest, ["lockAfter"] = packageLock };
            plan["planSha256"] = PackageOperationFiles.HashJson(plan); return plan;
        }
        internal static bool HasLifecycleInverseProof(JObject journal)
        {
            if ((bool?)journal["observedStateInverse"] != true || journal["lifecycleInverseProof"] is not JObject proof) return false;
            if (!JToken.DeepEquals(proof["oldNativePid"], journal["editorPid"]) || !JToken.DeepEquals(proof["oldNativeProcessStartTicks"], journal["nativeProcessStartTicks"])) return false;
            if ((bool?)proof["observedNoMutation"] != (bool?)journal["observedNoMutation"]) return false;
            foreach (var field in new[] { "destinationAfter", "manifestAfter", "lockAfter" }) if (!JToken.DeepEquals(proof[field], journal[field])) return false;
            var hash = (string)proof["planSha256"]; var copy = (JObject)proof.DeepClone(); copy.Remove("planSha256");
            if (hash != PackageOperationFiles.HashJson(copy)) return false;
            RequireOriginalProcessAbsent(journal); return true;
        }
        private static void RequireOriginalProcessAbsent(JObject journal)
        {
            if (journal["editorPid"]?.Type != JTokenType.Integer || journal["nativeProcessStartTicks"]?.Type != JTokenType.String)
                throw new PackageOperationException("LIFECYCLE_BARRIER_REQUIRED", "The receipt must contain the exact original native Editor identity.");
            var pidValue = (long)journal["editorPid"];
            if (pidValue <= 0 || pidValue > int.MaxValue) throw new PackageOperationException("LIFECYCLE_BARRIER_REQUIRED", "The original native PID is invalid.");
            var pid = (int?)pidValue; var ticks = (string)journal["nativeProcessStartTicks"];
            if (!pid.HasValue || pid <= 0 || !long.TryParse(ticks, NumberStyles.None, CultureInfo.InvariantCulture, out var started) || started <= 0)
                throw new PackageOperationException("LIFECYCLE_BARRIER_REQUIRED", "The receipt must contain the exact original native Editor identity.");
            try
            {
                using var process = Process.GetProcessById(pid.Value);
                if (!process.HasExited && process.StartTime.ToUniversalTime().Ticks == started)
                    throw new PackageOperationException("LIFECYCLE_BARRIER_REQUIRED", "Stop the exact original Editor and its owned auxiliary workers, then reopen and verify the registered project before observed-state inverse.");
            }
            catch (ArgumentException) { }
        }
        [CliCommand("package_sample_readback", "Read exact imported sample bytes, metadata, GUIDs and installed package after terminal compilation.", Tags = new[] { "unity-cli-commands", "package", "asset" })]
        public static CommandResult<JObject> SampleReadback(string operationId, string expectedJournalSha256)
        {
            try
            {
                var root = Root(); var path = PackageOperationFiles.JournalPath(root, operationId);
                var bytes = File.ReadAllBytes(path);
                if (PackageOperationFiles.Hash(bytes) != expectedJournalSha256) throw new PackageOperationException("JOURNAL_HASH_MISMATCH", "Bind readback to the exact current operation journal.");
                var journal = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
                if ((string)journal["schema"] != PackageOperationFiles.JournalSchema || (string)journal["kind"] != "sample" || (string)journal["operationId"] != operationId ||
                    (string)journal["project"] != root || ((string)journal["state"] != "imported" && (string)journal["state"] != "awaiting-readback") ||
                    (bool?)journal["acknowledged"] != true || (bool?)journal["importAcknowledged"] != true || (string)journal["commandSha256"] != CommandHash())
                    throw new PackageOperationException("SAMPLE_NOT_TERMINAL", "The exact sample journal is not an acknowledged import for this project and command source.");
                var package = ExactPackage((string)journal["name"], (string)journal["version"]);
                BindIdentity((int)journal["editorPid"], (string)journal["nativeProcessStartTicks"]);
                var awaitingReadback = (string)journal["state"] == "awaiting-readback";
                var plan = (JObject)journal["samplePlan"]; PackageSampleInventory.ValidatePlanHash(plan);
                if (new[] { "name", "version", "sampleName", "commandSha256" }.Any(field => !JToken.DeepEquals(plan[field], journal[field])) ||
                    !JToken.DeepEquals(plan["originalPackage"], PackageData(package)))
                    throw new PackageOperationException("PLAN_HASH_MISMATCH", "The retained sample plan is not bound to the exact installed package and command source.");
                var matches = Sample.FindByPackage(package.name, package.version).Where(sample => sample.displayName == (string)journal["sampleName"]).ToArray();
                if (matches.Length != 1 || !string.Equals(Path.GetFullPath(matches[0].resolvedPath), Path.GetFullPath((string)plan["sampleRoot"]), ProjectPathPolicy.PathComparison()) ||
                    Path.GetRelativePath(root, matches[0].importPath).Replace('\\', '/') != (string)plan["importPath"])
                    throw new PackageOperationException("PLAN_HASH_MISMATCH", "The native sample source or import path differs from the retained plan.");
                var actualPlan = PackageSampleInventory.Prepare(root, matches[0].resolvedPath, package.resolvedPath, (string)plan["importPath"], true);
                if (new[] { "source", "sampleRoot", "packageRoot", "importPath", "archive", "entries", "watched", "sourceInventory" }.Any(field => !JToken.DeepEquals(actualPlan[field], plan[field])))
                    throw new PackageOperationException("PLAN_HASH_MISMATCH", "The canonical sample source planner differs from the approved retained plan.");
                PackageSampleInventory.ValidateCompletionInventory(root, plan, (JObject)journal["destinationAfter"], PackageSampleInventory.DestinationInventory(root, plan), false);
                PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource((string)journal["samplePlan"]["sampleRoot"], package.resolvedPath));
                PackageSampleInventory.ValidateReadbackFiles(root, (JObject)journal["samplePlan"], awaitingReadback);
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    throw new PackageOperationException("SAMPLE_IMPORT_PENDING", "Native sample import or compilation is still active.");
                RequireStopped(); PackageEmbedObserver.RequireIdle(root);
                var result = PackageSampleInventory.Readback(root, plan, awaitingReadback);
                PackageSampleInventory.ValidateCompletionInventory(root, plan, (JObject)journal["destinationAfter"], (JObject)result["inventory"], false);
                if (!awaitingReadback && !PackageOperationFiles.SameInventory((JObject)journal["destinationAfter"], (JObject)result["inventory"]))
                    throw new PackageOperationException("SAMPLE_DESTINATION_CHANGED", "The sample inventory changed after its captured import outcome.");
                result["package"] = PackageData(package);
                if (awaitingReadback)
                {
                    journal["readback"] = result.DeepClone(); journal["destinationAfter"] = result["inventory"].DeepClone();
                    journal["state"] = "imported"; journal["finishedUtc"] = DateTime.UtcNow.ToString("o");
                    PackageOperationFiles.Publish(root, journal, expectedJournalSha256);
                }
                return CommandResult<JObject>.Success(Schema, result);
            }
            catch (Exception exception) { return Failure(exception); }
        }
        [CliCommand("package_embed_detach", "Detach this operation's observer, retain unresolved UPM state, and never claim SDK cancellation.", Tags = new[] { "unity-cli-commands", "package" })]
        public static CommandResult<JObject> Detach(string operationId, bool confirm = false)
        {
            try
            {
                if (!confirm) throw new PackageOperationException("CONFIRMATION_REQUIRED", "Confirm observer detachment; it does not cancel the SDK request.");
                PackageOperationFiles.JournalPath(Root(), operationId);
                return CommandResult<JObject>.Success(Schema, new JObject { ["observerDetached"] = PackageEmbedObserver.DetachOperation(operationId, "host-interruption"), ["sdkRequestCancelled"] = false });
            }
            catch (Exception exception) { return Failure(exception); }
        }
        internal static JObject EmbeddedInventory(string root, string name)
        {
            var destination = ProjectPathPolicy.ValidatePackageRoot(name, root);
            PackageOperationFiles.Require(destination);
            return PackageOperationFiles.Inventory(root, destination.Result.Exists ? new[] { destination.Result.Path } : Array.Empty<string>(), false,
                relative => PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(Path.Combine(root, relative), Path.Combine(root, "Packages", name))));
        }
        private static void RecoverEmbedded(string root, JObject journal, bool preview, int pid, string start)
        {
            RecoverEmbeddedForPackage(root, journal, PackageData(ExactPackage((string)journal["name"], (string)journal["version"])), preview, pid, start);
        }
        internal static void RecoverEmbeddedForPackage(string root, JObject journal, JObject installed, bool preview, int pid, string start)
        {
            var name = (string)journal["name"];
            if ((string)installed["name"] != name || (string)installed["version"] != (string)journal["version"] ||
                (string)installed["source"] != "Embedded" || !string.Equals(Path.GetFullPath((string)installed["resolvedPath"]), Path.GetFullPath(Path.Combine(root, "Packages", name)), ProjectPathPolicy.PathComparison()))
                throw new PackageOperationException("RECOVERY_PACKAGE_MISMATCH", "The installed package is not the exact operation-created embedded destination.");
            var current = EmbeddedInventory(root, name);
            if (journal["destinationAfter"] == null || !PackageOperationFiles.SameInventory((JObject)journal["destinationAfter"], current) ||
                !JToken.DeepEquals(journal["manifestAfter"], ManifestState(root, "manifest.json")) ||
                !JToken.DeepEquals(journal["lockAfter"], ManifestState(root, "packages-lock.json")))
                throw new PackageOperationException("RECOVERY_HASH_MISMATCH", "The embedded root, manifest, or lock changed; retain the journal and external changes.");
            if (((JObject)journal["destinationBefore"]["files"]).Count != 0 || journal["destinationBefore"]["directories"].Any())
                throw new PackageOperationException("RECOVERY_OWNERSHIP_MISMATCH", "Recovery only removes a root that was absent before this operation.");
            if (preview) return;
            BindIdentity(pid, start); RequireStopped();
            PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageRoot(name, root));
            // The complete inventory is checked before the first delete; every leaf is revalidated again.
            foreach (var file in ((JObject)current["files"]).Properties().OrderByDescending(item => item.Name.Length))
            {
                PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(Path.Combine(root, file.Name), Path.Combine(root, "Packages", name)));
                if ((string)PackageOperationFiles.FileState(root, file.Name, false)["sha256"] != (string)file.Value["sha256"])
                    throw new PackageOperationException("RECOVERY_HASH_MISMATCH", "An embedded file changed at the deletion boundary.");
                File.Delete(Path.Combine(root, file.Name));
            }
            foreach (var directory in current["directories"].Values<string>().OrderByDescending(value => value.Length))
            {
                PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(Path.Combine(root, directory), Path.Combine(root, "Packages", name)));
                Directory.Delete(Path.Combine(root, directory));
            }
            RestoreManifest(root, "manifest.json", (JObject)journal["manifestBefore"], (JObject)journal["manifestAfter"]);
            RestoreManifest(root, "packages-lock.json", (JObject)journal["lockBefore"], (JObject)journal["lockAfter"]);
        }
        private static void RestoreManifest(string root, string filename, JObject before, JObject after)
        {
            if (!JToken.DeepEquals(after, ManifestState(root, filename))) throw new PackageOperationException("RECOVERY_HASH_MISMATCH", "The package manifest or lock changed during inverse mutation.");
            var target = Path.Combine(root, "Packages", filename);
            PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(target, Path.Combine(root, "Packages")));
            if ((bool)before["exists"]) File.WriteAllBytes(target, Convert.FromBase64String((string)before["bytesBase64"]));
            else if (File.Exists(target)) File.Delete(target);
            if (!JToken.DeepEquals(before, ManifestState(root, filename))) throw new PackageOperationException("RECOVERY_READBACK_MISMATCH", "Manifest or lock bytes differ after recovery.");
        }
        private static JObject ManifestState(string root, string name)
        {
            PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(Path.Combine(root, "Packages", name), Path.Combine(root, "Packages")));
            return PackageOperationFiles.FileState(root, "Packages/" + name);
        }
        private static JObject SourceInventory(PackageInfo package)
            => PackageOperationFiles.Inventory(package.resolvedPath, new[] { "." }, false,
                path => PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(Path.Combine(package.resolvedPath, path), package.resolvedPath)));
        private static PackageInfo ExactPackage(string name, string version)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version)) throw new PackageOperationException("PACKAGE_IDENTITY_REQUIRED", "Select an exact installed package name and version.");
            var matches = PackageInfo.GetAllRegisteredPackages().Where(package => package.name == name).ToArray();
            if (matches.Length != 1) throw new PackageOperationException("PACKAGE_NOT_INSTALLED", "Select one exact installed package name.");
            if (matches[0].version != version) throw new PackageOperationException("PACKAGE_VERSION_MISMATCH", "The requested version differs from the installed package.");
            return matches[0];
        }
        private static JObject PackageData(PackageInfo package) => new JObject { ["name"] = package.name, ["version"] = package.version,
            ["source"] = package.source.ToString(), ["resolvedPath"] = package.resolvedPath, ["displayName"] = package.displayName,
            ["isDirectDependency"] = package.isDirectDependency, ["dependencies"] = JArray.FromObject(package.dependencies) };
        private static string Root() => Directory.GetParent(UnityEngine.Application.dataPath).FullName;
        private static string StartTicks() => Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture);
        private static void BindIdentity(int pid, string ticks)
        {
            if (pid != Process.GetCurrentProcess().Id || ticks != StartTicks()) throw new PackageOperationException("EDITOR_IDENTITY_MISMATCH", "Bind mutation to the PID and native process start ticks returned by its preview.");
        }
        private static void RequireStopped()
            => RequireStoppedState(EditorApplication.isPlayingOrWillChangePlaymode, EditorApplication.isCompiling, EditorApplication.isUpdating);
        internal static void RequireStoppedState(bool playing, bool compiling, bool updating)
        {
            if (playing || compiling || updating)
                throw new PackageOperationException("EDITOR_NOT_READY", "Wait for a stopped, idle, compilation-clean Editor before package mutation.");
        }
        private static void GuardOperation(string root, string id)
        {
            var path = PackageOperationFiles.JournalPath(root, id);
            if (File.Exists(path)) throw new PackageOperationException("OPERATION_ID_REUSED", "Use a fresh operation ID; preserve previous journals for recovery.");
        }
        private static string CommandHash()
        {
            var package = PackageInfo.FindForAssembly(typeof(PackageAuthoringCommands).Assembly);
            if (package == null) throw new PackageOperationException("COMMAND_SOURCE_UNAVAILABLE", "The package command source cannot be identified.");
            var folder = Path.Combine(package.resolvedPath, "Editor/Packages");
            var hashes = new JObject();
            foreach (var file in Directory.GetFiles(folder, "*.cs").OrderBy(path => path, StringComparer.Ordinal))
            {
                PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(file, package.resolvedPath));
                hashes[Path.GetFileName(file)] = PackageOperationFiles.Hash(File.ReadAllBytes(file));
            }
            return PackageOperationFiles.HashJson(hashes);
        }
        private static void BindPlan(JObject plan, string hash)
        {
            if (hash != (string)plan["planSha256"]) throw new PackageOperationException("PLAN_HASH_MISMATCH", "Use the exact fresh preview hash; source or destination may have changed.");
        }
        private static JObject Preview(JObject plan, string id, bool dryRun)
        {
            var result = (JObject)plan.DeepClone(); result["operationId"] = id; result["dryRun"] = dryRun; result["applied"] = false; result["acknowledged"] = false;
            result["nativePid"] = Process.GetCurrentProcess().Id; result["nativeProcessStartTicks"] = StartTicks();
            return result;
        }
        private static JObject Journal(string root, string id, string kind, string name, string version, string hostStart)
            => new JObject { ["schema"] = PackageOperationFiles.JournalSchema, ["operationId"] = id, ["project"] = root,
                ["editorPid"] = Process.GetCurrentProcess().Id, ["nativeProcessStartTicks"] = StartTicks(),
                ["editorStartedAt"] = hostStart, ["commandSha256"] = CommandHash(), ["name"] = name, ["version"] = version,
                ["kind"] = kind, ["state"] = "starting", ["startedUtc"] = DateTime.UtcNow.ToString("o"), ["acknowledged"] = false,
                ["observerDetached"] = kind != "embed", ["requestUnresolved"] = false, ["sdkRequestCancelled"] = false };
        private static CommandResult<JObject> Failure(Exception exception, JObject journal = null)
        {
            var code = exception is PackageOperationException refusal ? refusal.Code : "PACKAGE_OPERATION_FAILED";
            return CommandResult<JObject>.Failure(Schema, code, exception.GetBaseException().Message,
                journal == null ? null : new Dictionary<string, object> { ["recoveryJournal"] = journal });
        }
    }
}
