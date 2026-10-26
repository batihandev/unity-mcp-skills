using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace BatihanDev.UnityCliCommands.Packages
{
    internal static class PackageSampleInventory
    {
        internal static JObject Prepare(string root, string sampleRoot, string packageRoot, string importPath, bool overwrite)
        {
            root = Path.GetFullPath(root); sampleRoot = Path.GetFullPath(sampleRoot); packageRoot = Path.GetFullPath(packageRoot);
            PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(sampleRoot, packageRoot));
            if (!Directory.Exists(sampleRoot)) throw new PackageOperationException("SAMPLE_SOURCE_MISSING", "The selected native sample directory is missing.");
            var sourceInventory = SourceInventory(sampleRoot, packageRoot);
            var archives = Directory.GetFiles(sampleRoot, "*.unitypackage", SearchOption.TopDirectoryOnly);
            if (archives.Length > 1) throw new PackageOperationException("SAMPLE_ARCHIVE_AMBIGUOUS", "The selected sample directory contains more than one top-level archive.");
            var archive = archives.Length == 1;
            var source = archive ? Path.GetFullPath(archives[0]) : sampleRoot;
            PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(source, sampleRoot));
            PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(source, packageRoot));
            if (archive && (!File.Exists(source) || (File.GetAttributes(source) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0))
                throw new PackageOperationException("SAMPLE_SOURCE_INVALID", "The selected sample archive must be a confined regular file.");
            var entries = archive ? ArchiveEntries(root, source) : FolderEntries(root, source, importPath, sourceInventory);
            if (entries.Count == 0) throw new PackageOperationException("SAMPLE_EMPTY", "The exact sample has no importable entries.");
            var roots = new SortedSet<string>(StringComparer.Ordinal);
            if (archive)
            {
                foreach (var entry in entries) roots.Add(((string)entry["path"]).Split('/').Take(2).Aggregate((left, right) => left + "/" + right));
            }
            else
            {
                var pieces = importPath.Split('/');
                roots.Add(pieces.Length >= 4 && pieces[1] == "Samples" ? string.Join("/", pieces.Take(3)) : importPath);
            }
            var watched = roots.ToList();
            foreach (var selected in roots)
                for (var parent = selected; parent != "Assets" && !string.IsNullOrEmpty(parent); parent = Path.GetDirectoryName(parent)?.Replace('\\', '/'))
                    watched.Add(parent + ".meta");
            var before = CaptureDestination(root, watched.ToArray());
            var collisions = new JArray();
            foreach (var entry in entries)
            {
                var path = (string)entry["path"];
                ValidateAsset(root, path);
                if (File.Exists(Path.Combine(root, path)) || Directory.Exists(Path.Combine(root, path))) collisions.Add(path);
            }
            // Prior-version removal is authorized against the complete containing sample inventory.
            if (!archive && roots.Any(path => Directory.Exists(Path.Combine(root, path)) && Directory.GetFileSystemEntries(Path.Combine(root, path)).Length > 0))
                foreach (var path in ((JObject)before["files"]).Properties().Select(item => item.Name)) if (!collisions.Values<string>().Contains(path)) collisions.Add(path);
            if (collisions.Count > 0 && !overwrite)
                throw new PackageOperationException("SAMPLE_COLLISION", "Preview destination collisions with allowOverwrite=true, then confirm the exact plan hash.");
            ValidateGuids(root, entries, archive, overwrite);
            if (!PackageOperationFiles.SameInventory(sourceInventory, SourceInventory(sampleRoot, packageRoot)))
                throw new PackageOperationException("SAMPLE_SOURCE_CHANGED", "The selected sample source changed during preview.");
            var plan = new JObject { ["source"] = source, ["sampleRoot"] = sampleRoot, ["packageRoot"] = packageRoot,
                ["importPath"] = importPath, ["archive"] = archive,
                ["allowOverwrite"] = overwrite, ["entries"] = entries, ["watched"] = new JArray(watched.Distinct()),
                ["before"] = before, ["sourceInventory"] = sourceInventory, ["collisions"] = collisions };
            plan["planSha256"] = PackageOperationFiles.HashJson(plan);
            return plan;
        }
        internal static void ValidatePlanHash(JObject plan)
        {
            var copy = (JObject)plan.DeepClone(); copy.Remove("planSha256");
            if ((string)plan["planSha256"] != PackageOperationFiles.HashJson(copy))
                throw new PackageOperationException("PLAN_HASH_MISMATCH", "The retained sample plan content differs from its approved digest.");
        }
        internal static void ValidateCompletionInventory(string root, JObject plan, JObject retained, JObject current, bool allowPriorRemoval)
        {
            var before = (JObject)plan["before"]["files"]; var baseline = (JObject)retained["files"]; var files = (JObject)current["files"];
            var entries = plan["entries"].Select(entry => (string)entry["path"]).ToHashSet(StringComparer.Ordinal);
            var directories = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in plan["entries"])
            {
                var path = (string)entry["path"];
                if ((bool?)entry["directory"] == true) directories.Add(path);
                for (var parent = Path.GetDirectoryName(path)?.Replace('\\', '/'); parent != "Assets" && !string.IsNullOrEmpty(parent); parent = Path.GetDirectoryName(parent)?.Replace('\\', '/')) directories.Add(parent);
            }
            var generatedMeta = entries.Where(path => !path.EndsWith(".meta", StringComparison.Ordinal)).Select(path => path + ".meta")
                .Concat(directories.Select(path => path + ".meta")).ToHashSet(StringComparer.Ordinal);
            bool PriorRemoval(string path) => allowPriorRemoval && (bool)plan["allowOverwrite"] && !(bool)plan["archive"] &&
                path.StartsWith(string.Join("/", ((string)plan["importPath"]).Split('/').Take(3)) + "/", StringComparison.Ordinal);
            foreach (var path in before.Properties().Select(item => item.Name).Concat(baseline.Properties().Select(item => item.Name)).Concat(files.Properties().Select(item => item.Name)).Distinct(StringComparer.Ordinal))
            {
                if (entries.Contains(path)) continue;
                var expected = baseline[path]; var actual = files[path];
                if (actual == null && expected != null && PriorRemoval(path)) continue;
                if (expected != null)
                {
                    if (actual == null || !PackageOperationFiles.SameInventory(new JObject { ["files"] = new JObject { [path] = expected.DeepClone() }, ["directories"] = new JArray() }, new JObject { ["files"] = new JObject { [path] = actual.DeepClone() }, ["directories"] = new JArray() }))
                        throw new PackageOperationException("SAMPLE_DESTINATION_CHANGED", "An unrelated retained sample file changed or disappeared; preserve its external bytes.");
                }
                else if (actual != null)
                {
                    if (!generatedMeta.Contains(path) || before[path] != null) throw new PackageOperationException("SAMPLE_DESTINATION_CHANGED", "A foreign file appeared outside the approved sample mutation footprint.");
                }
                if (actual != null && before[path] == null && generatedMeta.Contains(path))
                {
                    var asset = path.Substring(0, path.Length - 5); var meta = PackageOperationFiles.MetaGuid(File.ReadAllBytes(Path.Combine(root, path)));
                    var registered = AssetDatabase.AssetPathToGUID(asset);
                    if (meta == null || !string.IsNullOrEmpty(registered) && !string.Equals(meta, registered, StringComparison.OrdinalIgnoreCase))
                        throw new PackageOperationException("SAMPLE_DESTINATION_CHANGED", "Generated metadata differs from its approved sample asset or ancestor registration.");
                    if (!allowPriorRemoval && string.IsNullOrEmpty(registered))
                        throw new PackageOperationException("SAMPLE_IMPORT_PENDING", "Generated sample metadata is still awaiting native registration.");
                }
            }
            foreach (var field in new[] { "directories", "parentDirectories" })
            {
                var prior = retained[field].Values<string>().ToHashSet(StringComparer.Ordinal); var observed = current[field].Values<string>().ToHashSet(StringComparer.Ordinal);
                if (observed.Any(path => !prior.Contains(path) && !directories.Contains(path)) || prior.Any(path => !observed.Contains(path) && !PriorRemoval(path)))
                    throw new PackageOperationException("SAMPLE_DESTINATION_CHANGED", "An unrelated retained directory changed outside the approved sample mutation footprint.");
            }
        }
        internal static void ValidateSource(JObject plan)
        {
            PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource((string)plan["sampleRoot"], (string)plan["packageRoot"]));
            PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource((string)plan["source"], (string)plan["sampleRoot"]));
            PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource((string)plan["source"], (string)plan["packageRoot"]));
            if (!PackageOperationFiles.SameInventory((JObject)plan["sourceInventory"], SourceInventory((string)plan["sampleRoot"], (string)plan["packageRoot"])))
                throw new PackageOperationException("SAMPLE_SOURCE_CHANGED", "The selected sample source changed after preview.");
        }
        internal static void ValidateCurrent(string root, JObject plan)
        {
            ValidateSource(plan);
            if (!PackageOperationFiles.SameInventory((JObject)plan["before"], DestinationInventory(root, plan)))
                throw new PackageOperationException("SAMPLE_DESTINATION_CHANGED", "The selected sample destination changed after preview.");
            ValidateGuids(root, (JArray)plan["entries"], (bool)plan["archive"], (bool)plan["allowOverwrite"]);
            foreach (var path in plan["watched"].Values<string>()) ValidateAsset(root, path);
            foreach (var entry in plan["entries"]) ValidateAsset(root, (string)entry["path"]);
        }
        internal static JObject DestinationInventory(string root, JObject plan)
            => CaptureDestination(root, plan["watched"].Values<string>().ToArray());
        internal static void Restore(string root, JObject plan, JObject after)
        {
            var current = DestinationInventory(root, plan);
            ValidateCompletionInventory(root, plan, (JObject)plan["before"], after, true);
            if (!PackageOperationFiles.SameInventory(after, current))
                throw new PackageOperationException("RECOVERY_HASH_MISMATCH", "Destination inventory changed; preserve external changes and recovery snapshots.");
            PackageOperationFiles.RestoreInventory(root, (JObject)plan["before"], current, path => ValidateAsset(root, path));
            var oldParents = plan["before"]["parentDirectories"].Values<string>().ToHashSet(StringComparer.Ordinal);
            foreach (var parent in after["parentDirectories"].Values<string>().OrderByDescending(path => path.Length))
                if (!oldParents.Contains(parent))
                {
                    ValidateAsset(root, parent);
                    var full = Path.Combine(root, parent);
                    if (Directory.Exists(full) && Directory.GetFileSystemEntries(full).Length == 0) Directory.Delete(full);
                }
            if (!PackageOperationFiles.SameInventory((JObject)plan["before"], DestinationInventory(root, plan)))
                throw new PackageOperationException("RECOVERY_READBACK_MISMATCH", "Sample recovery did not reproduce the captured inventory.");
        }
        private static JObject CaptureDestination(string root, string[] watched)
        {
            var inventory = PackageOperationFiles.Inventory(root, watched, true, path => ValidateAsset(root, path));
            var parents = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var path in watched)
                for (var parent = Path.GetDirectoryName(path)?.Replace('\\', '/'); parent != "Assets" && !string.IsNullOrEmpty(parent); parent = Path.GetDirectoryName(parent)?.Replace('\\', '/'))
                {
                    ValidateAsset(root, parent);
                    if (Directory.Exists(Path.Combine(root, parent))) parents.Add(parent);
                }
            inventory["parentDirectories"] = new JArray(parents);
            return inventory;
        }
        internal static void ValidateReadbackFiles(string root, JObject plan, bool awaitingReadback)
        {
            ValidateSource(plan);
            var missing = false;
            foreach (var entry in plan["entries"])
            {
                var path = (string)entry["path"]; ValidateAsset(root, path);
                var full = Path.Combine(root, path);
                var directory = (bool?)entry["directory"] == true;
                if (directory ? File.Exists(full) : Directory.Exists(full))
                    throw new PackageOperationException("SAMPLE_READBACK_MISMATCH", "Imported destination kind differs from the approved sample source.");
                if (directory ? !Directory.Exists(full) : !File.Exists(full)) { missing = true; continue; }
                if (directory) continue;
                var bytes = File.ReadAllBytes(full);
                if (path.EndsWith(".meta", StringComparison.Ordinal))
                {
                    if (!string.Equals(PackageOperationFiles.MetaGuid(bytes), (string)entry["guid"], StringComparison.OrdinalIgnoreCase))
                        throw new PackageOperationException("SAMPLE_GUID_MISMATCH", "Imported metadata GUID differs from the approved sample source.");
                }
                else if (PackageOperationFiles.Hash(bytes) != (string)entry["sha256"])
                    throw new PackageOperationException("SAMPLE_READBACK_MISMATCH", "Imported bytes differ from the approved sample source.");
            }
            if (missing) throw new PackageOperationException(awaitingReadback ? "SAMPLE_IMPORT_PENDING" : "SAMPLE_READBACK_MISMATCH", "Approved sample destinations are still missing.");
        }
        internal static JObject Readback(string root, JObject plan, bool awaitingReadback = false)
        {
            ValidateReadbackFiles(root, plan, awaitingReadback);
            var observed = new JArray();
            foreach (var entry in plan["entries"])
            {
                var path = (string)entry["path"]; ValidateAsset(root, path);
                var full = Path.Combine(root, path);
                if ((bool?)entry["directory"] == true)
                {
                    if (!Directory.Exists(full)) throw new PackageOperationException("SAMPLE_READBACK_MISMATCH", "An imported archive directory is missing.");
                    continue;
                }
                if (!File.Exists(full) || (!path.EndsWith(".meta", StringComparison.Ordinal) && PackageOperationFiles.Hash(File.ReadAllBytes(full)) != (string)entry["sha256"]))
                    throw new PackageOperationException("SAMPLE_READBACK_MISMATCH", "Imported bytes differ from the approved sample source.");
                if (path.EndsWith(".meta", StringComparison.Ordinal))
                {
                    var asset = path.Substring(0, path.Length - 5);
                    var guid = AssetDatabase.AssetPathToGUID(asset);
                    if (!string.Equals(guid, (string)entry["guid"], StringComparison.OrdinalIgnoreCase))
                        throw new PackageOperationException("SAMPLE_GUID_MISMATCH", "Imported GUID differs from the approved sample metadata.");
                }
                else
                {
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    if (string.IsNullOrEmpty(guid)) throw new PackageOperationException("SAMPLE_GUID_MISSING", "The imported sample asset has no registered GUID.");
                    observed.Add(new JObject { ["path"] = path, ["guid"] = guid, ["sha256"] = (string)entry["sha256"], ["type"] = AssetDatabase.GetMainAssetTypeAtPath(path)?.FullName });
                }
            }
            return new JObject { ["assets"] = observed, ["inventory"] = DestinationInventory(root, plan) };
        }
        internal static void ValidateAsset(string root, string path)
            => PackageOperationFiles.Require(ProjectPathPolicy.Validate(path, root));
        private static JObject SourceInventory(string source, string packageRoot)
        {
            var parent = Path.GetDirectoryName(source); var relative = Path.GetFileName(source);
            return PackageOperationFiles.Inventory(parent, new[] { relative }, false,
                path =>
                {
                    PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(Path.Combine(parent, path), source));
                    PackageOperationFiles.Require(ProjectPathPolicy.ValidatePackageSource(Path.Combine(parent, path), packageRoot));
                });
        }
        private static JArray FolderEntries(string root, string source, string importPath, JObject inventory)
        {
            ValidateAsset(root, importPath);
            if (!Directory.Exists(source)) throw new PackageOperationException("SAMPLE_SOURCE_MISSING", "The selected sample folder is missing.");
            var prefix = Path.GetFileName(source) + "/";
            var entries = new JArray();
            foreach (var file in ((JObject)inventory["files"]).Properties())
            {
                var suffix = file.Name.Substring(prefix.Length);
                var path = importPath.TrimEnd('/') + "/" + suffix;
                var bytes = File.ReadAllBytes(Path.Combine(source, suffix));
                var entry = Entry(path, bytes);
                if (path.EndsWith(".meta", StringComparison.Ordinal))
                {
                    var guid = PackageOperationFiles.MetaGuid(bytes);
                    if (guid == null) throw new PackageOperationException("SAMPLE_GUID_INVALID", "Source metadata has no valid GUID.");
                    entry["guid"] = guid;
                }
                entries.Add(entry);
            }
            return entries;
        }
        private static JObject Entry(string path, byte[] bytes) => new JObject { ["path"] = path, ["sha256"] = PackageOperationFiles.Hash(bytes), ["length"] = bytes.Length };
        private static JArray ArchiveEntries(string root, string source)
        {
            if (!File.Exists(source)) throw new PackageOperationException("SAMPLE_SOURCE_MISSING", "The selected sample archive is missing.");
            var members = new Dictionary<string, Dictionary<string, byte[]>>(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var input = File.OpenRead(source))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            {
                var header = new byte[512];
                while (ReadBlock(gzip, header))
                {
                    if (header.All(value => value == 0))
                    {
                        var padding = new byte[8192]; int count;
                        while ((count = gzip.Read(padding, 0, padding.Length)) != 0)
                            for (var index = 0; index < count; index++)
                                if (padding[index] != 0) throw new PackageOperationException("ARCHIVE_FORMAT_INVALID", "The archive contains nonzero data after its tar terminator.");
                        break;
                    }
                    var checksum = Octal(header, 148, 8);
                    long sum = 0; for (var index = 0; index < 512; index++) sum += index >= 148 && index < 156 ? 32 : header[index];
                    if (sum != checksum) throw new PackageOperationException("ARCHIVE_MEMBER_UNSAFE", "The archive header checksum is invalid.");
                    var name = Text(header, 0, 100); var prefix = Text(header, 345, 155);
                    if (prefix.Length > 0) name = prefix + "/" + name;
                    var kind = header[156];
                    var pieces = name.TrimEnd('/').Split('/');
                    var attestation = string.Equals(name, "package/.attestation.p7m", StringComparison.Ordinal) && (kind == 0 || kind == '0');
                    if (name.Contains('\\') || pieces.Length < 1 || pieces.Length > 2 || (!attestation && !GuidValid(pieces[0])) || (kind != 0 && kind != '0' && kind != '5'))
                        throw new PackageOperationException("ARCHIVE_MEMBER_UNSAFE", "The archive contains a link, special member, or unsafe member path.");
                    if (!seen.Add(name)) throw new PackageOperationException("ARCHIVE_MEMBER_DUPLICATE", "The archive contains duplicate member identities.");
                    var length = Octal(header, 124, 12);
                    if (length < 0 || length > int.MaxValue) throw new PackageOperationException("ARCHIVE_MEMBER_UNSAFE", "The archive member size is unsupported.");
                    var bytes = new byte[(int)length]; ReadExact(gzip, bytes, bytes.Length);
                    ReadExact(gzip, new byte[(int)((512 - length % 512) % 512)], (int)((512 - length % 512) % 512));
                    // Unity owns signature verification; the envelope is source metadata, never an Assets target.
                    if (attestation) continue;
                    if (kind == '5')
                    {
                        if (pieces.Length != 1 || length != 0) throw new PackageOperationException("ARCHIVE_MEMBER_UNSAFE", "An archive directory member is unsafe.");
                        continue;
                    }
                    if (pieces.Length != 2 || !new[] { "pathname", "asset", "asset.meta", "preview.png" }.Contains(pieces[1]))
                        throw new PackageOperationException("ARCHIVE_MEMBER_UNSAFE", "The archive contains an unrecognized import member.");
                    if (!members.TryGetValue(pieces[0], out var group)) members.Add(pieces[0], group = new Dictionary<string, byte[]>());
                    if (group.ContainsKey(pieces[1])) throw new PackageOperationException("ARCHIVE_MEMBER_DUPLICATE", "The archive contains duplicate GUID members.");
                    group.Add(pieces[1], bytes);
                }
            }
            var entries = new JArray(); var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var member in members.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                var group = member.Value;
                if (!group.TryGetValue("pathname", out var pathname) || !group.TryGetValue("asset.meta", out var meta))
                    throw new PackageOperationException("ARCHIVE_MEMBER_UNSAFE", "Every archive asset needs a pathname and metadata.");
                var path = new UTF8Encoding(false, true).GetString(pathname).TrimEnd('\0', '\r', '\n');
                var validated = ProjectPathPolicy.Validate(path, root);
                if (!validated.Ok || path.Contains('\\') || path.Contains(':') || !path.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new PackageOperationException("ARCHIVE_PATH_UNSAFE", "An archive pathname escapes Assets or is not a safe authoring path.");
                path = validated.Result.Path;
                if (!destinations.Add(path) || path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    throw new PackageOperationException("ARCHIVE_MEMBER_DUPLICATE", "Archive assets overlap a target or metadata path.");
                var guid = PackageOperationFiles.MetaGuid(meta);
                if (guid == null || !string.Equals(guid, member.Key, StringComparison.OrdinalIgnoreCase))
                    throw new PackageOperationException("ARCHIVE_GUID_INVALID", "The archive GUID directory and asset metadata disagree.");
                var folder = Encoding.UTF8.GetString(meta).Contains("folderAsset: yes");
                if (!folder && !group.ContainsKey("asset")) throw new PackageOperationException("ARCHIVE_MEMBER_UNSAFE", "An archive asset payload is missing.");
                var entry = folder ? new JObject { ["path"] = path, ["directory"] = true } : Entry(path, group["asset"]);
                entries.Add(entry);
                var metaEntry = Entry(path + ".meta", meta); metaEntry["guid"] = guid; entries.Add(metaEntry);
            }
            return entries;
        }
        private static void ValidateGuids(string root, JArray entries, bool archive, bool overwrite)
        {
            var guids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            void Scan(string path)
            {
                PackageOperationFiles.Require(ProjectPathPolicy.ValidateReadOnlyDirectory(path, root));
                foreach (var child in Directory.GetFileSystemEntries(Path.Combine(root, path)))
                {
                    var relative = Path.GetRelativePath(root, child).Replace('\\', '/'); ValidateAsset(root, relative);
                    if (Directory.Exists(child)) Scan(relative);
                    else if (relative.EndsWith(".meta", StringComparison.Ordinal))
                    {
                        var guid = PackageOperationFiles.MetaGuid(File.ReadAllBytes(child));
                        if (guid != null)
                        {
                            var asset = relative.Substring(0, relative.Length - 5);
                            if (guids.TryGetValue(guid, out var prior) && prior != asset)
                                throw new PackageOperationException("PROJECT_GUID_AMBIGUOUS", "The project contains duplicate GUID metadata; resolve it before sample import.");
                            guids[guid] = asset;
                        }
                    }
                }
            }
            Scan("Assets");
            var activeProject = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            var usesActiveProject = activeProject != null && string.Equals(Path.GetFullPath(root), Path.GetFullPath(activeProject), ProjectPathPolicy.PathComparison());
            var sourceGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries.Where(item => item["guid"] != null))
            {
                var guid = (string)entry["guid"]; var metaPath = (string)entry["path"]; var asset = metaPath.Substring(0, metaPath.Length - 5);
                if (!sourceGuids.Add(guid)) throw new PackageOperationException("SAMPLE_GUID_AMBIGUOUS", "The sample contains duplicate GUID metadata.");
                if (guids.TryGetValue(guid, out var existing) && existing != asset)
                    throw new PackageOperationException(archive ? "ARCHIVE_GUID_RELOCATION" : "SAMPLE_GUID_RELOCATION", "A source GUID belongs to an existing asset at a different path.");
                var registered = usesActiveProject ? AssetDatabase.GUIDToAssetPath(guid) : "";
                if (!string.IsNullOrEmpty(registered) && registered.Replace('\\', '/') != asset)
                    throw new PackageOperationException(archive ? "ARCHIVE_GUID_RELOCATION" : "SAMPLE_GUID_RELOCATION", "A source GUID belongs to a registered asset at a different path.");
                if (archive && File.Exists(Path.Combine(root, metaPath)))
                {
                    var existingGuid = PackageOperationFiles.MetaGuid(File.ReadAllBytes(Path.Combine(root, metaPath)));
                    if (!string.Equals(existingGuid, guid, StringComparison.OrdinalIgnoreCase))
                        throw new PackageOperationException("ARCHIVE_GUID_COLLISION", "The archive target has different existing GUID metadata.");
                }
            }
        }
        private static bool GuidValid(string value) => System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-fA-F0-9]{32}$");
        private static string Text(byte[] bytes, int offset, int count) => Encoding.UTF8.GetString(bytes, offset, count).TrimEnd('\0');
        private static long Octal(byte[] bytes, int offset, int count)
        {
            var text = Encoding.ASCII.GetString(bytes, offset, count).Trim('\0', ' ');
            if (!System.Text.RegularExpressions.Regex.IsMatch(text, "^[0-7]+$")) throw new PackageOperationException("ARCHIVE_MEMBER_UNSAFE", "Archive numeric fields are invalid.");
            try { return Convert.ToInt64(text, 8); } catch (Exception) { throw new PackageOperationException("ARCHIVE_MEMBER_UNSAFE", "Archive numeric fields exceed supported limits."); }
        }
        private static bool ReadBlock(Stream stream, byte[] bytes)
        {
            var first = stream.ReadByte(); if (first < 0) throw new PackageOperationException("ARCHIVE_MEMBER_UNSAFE", "The archive has no complete terminal block.");
            bytes[0] = (byte)first; var offset = 1;
            while (offset < bytes.Length) { var read = stream.Read(bytes, offset, bytes.Length - offset); if (read == 0) throw new PackageOperationException("ARCHIVE_MEMBER_UNSAFE", "The archive is truncated."); offset += read; }
            return true;
        }
        private static void ReadExact(Stream stream, byte[] bytes, int length)
        {
            var offset = 0;
            while (offset < length) { var read = stream.Read(bytes, offset, length - offset); if (read == 0) throw new PackageOperationException("ARCHIVE_MEMBER_UNSAFE", "The archive member is truncated."); offset += read; }
        }
    }
}
