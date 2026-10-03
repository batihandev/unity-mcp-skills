using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Foundation
{
    public static class ProjectPathPolicy
    {
        public const string Schema = "unity.foundation.path@1";

        public static CommandResult<ProjectPathResult> Validate(string path)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return Validate(path, projectRoot, false);
        }

        public static CommandResult<ProjectPathResult> Validate(string path, bool allowEmbeddedPackages)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return Validate(path, projectRoot, allowEmbeddedPackages);
        }

        internal static CommandResult<ProjectPathResult> Validate(string path, string projectRoot)
        {
            return Validate(path, projectRoot, false);
        }

        internal static CommandResult<ProjectPathResult> Validate(
            string path, string projectRoot, bool allowEmbeddedPackages)
        {
            try
            {
                return ValidateCore(path, projectRoot, allowEmbeddedPackages);
            }
            catch (Exception exception) when (IsExpectedPathException(exception))
            {
                return FileSystemFailure<ProjectPathResult>(Schema, path, exception);
            }
        }

        internal static CommandResult<ProjectPathResult> ValidateReadOnlyDirectory(string path, string projectRoot = null)
        {
            try
            {
                projectRoot = projectRoot ?? Directory.GetParent(Application.dataPath)?.FullName;
                var result = ValidateCore(path, projectRoot, false, true);
                if (!result.Ok) return result;
                var fullPath = Path.Combine(projectRoot, result.Result.Path);
                if (!result.Result.Exists)
                    return Failure(FoundationErrorCode.FileNotFound, "The scan directory was not found.", path);
                if (!Directory.Exists(fullPath))
                    return Failure("PATH_NOT_DIRECTORY", "Select an existing project directory to scan.", path);
                return result;
            }
            catch (Exception exception) when (IsExpectedPathException(exception))
            {
                return FileSystemFailure<ProjectPathResult>(Schema, path, exception);
            }
        }

        private static CommandResult<ProjectPathResult> ValidateCore(
            string path, string projectRoot, bool allowEmbeddedPackages, bool allowAssetsRoot = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Failure(FoundationErrorCode.PathRequired, "A project-relative Assets path is required.", path);

            var normalized = path.Replace('\\', '/');
            if (Path.IsPathRooted(path))
                return Failure(FoundationErrorCode.PathOutsideRoot, "The path must be project-relative under Assets.", path);

            var segments = normalized.Split('/');
            if (segments.Any(segment => string.Equals(segment, "..", StringComparison.Ordinal)))
                return Failure(FoundationErrorCode.PathTraversal, "The path must not contain '..' segments.", path);

            var compact = new List<string>();
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment) || string.Equals(segment, ".", StringComparison.Ordinal))
                    continue;
                compact.Add(segment);
            }

            if (compact.Count == 0)
                return Failure(FoundationErrorCode.PathOutsideRoot, "The path must be under Assets.", path);

            var underAssets = string.Equals(compact[0], "Assets", StringComparison.Ordinal);
            var underPackages = string.Equals(compact[0], "Packages", StringComparison.Ordinal);
            if (!underAssets && (!allowEmbeddedPackages || !underPackages))
                return Failure(FoundationErrorCode.PathOutsideRoot, "The path must be under Assets.", path);
            if (underAssets && compact.Count == 1 && !allowAssetsRoot)
                return Failure(FoundationErrorCode.PathRootForbidden, "The Assets root is not an authoring target.", path);
            if (underPackages && compact.Count < 3)
                return Failure(FoundationErrorCode.PathRootForbidden,
                    "Packages and embedded package roots are not authoring targets.", path);
            if (underPackages && compact.Count == 3 &&
                string.Equals(compact[2], "package.json", StringComparison.OrdinalIgnoreCase))
                return Failure(FoundationErrorCode.PathRootForbidden,
                    "An embedded package manifest is not an authoring target.", path);

            normalized = string.Join("/", compact);
            var fullProjectRoot = Path.GetFullPath(projectRoot);
            var allowedRoot = Path.GetFullPath(Path.Combine(fullProjectRoot, underAssets ? "Assets" : "Packages"));
            var fullTarget = Path.GetFullPath(
                Path.Combine(fullProjectRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
            var allowedPrefix = allowedRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            if (!(allowAssetsRoot && underAssets && string.Equals(fullTarget, allowedRoot, PathComparison())) &&
                !fullTarget.StartsWith(allowedPrefix, PathComparison()))
                return Failure(FoundationErrorCode.PathOutsideRoot,
                    "The canonical path must remain under the selected authoring root.", path);

            if (underPackages)
            {
                var packageRoot = Path.Combine(fullProjectRoot, "Packages", compact[1]);
                var manifest = Path.Combine(packageRoot, "package.json");
                if (!Directory.Exists(packageRoot) || !File.Exists(manifest))
                    return Failure(FoundationErrorCode.PackageNotEmbedded,
                        "The package must be an existing project-contained embedded package.", path);
                if ((File.GetAttributes(packageRoot) & (FileAttributes.ReparsePoint | FileAttributes.ReadOnly)) != 0 ||
                    (File.GetAttributes(manifest) & (FileAttributes.ReparsePoint | FileAttributes.ReadOnly)) != 0)
                    return Failure(FoundationErrorCode.PathNotWritable,
                        "The embedded package root and manifest must not be marked read-only or linked.", path);
            }

            var current = fullTarget;
            while (!File.Exists(current) && !Directory.Exists(current))
            {
                var parent = Directory.GetParent(current);
                if (parent == null)
                    return Failure(FoundationErrorCode.PathOutsideRoot, "The path has no existing parent in the project.", path);
                current = parent.FullName;
            }

            var physical = ValidatePhysicalAncestors(fullProjectRoot, fullTarget, path);
            if (!physical.Ok) return physical;

            return CommandResult<ProjectPathResult>.Success(
                Schema,
                new ProjectPathResult
                {
                    Path = normalized,
                    Exists = File.Exists(fullTarget) || Directory.Exists(fullTarget)
                });
        }

        private static CommandResult<ProjectPathResult> Failure(string code, string message, string path)
        {
            return CommandResult<ProjectPathResult>.Failure(
                Schema,
                code,
                message,
                new Dictionary<string, object> { ["path"] = SafePathDetail(path) });
        }

        internal static CommandResult<ProjectPathResult> ValidateProjectFile(
            string path,
            string projectRoot,
            IReadOnlyCollection<string> allowedExtensions,
            Func<string, Stream> openRead = null)
        {
            try
            {
                return ValidateProjectFileCore(path, projectRoot, allowedExtensions, openRead);
            }
            catch (Exception exception) when (IsExpectedPathException(exception))
            {
                return FileSystemFailure<ProjectPathResult>(Schema, path, exception);
            }
        }

        private static CommandResult<ProjectPathResult> ValidateProjectFileCore(
            string path,
            string projectRoot,
            IReadOnlyCollection<string> allowedExtensions,
            Func<string, Stream> openRead)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Failure(FoundationErrorCode.PathRequired, "A project-relative file path is required.", path);
            if (string.IsNullOrWhiteSpace(projectRoot))
                return Failure(FoundationErrorCode.PathOutsideRoot, "The project root is unavailable.", path);
            if (Path.IsPathRooted(path))
                return Failure(FoundationErrorCode.PathOutsideRoot, "The path must be project-relative.", path);

            var normalized = path.Replace('\\', '/');
            var segments = normalized.Split('/');
            if (segments.Any(segment => string.Equals(segment, "..", StringComparison.Ordinal)))
                return Failure(FoundationErrorCode.PathTraversal, "The path must not contain '..' segments.", path);
            var compact = segments.Where(segment => !string.IsNullOrEmpty(segment) && segment != ".").ToArray();
            if (compact.Length == 0)
                return Failure(FoundationErrorCode.NotRegularFile, "A regular file is required.", path);
            normalized = string.Join("/", compact);

            var possibleDirectory = Path.GetFullPath(Path.Combine(
                Path.GetFullPath(projectRoot), normalized.Replace('/', Path.DirectorySeparatorChar)));
            if (Directory.Exists(possibleDirectory))
                return Failure(FoundationErrorCode.NotRegularFile, "A regular file is required.", path);

            var extension = Path.GetExtension(normalized);
            if (allowedExtensions == null || !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return Failure(FoundationErrorCode.CaptureExtensionUnsupported,
                    "The capture extension must be .raw or .data.", path);

            var fullRoot = Path.GetFullPath(projectRoot).TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullTarget = Path.GetFullPath(Path.Combine(
                fullRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
            var rootPrefix = fullRoot + Path.DirectorySeparatorChar;
            if (!fullTarget.StartsWith(rootPrefix, PathComparison()))
                return Failure(FoundationErrorCode.PathOutsideRoot,
                    "The canonical path must remain under the project root.", path);

            var physical = ValidatePhysicalAncestors(fullRoot, fullTarget, path);
            if (!physical.Ok) return physical;

            if (Directory.Exists(fullTarget))
                return Failure(FoundationErrorCode.NotRegularFile, "A regular file is required.", path);
            if (!File.Exists(fullTarget))
                return Failure(FoundationErrorCode.FileNotFound, "The capture file was not found.", path);

            using ((openRead ?? OpenRead)(fullTarget))
            {
            }

            return CommandResult<ProjectPathResult>.Success(
                Schema,
                new ProjectPathResult { Path = normalized, Exists = true });
        }

        internal const string PackageOperationDirectory = "Library/unity-cli-package-operations";

        internal static CommandResult<ProjectPathResult> ValidatePackageOperationFile(string path, string projectRoot)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(path ?? "", "^" + System.Text.RegularExpressions.Regex.Escape(PackageOperationDirectory) + @"/[a-f0-9]{32}\.json(?:\.[a-f0-9]{32}\.tmp)?$"))
                return Failure(FoundationErrorCode.PathOutsideRoot, "Select an exact owned package-operation journal.", path);
            return ValidatePhysicalAncestors(projectRoot, Path.Combine(projectRoot, path), path);
        }

        internal static CommandResult<ProjectPathResult> ValidatePackageRoot(string name, string projectRoot)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(name ?? "", @"^[a-z0-9]+(?:[.-][a-z0-9]+)+$"))
                return Failure(FoundationErrorCode.PathInvalid, "Select an exact UPM package name.", name);
            var reportedPath = "Packages/" + name;
            try
            {
                var parent = Path.Combine(Path.GetFullPath(projectRoot), "Packages");
                var physical = ValidatePhysicalAncestors(projectRoot, parent, reportedPath);
                if (!physical.Ok) return physical;
                var exists = false;
                if (physical.Result.Exists)
                {
                    // Unity VFS maps logical registry roots into the cache; physical parent entries own Embed destinations.
                    foreach (var entry in new DirectoryInfo(parent).GetFileSystemInfos())
                    {
                        if (!string.Equals(entry.Name, name, PathComparison())) continue;
                        if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                            return Failure(FoundationErrorCode.PathReparsePoint,
                                "The path crosses a link, junction, or reparse point.", reportedPath);
                        exists = true;
                        break;
                    }
                }
                return CommandResult<ProjectPathResult>.Success(Schema,
                    new ProjectPathResult { Path = reportedPath, Exists = exists });
            }
            catch (Exception exception) when (IsExpectedPathException(exception))
            { return FileSystemFailure<ProjectPathResult>(Schema, reportedPath, exception); }
        }

        internal static CommandResult<ProjectPathResult> ValidatePackageSource(string path, string packageRoot)
        {
            try
            {
                var root = Path.GetFullPath(packageRoot);
                var full = Path.GetFullPath(path);
                if (!string.Equals(root, full, PathComparison()) &&
                    !full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, PathComparison()))
                    return Failure(FoundationErrorCode.PathOutsideRoot, "The source must remain inside the exact resolved package.", "<package-source>");
                return ValidatePhysicalAncestors(root, full, "<package-source>");
            }
            catch (Exception exception) when (IsExpectedPathException(exception))
            { return FileSystemFailure<ProjectPathResult>(Schema, "<package-source>", exception); }
        }

        private static CommandResult<ProjectPathResult> ValidatePhysicalAncestors(string root, string target, string reportedPath)
        {
            try
            {
                root = Path.GetFullPath(root);
                target = Path.GetFullPath(target);
                for (var cursor = new DirectoryInfo(root); cursor != null; cursor = cursor.Parent)
                {
                    if ((cursor.Exists || File.Exists(cursor.FullName)) &&
                        (File.GetAttributes(cursor.FullName) & FileAttributes.ReparsePoint) != 0)
                        return Failure(FoundationErrorCode.PathReparsePoint, "The root or an ancestor is linked.", reportedPath);
                }
                var inspected = root;
                foreach (var segment in Path.GetRelativePath(root, target).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                {
                    if (string.IsNullOrEmpty(segment) || segment == ".") continue;
                    if (segment == "..") return Failure(FoundationErrorCode.PathOutsideRoot, "The target escapes its selected root.", reportedPath);
                    inspected = Path.Combine(inspected, segment);
                    // GetAttributes also observes dangling links that Exists omits.
                    FileAttributes attributes;
                    try { attributes = File.GetAttributes(inspected); }
                    catch (FileNotFoundException) { break; }
                    catch (DirectoryNotFoundException) { break; }
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                        return Failure(FoundationErrorCode.PathReparsePoint, "The path crosses a link, junction, or reparse point.", reportedPath);
                }
                return CommandResult<ProjectPathResult>.Success(Schema,
                    new ProjectPathResult { Path = reportedPath, Exists = File.Exists(target) || Directory.Exists(target) });
            }
            catch (Exception exception) when (IsExpectedPathException(exception))
            { return FileSystemFailure<ProjectPathResult>(Schema, reportedPath, exception); }
        }

        internal static CommandResult<T> FileSystemFailure<T>(string schema, string path, Exception exception)
        {
            var code = FileSystemErrorCode(exception);
            var message = code == FoundationErrorCode.PathInvalid
                ? "The path format is invalid."
                : code == FoundationErrorCode.PathAccessDenied
                    ? "Access to the path was denied."
                    : code == FoundationErrorCode.FileNotFound
                        ? "The file was not found."
                        : "The filesystem operation failed.";
            return CommandResult<T>.Failure(schema, code, message,
                new Dictionary<string, object> { ["path"] = SafePathDetail(path) });
        }

        internal static bool IsExpectedPathException(Exception exception)
        {
            return exception is ArgumentException || exception is NotSupportedException ||
                   exception is PathTooLongException || exception is UnauthorizedAccessException ||
                   exception is SecurityException || exception is IOException;
        }

        private static string FileSystemErrorCode(Exception exception)
        {
            if (exception is FileNotFoundException || exception is DirectoryNotFoundException)
                return FoundationErrorCode.FileNotFound;
            if (exception is UnauthorizedAccessException || exception is SecurityException)
                return FoundationErrorCode.PathAccessDenied;
            if (exception is ArgumentException || exception is NotSupportedException ||
                exception is PathTooLongException)
                return FoundationErrorCode.PathInvalid;
            return FoundationErrorCode.PathIoError;
        }

        private static string SafePathDetail(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            if (path.IndexOf('\0') >= 0)
                return "<invalid-path>";
            try
            {
                return Path.IsPathRooted(path) ? "<rooted-path>" : path.Replace('\\', '/');
            }
            catch (Exception exception) when (IsExpectedPathException(exception))
            {
                return "<invalid-path>";
            }
        }

        private static Stream OpenRead(string path)
        {
            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        internal static StringComparison PathComparison()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
        }
    }

    [Serializable]
    public sealed class ProjectPathResult
    {
        public string Path { get; set; }
        public bool Exists { get; set; }
    }
}
