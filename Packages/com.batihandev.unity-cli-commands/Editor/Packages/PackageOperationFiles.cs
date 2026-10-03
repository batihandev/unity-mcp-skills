using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
#if UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
#endif
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BatihanDev.UnityCliCommands.Packages
{
    internal sealed class PackageOperationException : Exception
    {
        public string Code { get; }
        internal PackageOperationException(string code, string message) : base(message) { Code = code; }
    }

    internal static class PackageOperationFiles
    {
        internal const string JournalSchema = "unity.package.operation@1";
        internal static string Hash(byte[] bytes)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        internal static string HashJson(JToken value) => Hash(Encoding.UTF8.GetBytes(value.ToString(Formatting.None)));
        internal static void Require(CommandResult<ProjectPathResult> result)
        {
            if (!result.Ok) throw new PackageOperationException(result.Error.Code, result.Error.Message);
        }
        internal static string JournalRelativePath(string id) => ProjectPathPolicy.PackageOperationDirectory + "/" + id + ".json";
        internal static string JournalDirectory(string root) => Path.Combine(root, ProjectPathPolicy.PackageOperationDirectory);
        internal static string JournalPath(string root, string id)
        {
            var relative = JournalRelativePath(id);
            Require(ProjectPathPolicy.ValidatePackageOperationFile(relative, root));
            return Path.Combine(root, relative);
        }
        internal static string Publish(string root, JObject journal, string expected = null)
        {
            var target = JournalPath(root, (string)journal["operationId"]);
            if (File.Exists(target) && (expected == null || Hash(File.ReadAllBytes(target)) != expected))
                throw new PackageOperationException("JOURNAL_CHANGED", "The owned journal changed; preserve it and inspect recovery evidence.");
            if (!File.Exists(target) && expected != null)
                throw new PackageOperationException("JOURNAL_CHANGED", "The owned journal disappeared; preserve recovery evidence.");
            var relativeTemp = JournalRelativePath((string)journal["operationId"]) + "." + Guid.NewGuid().ToString("N") + ".tmp";
            Require(ProjectPathPolicy.ValidatePackageOperationFile(relativeTemp, root));
            var temp = Path.Combine(root, relativeTemp);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            var bytes = new UTF8Encoding(false).GetBytes(journal.ToString(Formatting.None));
            try
            {
                Require(ProjectPathPolicy.ValidatePackageOperationFile(relativeTemp, root));
                using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                Require(ProjectPathPolicy.ValidatePackageOperationFile(JournalRelativePath((string)journal["operationId"]), root));
                PublishStaged(temp, target, expected);
                return Hash(bytes);
            }
            finally { if (File.Exists(temp)) { Require(ProjectPathPolicy.ValidatePackageOperationFile(relativeTemp, root)); File.Delete(temp); } }
        }
        internal static void PublishStaged(string temp, string target, string expected)
        {
            if (expected == null)
            {
                if (File.Exists(target)) throw new PackageOperationException("JOURNAL_CHANGED", "A journal appeared during initial publication.");
                File.Move(temp, target);
                return;
            }
            try
            {
                if (!File.Exists(target) || Hash(File.ReadAllBytes(target)) != expected)
                    throw new PackageOperationException("JOURNAL_CHANGED", "The journal changed or disappeared during publication.");
#if UNITY_EDITOR_WIN
                ReplaceExistingWindows(temp, target);
#else
                File.Replace(temp, target, null);
#endif
            }
            catch (IOException exception) { throw new PackageOperationException("JOURNAL_CHANGED", "Existing journal replacement failed; preserve recovery evidence: " + exception.Message); }
        }
#if UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct FileRenameInformation
        {
            public uint Flags;
            public IntPtr RootDirectory;
            public uint FileNameLength;
            public char FileName;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(string path, uint access, uint share,
            IntPtr securityAttributes, uint creationDisposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int informationClass,
            IntPtr information, uint size);
        private static void ReplaceExistingWindows(string temp, string target)
        {
            const uint deleteAccess = 0x00010000;
            const uint shareReadWriteDelete = 7;
            const uint openExisting = 3;
            const uint normalAttributes = 0x80;
            const int fileRenameInfoEx = 22;
            const int replaceIfExists = 1;
            const int posixSemantics = 2;
            var name = Encoding.Unicode.GetBytes(Path.GetFullPath(target));
            var type = typeof(FileRenameInformation);
            var nameOffset = (int)Marshal.OffsetOf(type, nameof(FileRenameInformation.FileName));
            var size = checked(nameOffset + name.Length + 2);
            var information = Marshal.AllocHGlobal(size);
            try
            {
                for (var index = 0; index < size; index++) Marshal.WriteByte(information, index, 0);
                Marshal.WriteInt32(information, (int)Marshal.OffsetOf(type, nameof(FileRenameInformation.Flags)), replaceIfExists | posixSemantics);
                Marshal.WriteIntPtr(information, (int)Marshal.OffsetOf(type, nameof(FileRenameInformation.RootDirectory)), IntPtr.Zero);
                Marshal.WriteInt32(information, (int)Marshal.OffsetOf(type, nameof(FileRenameInformation.FileNameLength)), name.Length);
                Marshal.Copy(name, 0, IntPtr.Add(information, nameOffset), name.Length);
                using (var handle = CreateFileW(temp, deleteAccess, shareReadWriteDelete, IntPtr.Zero, openExisting, normalAttributes, IntPtr.Zero))
                {
                    if (handle.IsInvalid)
                        throw new IOException("Could not open staged journal for replacement; Win32 error=" + Marshal.GetLastWin32Error());
                    if (!SetFileInformationByHandle(handle, fileRenameInfoEx, information, (uint)size))
                        throw new IOException("Could not replace existing journal; Win32 error=" + Marshal.GetLastWin32Error());
                }
            }
            finally { Marshal.FreeHGlobal(information); }
        }
#endif
        internal static JObject FileState(string root, string path, bool bytes = true)
        {
            var full = Path.Combine(root, path);
            if (!File.Exists(full)) return new JObject { ["exists"] = false };
            var data = File.ReadAllBytes(full);
            var state = new JObject { ["exists"] = true, ["sha256"] = Hash(data), ["length"] = data.Length };
            if (bytes) state["bytesBase64"] = Convert.ToBase64String(data);
            return state;
        }
        internal static JObject Inventory(string root, string[] paths, bool bytes, Action<string> validate)
        {
            var files = new JObject(); var directories = new JArray();
            foreach (var path in paths.Distinct(StringComparer.Ordinal).OrderBy(item => item, StringComparer.Ordinal))
                Collect(root, path, bytes, validate, files, directories);
            return new JObject { ["files"] = files, ["directories"] = new JArray(directories.Values<string>().Distinct().OrderBy(value => value, StringComparer.Ordinal)) };
        }
        private static void Collect(string root, string path, bool bytes, Action<string> validate, JObject files, JArray directories)
        {
            validate(path);
            var full = Path.Combine(root, path);
            if (File.Exists(full)) { files[path] = FileState(root, path, bytes); return; }
            if (!Directory.Exists(full)) return;
            directories.Add(path);
            foreach (var child in Directory.GetFileSystemEntries(full).OrderBy(item => item, StringComparer.Ordinal))
                Collect(root, Path.GetRelativePath(root, child).Replace('\\', '/'), bytes, validate, files, directories);
        }
        internal static bool SameInventory(JObject left, JObject right)
        {
            var l = (JObject)left.DeepClone(); var r = (JObject)right.DeepClone();
            foreach (var inventory in new[] { l, r })
                foreach (var state in ((JObject)inventory["files"]).Properties()) ((JObject)state.Value).Remove("bytesBase64");
            return JToken.DeepEquals(l, r);
        }
        internal static void RestoreInventory(string root, JObject before, JObject after, Action<string> validate)
        {
            var prior = (JObject)before["files"]; var current = (JObject)after["files"];
            foreach (var item in current.Properties())
            {
                validate(item.Name);
                if (!JToken.DeepEquals(FileState(root, item.Name, false), WithoutBytes((JObject)item.Value)))
                    throw new PackageOperationException("RECOVERY_HASH_MISMATCH", "A captured destination file changed; preserve external changes and the journal.");
            }
            foreach (var item in current.Properties().OrderByDescending(item => item.Name.Length))
                if (prior[item.Name] == null) { validate(item.Name); File.Delete(Path.Combine(root, item.Name)); }
            foreach (var directory in before["directories"].Values<string>().OrderBy(value => value.Length))
            { validate(directory); Directory.CreateDirectory(Path.Combine(root, directory)); }
            foreach (var item in prior.Properties())
            {
                validate(item.Name);
                var full = Path.Combine(root, item.Name); Directory.CreateDirectory(Path.GetDirectoryName(full));
                validate(item.Name); File.WriteAllBytes(full, Convert.FromBase64String((string)item.Value["bytesBase64"]));
            }
            var preserved = before["directories"].Values<string>().ToHashSet(StringComparer.Ordinal);
            foreach (var directory in after["directories"].Values<string>().OrderByDescending(value => value.Length))
                if (!preserved.Contains(directory))
                {
                    validate(directory); var full = Path.Combine(root, directory);
                    if (Directory.Exists(full) && Directory.GetFileSystemEntries(full).Length == 0) Directory.Delete(full);
                }
        }
        private static JObject WithoutBytes(JObject state) { var copy = (JObject)state.DeepClone(); copy.Remove("bytesBase64"); return copy; }
        internal static string MetaGuid(byte[] bytes)
        {
            var match = System.Text.RegularExpressions.Regex.Match(Encoding.UTF8.GetString(bytes), @"(?m)^guid:\s*([a-fA-F0-9]{32})\s*$");
            return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
        }
    }
}
