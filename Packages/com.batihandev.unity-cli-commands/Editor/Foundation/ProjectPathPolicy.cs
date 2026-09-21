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

        private static CommandResult<ProjectPathResult> ValidateCore(
            string path, string projectRoot, bool allowEmbeddedPackages)
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
            if (underAssets && compact.Count == 1)
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
            if (!fullTarget.StartsWith(allowedPrefix, PathComparison()))
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

            for (var cursor = new DirectoryInfo(fullProjectRoot); cursor != null; cursor = cursor.Parent)
            {
                if ((cursor.Attributes & FileAttributes.ReparsePoint) != 0)
                    return Failure(FoundationErrorCode.PathReparsePoint, "The project root or an ancestor is a reparse point.", path);
            }

            var relativeExisting = Path.GetRelativePath(fullProjectRoot, current);
            var existingSegments = relativeExisting.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var inspected = fullProjectRoot;
            foreach (var segment in existingSegments)
            {
                if (string.IsNullOrEmpty(segment) || segment == ".")
                    continue;
                inspected = Path.Combine(inspected, segment);
                if ((File.GetAttributes(inspected) & FileAttributes.ReparsePoint) != 0)
                    return Failure(
                        FoundationErrorCode.PathReparsePoint,
                        "The path crosses a link, junction, or reparse point.",
                        path);
            }

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

            for (var cursor = new DirectoryInfo(fullRoot); cursor != null; cursor = cursor.Parent)
                if ((cursor.Attributes & FileAttributes.ReparsePoint) != 0)
                    return Failure(FoundationErrorCode.PathReparsePoint,
                        "The project root or an ancestor is a reparse point.", path);

            var inspected = fullRoot;
            foreach (var segment in compact)
            {
                inspected = Path.Combine(inspected, segment);
                if (!File.Exists(inspected) && !Directory.Exists(inspected))
                    break;
                if ((File.GetAttributes(inspected) & FileAttributes.ReparsePoint) != 0)
                    return Failure(FoundationErrorCode.PathReparsePoint,
                        "The path crosses a link, junction, or reparse point.", path);
            }

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
