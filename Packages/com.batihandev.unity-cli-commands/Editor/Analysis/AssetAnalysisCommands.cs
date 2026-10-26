using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BatihanDev.UnityCliCommands.Foundation;
using UnityEditor;
using Unity.Pipeline.Commands;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class AssetAnalysisCommands
    {
        private const string RuntimeNote = "Dependency evidence cannot establish runtime or Addressables use; review runtime loading before removal.";
        [CliCommand("analysis.assets-large", "Report complete scoped file sizes with explicit threshold and ordering policies.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<AssetListReport> Large(
            [CliArg("searchPath", "Existing Assets scan directory.")] string searchPath = "Assets",
            [CliArg("limit", "Maximum shown assets, nonnegative.")] int limit = 20,
            [CliArg("minSizeBytes", "Nonnegative byte threshold.")] long minSizeBytes = 0,
            [CliArg("inclusive", "Include assets equal to the byte threshold.")] bool inclusive = false,
            [CliArg("sort", "Sort largest first.")] bool sort = true,
            [CliArg("assetType", "Unique asset type, empty for all files.")] string assetType = "",
            [CliArg("assetDatabaseOrder", "Use AssetDatabase discovery order before optional sorting.")] bool assetDatabaseOrder = false)
        {
            const string schema = "unity.analysis.assets-large@1";
            return Run(schema, () =>
            {
                if (limit < 0 || minSizeBytes < 0) return Invalid<AssetListReport>(schema);
                var scan = AssetAnalysisFacts.Scan(searchPath);
                if (!scan.Ok) return CommandResult<AssetListReport>.Failure(schema, scan.Error);
                IEnumerable<AnalysisAsset> assets = assetDatabaseOrder || !string.IsNullOrWhiteSpace(assetType)
                    ? AssetAnalysisFacts.Ordered(scan.Result, searchPath, assetType) : scan.Result.Files;
                assets = assets.Where(asset => inclusive ? asset.SizeBytes >= minSizeBytes : asset.SizeBytes > minSizeBytes);
                if (sort) assets = assets.OrderByDescending(asset => asset.SizeBytes).ThenBy(asset => asset.Path, StringComparer.Ordinal);
                var shown = assets.ToList();
                foreach (var asset in shown) asset.Name = System.IO.Path.GetFileName(asset.Path);
                return List(schema, shown, limit);
            });
        }
        [CliCommand("analysis.assets-duplicates", "Find exact byte duplicates with source identities and wasted bytes.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<AssetDuplicateReport> Duplicates(
            [CliArg("assetType", "Unique asset type; default Texture2D.")] string assetType = "Texture2D",
            [CliArg("searchPath", "Existing Assets scan directory.")] string searchPath = "Assets",
            [CliArg("limit", "Maximum duplicate groups.")] int limit = 50)
        {
            const string schema = "unity.analysis.assets-duplicates@1";
            return Run(schema, () =>
            {
                if (limit < 0) return Invalid<AssetDuplicateReport>(schema);
                var scan = AssetAnalysisFacts.Scan(searchPath);
                if (!scan.Ok) return CommandResult<AssetDuplicateReport>.Failure(schema, scan.Error);
                var groups = new List<AssetDuplicateGroup>();
                using (var hash = SHA256.Create())
                {
                    foreach (var sizeGroup in AssetAnalysisFacts.Ordered(scan.Result, searchPath, assetType).GroupBy(asset => asset.SizeBytes).Where(group => group.Count() > 1))
                    {
                        var hashes = new Dictionary<string, List<AnalysisAsset>>(StringComparer.Ordinal);
                        foreach (var asset in sizeGroup)
                        {
                            string digest;
                            using (var stream = File.OpenRead(AssetAnalysisFacts.FilePath(asset.Path))) digest = BitConverter.ToString(hash.ComputeHash(stream));
                            if (!hashes.TryGetValue(digest, out var rows)) hashes[digest] = rows = new List<AnalysisAsset>();
                            rows.Add(asset);
                        }
                        foreach (var candidates in hashes.Values.Where(rows => rows.Count > 1))
                        {
                            var partitions = new List<List<AnalysisAsset>>();
                            foreach (var asset in candidates)
                            {
                                var equal = partitions.FirstOrDefault(part => EqualBytes(part[0].Path, asset.Path));
                                if (equal == null) partitions.Add(new List<AnalysisAsset> { asset }); else equal.Add(asset);
                            }
                            foreach (var partition in partitions.Where(part => part.Count > 1))
                                groups.Add(new AssetDuplicateGroup { Files = partition, SizeBytes = sizeGroup.Key, WastedBytes = sizeGroup.Key * (partition.Count - 1L) });
                        }
                    }
                }
                return CommandResult<AssetDuplicateReport>.Success(schema, new AssetDuplicateReport { Groups = groups.Take(limit).ToList(), Total = groups.Count,
                    TotalWastedBytes = groups.Sum(group => group.WastedBytes), Truncated = groups.Count > limit });
            });
        }
        [CliCommand("analysis.asset-folders", "Report recursively empty leaves and trees in ordinal sibling postorder.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<FolderReport> Folders([CliArg("searchPath", "Existing Assets scan directory.")] string searchPath = "Assets")
        {
            const string schema = "unity.analysis.asset-folders@1";
            return Run(schema, () => { var scan = AssetAnalysisFacts.Scan(searchPath); return scan.Ok
                ? CommandResult<FolderReport>.Success(schema, new FolderReport { Folders = scan.Result.EmptyFolders })
                : CommandResult<FolderReport>.Failure(schema, scan.Error); });
        }
        [CliCommand("analysis.assets-unused", "Find scoped dependency candidates excluding Resources sources and candidates.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<AssetListReport> Unused(
            [CliArg("assetType", "Unique asset type.")] string assetType = "Material",
            [CliArg("searchPath", "Existing Assets scan directory.")] string searchPath = "Assets",
            [CliArg("limit", "Maximum shown candidates.")] int limit = 100,
            [CliArg("globalCandidates", "Include all project and package candidates; dependency sources remain Assets-only.")] bool globalCandidates = false,
            [CliArg("excludeResourcesCandidates", "Exclude candidates in Resources folders.")] bool excludeResourcesCandidates = true,
            [CliArg("excludeResourcesSources", "Exclude dependency sources in Resources folders.")] bool excludeResourcesSources = true)
        {
            const string schema = "unity.analysis.assets-unused@1";
            return Run(schema, () =>
            {
                if (limit < 0) return Invalid<AssetListReport>(schema);
                var sourceRoot = globalCandidates ? "Assets" : searchPath;
                var scan = AssetAnalysisFacts.Scan(sourceRoot); if (!scan.Ok) return CommandResult<AssetListReport>.Failure(schema, scan.Error);
                IEnumerable<AnalysisAsset> discovered = globalCandidates
                    ? AssetAnalysisFacts.Global(assetType)
                    : AssetAnalysisFacts.Ordered(scan.Result, searchPath, assetType);
                var candidates = discovered.Where(asset => !excludeResourcesCandidates || !AssetAnalysisFacts.ResourcesPath(asset.Path)).ToList();
                var referenced = new HashSet<string>(StringComparer.Ordinal);
                foreach (var source in AssetAnalysisFacts.Sources(scan.Result, sourceRoot).Where(asset => !excludeResourcesSources || !AssetAnalysisFacts.ResourcesPath(asset.Path)))
                    foreach (var dependency in AssetDatabase.GetDependencies(source.Path, true))
                        if (!string.Equals(dependency, source.Path, StringComparison.Ordinal)) referenced.Add(dependency);
                return List(schema, candidates.Where(asset => !referenced.Contains(asset.Path)).ToList(), limit, RuntimeNote);
            });
        }
        [CliCommand("analysis.asset-usage", "Report direct reverse references within Assets with pre-cap totals.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<AssetListReport> Usage(
            [CliArg("assetPath", "Exact existing source asset file.", Required = true)] string assetPath,
            [CliArg("limit", "Maximum shown usages.")] int limit = 50)
        {
            const string schema = "unity.analysis.asset-usage@1";
            return Run(schema, () =>
            {
                if (limit < 0) return Invalid<AssetListReport>(schema);
                var target = Source(assetPath, false); if (!target.Ok) return CommandResult<AssetListReport>.Failure(schema, target.Error);
                var scan = AssetAnalysisFacts.Scan("Assets"); if (!scan.Ok) return CommandResult<AssetListReport>.Failure(schema, scan.Error);
                var rows = AssetAnalysisFacts.Sources(scan.Result, "Assets").Where(source => source.Path != target.Result.Path &&
                    AssetDatabase.GetDependencies(source.Path, false).Contains(target.Result.Path, StringComparer.Ordinal)).ToList();
                return List(schema, rows, limit, RuntimeNote, AssetAnalysisFacts.Describe(target.Result.Path));
            });
        }
        [CliCommand("analysis.asset-dependencies", "Report direct or recursive dependencies of an existing file or folder.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<AssetListReport> Dependencies(
            [CliArg("assetPath", "Exact existing source file or folder.", Required = true)] string assetPath,
            [CliArg("recursive", "Include transitive dependencies.")] bool recursive = true)
        {
            const string schema = "unity.analysis.asset-dependencies@1";
            return Run(schema, () =>
            {
                var source = Source(assetPath, true); if (!source.Ok) return CommandResult<AssetListReport>.Failure(schema, source.Error);
                if (Directory.Exists(AssetAnalysisFacts.FilePath(source.Result.Path)))
                {
                    var scan = AssetAnalysisFacts.Scan(source.Result.Path);
                    if (!scan.Ok) return CommandResult<AssetListReport>.Failure(schema, scan.Error);
                }
                var dependencies = new List<AnalysisAsset>();
                foreach (var path in AssetDatabase.GetDependencies(source.Result.Path, recursive).Where(path => path != source.Result.Path))
                {
                    if (path.StartsWith("Assets/", StringComparison.Ordinal))
                    {
                        var valid = ProjectPathPolicy.Validate(path); if (!valid.Ok) return CommandResult<AssetListReport>.Failure(schema, valid.Error);
                    }
                    dependencies.Add(AssetAnalysisFacts.Describe(path));
                }
                return List(schema, dependencies, int.MaxValue, source: AssetAnalysisFacts.Describe(source.Result.Path));
            });
        }
        private static CommandResult<ProjectPathResult> Source(string path, bool directory)
        {
            var result = directory && string.Equals(path?.Replace('\\', '/').TrimEnd('/'), "Assets", StringComparison.Ordinal)
                ? ProjectPathPolicy.ValidateReadOnlyDirectory(path) : ProjectPathPolicy.Validate(path);
            if (!result.Ok) return result;
            if (!result.Result.Exists) return CommandResult<ProjectPathResult>.Failure(result.Schema, FoundationErrorCode.FileNotFound, "Select an existing source asset.");
            if (!directory && Directory.Exists(AssetAnalysisFacts.FilePath(result.Result.Path))) return CommandResult<ProjectPathResult>.Failure(result.Schema, FoundationErrorCode.NotRegularFile, "Select a source asset file.");
            if (directory && Directory.Exists(AssetAnalysisFacts.FilePath(result.Result.Path))) return ProjectPathPolicy.ValidateReadOnlyDirectory(result.Result.Path);
            return result;
        }
        private static bool EqualBytes(string a, string b)
        {
            using (var left = File.OpenRead(AssetAnalysisFacts.FilePath(a))) using (var right = File.OpenRead(AssetAnalysisFacts.FilePath(b)))
            {
                if (left.Length != right.Length) return false;
                var lb = new byte[8192]; var rb = new byte[8192];
                while (true) { var n = left.Read(lb, 0, lb.Length); var m = right.Read(rb, 0, rb.Length); if (n != m) return false; if (n == 0) return true; for (var i = 0; i < n; i++) if (lb[i] != rb[i]) return false; }
            }
        }
        private static CommandResult<AssetListReport> List(string schema, List<AnalysisAsset> assets, int limit, string note = null, AnalysisAsset source = null) =>
            CommandResult<AssetListReport>.Success(schema, new AssetListReport { Assets = assets.Take(limit).ToList(), Total = assets.Count, Truncated = assets.Count > limit, Note = note, Source = source });
        private static CommandResult<T> Invalid<T>(string schema) => CommandResult<T>.Failure(schema, "ANALYSIS_INPUT_INVALID", "Use finite nonnegative thresholds and limits.");
        private static CommandResult<T> Run<T>(string schema, Func<CommandResult<T>> operation)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled(); if (!compatibility.Ok) return CommandResult<T>.Failure(schema, compatibility.Error);
            try { return operation(); }
            catch (ArgumentException exception) { return CommandResult<T>.Failure(schema, "ANALYSIS_TYPE_INVALID", exception.Message); }
            catch (Exception exception) when (ProjectPathPolicy.IsExpectedPathException(exception)) { return ProjectPathPolicy.FileSystemFailure<T>(schema, "Assets", exception); }
            catch (Exception exception) { return CommandResult<T>.Failure(schema, "ANALYSIS_READ_FAILED", "The analysis failed; inspect the reported error before retrying.", new Dictionary<string, object> { ["exceptionType"] = exception.GetType().Name, ["message"] = exception.Message }); }
        }
    }
}
