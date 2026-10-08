using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using UnityEditor;
using UnityEngine;
namespace BatihanDev.UnityCliCommands.Analysis
{
    internal sealed class AssetScanFacts
    {
        internal readonly List<AnalysisAsset> Files = new List<AnalysisAsset>();
        internal readonly List<string> Directories = new List<string>();
        internal readonly List<FolderFact> EmptyFolders = new List<FolderFact>();
    }
    internal static class AssetAnalysisFacts
    {
        internal static CommandResult<AssetScanFacts> Scan(string root)
        {
            const string schema = "unity.analysis.asset-facts@1";
            var project = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            var validation = ProjectPathPolicy.ValidateReadOnlyDirectory(root, project);
            if (!validation.Ok) return CommandResult<AssetScanFacts>.Failure(schema, validation.Error);
            try
            {
                var facts = new AssetScanFacts();
                Visit(validation.Result.Path, true, project, facts);
                return CommandResult<AssetScanFacts>.Success(schema, facts);
            }
            catch (ScanRefusal refusal) { return CommandResult<AssetScanFacts>.Failure(schema, refusal.Error); }
            catch (Exception exception) when (ProjectPathPolicy.IsExpectedPathException(exception))
            { return ProjectPathPolicy.FileSystemFailure<AssetScanFacts>(schema, root, exception); }
        }
        private static bool Visit(string path, bool root, string project, AssetScanFacts facts)
        {
            var validation = ProjectPathPolicy.ValidateReadOnlyDirectory(path, project);
            if (!validation.Ok) throw new ScanRefusal(validation.Error);
            facts.Directories.Add(validation.Result.Path);
            var entries = new DirectoryInfo(System.IO.Path.Combine(project, path)).GetFileSystemInfos()
                .OrderBy(entry => entry.Name, StringComparer.Ordinal).ToArray();
            var hasFile = false;
            var allChildrenEmpty = true;
            var children = 0;
            foreach (var entry in entries)
            {
                var child = path + "/" + entry.Name;
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new ScanRefusal(new CommandError { Schema = "unity.command.error@1", Code = FoundationErrorCode.PathReparsePoint,
                        Message = "Remove linked entries from the scan scope before scanning.", Details = new Dictionary<string, object> { ["path"] = child } });
                if ((entry.Attributes & FileAttributes.Directory) != 0)
                {
                    children++;
                    if (!Visit(child, false, project, facts)) allChildrenEmpty = false;
                }
                else
                {
                    var safe = ProjectPathPolicy.Validate(child);
                    if (!safe.Ok) throw new ScanRefusal(safe.Error);
                    if (entry.Name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                    hasFile = true;
                    facts.Files.Add(Describe(child, ((FileInfo)entry).Length));
                }
            }
            var empty = !hasFile && allChildrenEmpty;
            if (empty) facts.EmptyFolders.Add(new FolderFact { Path = path, Root = root, Leaf = children == 0 });
            return empty;
        }
        internal static AnalysisAsset Describe(string path, long? bytes = null)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            var absolute = System.IO.Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, path);
            return new AnalysisAsset { Path = path, Guid = AssetDatabase.AssetPathToGUID(path),
                Type = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name, Name = asset == null ? System.IO.Path.GetFileName(path) : asset.name,
                SizeBytes = bytes ?? (File.Exists(absolute) ? new FileInfo(absolute).Length : 0) };
        }
        internal static string FilePath(string path) => System.IO.Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, path);
        internal static IEnumerable<AnalysisAsset> Sources(AssetScanFacts facts, string root)
        {
            var files = facts.Files.ToDictionary(asset => asset.Path, StringComparer.Ordinal);
            foreach (var guid in AssetDatabase.FindAssets("t:Object", new[] { root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (files.TryGetValue(path, out var asset)) yield return asset;
            }
        }
        private static readonly HashSet<string> NativeCategories = new HashSet<string>(new[] { "Prefab", "Model", "Script", "Scene" }, StringComparer.OrdinalIgnoreCase);
        internal sealed class AssetFilter
        {
            internal string Query { get; }
            private readonly Type resolvedType;
            internal AssetFilter(string query, Type type) { Query = query; resolvedType = type; }
            internal bool Matches(string path) => resolvedType == null || AssetDatabase.LoadAllAssetsAtPath(path).Any(asset => asset != null && resolvedType.IsAssignableFrom(asset.GetType()));
        }
        internal static AssetFilter Filter(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return new AssetFilter("", null);
            var requested = type.Trim();
            if (NativeCategories.Contains(requested)) return new AssetFilter("t:" + requested, null);
            var resolved = AnalysisSceneObjects.ResolveType(requested, typeof(UnityEngine.Object));
            var assembly = resolved.Assembly.GetName().Name;
            var native = assembly == "UnityEngine" || assembly.StartsWith("UnityEngine.", StringComparison.Ordinal)
                || assembly == "UnityEditor" || assembly.StartsWith("UnityEditor.", StringComparison.Ordinal);
            return new AssetFilter("t:" + (native ? resolved.Name : resolved.FullName), resolved);
        }
        internal static IEnumerable<AnalysisAsset> Global(string type)
        {
            var filter = Filter(type);
            foreach (var guid in AssetDatabase.FindAssets(filter.Query))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!AssetDatabase.IsValidFolder(path) && filter.Matches(path)) yield return Describe(path);
            }
        }
        internal static bool ResourcesPath(string path) => path.Split('/').Contains("Resources", StringComparer.Ordinal);
        internal static IEnumerable<AnalysisAsset> Ordered(AssetScanFacts facts, string root, string type)
        {
            var files = facts.Files.ToDictionary(asset => asset.Path, StringComparer.Ordinal);
            var filter = Filter(type);
            foreach (var guid in AssetDatabase.FindAssets(filter.Query, new[] { root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (files.TryGetValue(path, out var asset) && filter.Matches(path)) yield return asset;
            }
        }
        private sealed class ScanRefusal : Exception
        {
            internal CommandError Error { get; }
            internal ScanRefusal(CommandError error) : base(error.Message) { Error = error; }
        }
    }
}
