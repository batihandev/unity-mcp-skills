using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Probing
{
    public static class ProbeCompilationCommands
    {
        [CliCommand("cli_compile_probes", "Compile each hash-bound source independently through native run_script dry_run. No probe assembly is loaded or executed.", MainThreadRequired = true, Tags = new[] { "scripts/eval", "unity-cli-commands" })]
        public static async Task<JObject> Compile(
            [CliArg("manifest", "Absolute path to the host-staged source manifest.", Required = true)] string manifest,
            [CliArg("manifest_sha256", "Expected SHA-256 of the manifest bytes.", Required = true)] string manifestSha256,
            [CliArg("time_budget_ms", "Whole catalogue compilation deadline, checked between sources.")] int timeBudgetMs = 45000)
        {
            if (timeBudgetMs <= 0 || timeBudgetMs > 600000) throw new ArgumentException("Invalid compilation budget.");
            var timer = Stopwatch.StartNew();
            manifest = AbsolutePath(manifest);
            var bytes = File.ReadAllBytes(manifest);
            if (Hash(bytes) != manifestSha256) throw new ArgumentException("Manifest changed.");
            var document = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            if ((int?)document["schemaVersion"] != 1) throw new ArgumentException("Unsupported manifest schema.");
            var sources = document["sources"] as JArray;
            if (sources == null || sources.Count == 0 || sources.Count > 2000) throw new ArgumentException("Select 1 to 2000 sources.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in sources)
                if (source is not JObject || !ids.Add((string)source["id"] ?? "") || string.IsNullOrWhiteSpace((string)source["id"]) ||
                    string.IsNullOrWhiteSpace((string)source["path"]) || !((string)source["path"]).EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                    !System.Text.RegularExpressions.Regex.IsMatch((string)source["sha256"] ?? "", "^[a-f0-9]{64}$"))
                    throw new ArgumentException("Invalid or duplicate source identity.");
            var matches = CommandRegistry.DiscoverCommands().Where(c => c.Name == "run_script").ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Native run_script is unavailable or ambiguous.");
            var command = matches[0];
            Require(command, "file", typeof(string));
            Require(command, "mode", typeof(string));
            Require(command, "dry_run", typeof(bool));
            Require(command, "timeout_ms", typeof(int));
            if (!command.MainThreadRequired) throw new InvalidOperationException("Native run_script thread contract changed.");
            var items = new JArray();
            var identity = EditorIdentity();
            var initialPlaying = EditorApplication.isPlaying;
            var initialPaused = EditorApplication.isPaused;
            foreach (var source in sources)
            {
                var item = new JObject { ["id"] = source["id"].DeepClone(), ["sha256"] = source["sha256"].DeepClone() };
                items.Add(item);
                if (timer.ElapsedMilliseconds >= timeBudgetMs)
                {
                    item["result"] = Failure("Batch Deadline", "The catalogue deadline expired before this source was compiled.");
                    continue;
                }
                try
                {
                    var path = AbsolutePath((string)source["path"]);
                    if (Hash(File.ReadAllBytes(path)) != (string)source["sha256"])
                        throw new InvalidOperationException("Staged source changed before compilation.");
                    if (timer.ElapsedMilliseconds >= timeBudgetMs)
                    {
                        item["result"] = Failure("Batch Deadline", "The catalogue deadline expired while reading this source.");
                        continue;
                    }
                    var values = command.Parameters.Select(p => p.DefaultValue).ToArray();
                    for (var i = 0; i < values.Length; ++i)
                    {
                        var parameter = command.Parameters[i];
                        switch (parameter.Name)
                        {
                            case "file": values[i] = path; break;
                            case "mode": values[i] = "ephemeral"; break;
                            case "dry_run": values[i] = true; break;
                            case "timeout_ms": values[i] = Math.Max(1, timeBudgetMs - (int)timer.ElapsedMilliseconds); break;
                            default:
                                if (parameter.Required) throw new InvalidOperationException("Unknown required native parameter: " + parameter.Name);
                                break;
                        }
                    }
                    var invocation = command.Method.Invoke(command.Target, values);
                    if (invocation is Task task)
                    {
                        await task;
                        invocation = task.GetType().GetProperty("Result")?.GetValue(task);
                    }
                    if (invocation == null) throw new InvalidOperationException("Native compilation returned no result.");
                    item["result"] = JToken.FromObject(invocation);
                    if (Hash(File.ReadAllBytes(path)) != (string)source["sha256"])
                        throw new InvalidOperationException("Staged source changed during compilation.");
                }
                catch (Exception error)
                {
                    var failure = Failure("Compile Check Failed", error.GetBaseException().Message);
                    if (item["result"] != null) failure["nativeResult"] = item["result"].DeepClone();
                    item["result"] = failure;
                }
            }
            if (identity.ToString() != EditorIdentity().ToString() || initialPlaying != EditorApplication.isPlaying || initialPaused != EditorApplication.isPaused)
                throw new InvalidOperationException("Editor identity or play state changed during compilation.");
            return new JObject { ["schemaVersion"] = 1, ["editorIdentity"] = identity, ["items"] = items, ["durationMs"] = timer.ElapsedMilliseconds };
        }

        private static string AbsolutePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
                throw new ArgumentException("Manifest and source paths must be absolute.");
            var root = Path.GetPathRoot(path);
            if (Path.DirectorySeparatorChar == '\\' && (root == "\\" || root.EndsWith(":")))
                throw new ArgumentException("Manifest and source paths must identify an absolute volume.");
            return Path.GetFullPath(path);
        }

        private static void Require(CommandInfo command, string name, Type type)
        {
            if (command.Parameters.Count(p => p.Name == name && p.ParameterType == type) != 1)
                throw new InvalidOperationException("Native run_script parameter contract changed: " + name);
        }
        private static JObject EditorIdentity()
        {
            using (var process = Process.GetCurrentProcess())
            {
#if UNITY_EDITOR_LINUX
                var stat = File.ReadAllText("/proc/self/stat");
                var fields = stat.Substring(stat.LastIndexOf(')') + 1).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                var start = fields[19];
#elif UNITY_EDITOR_OSX
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var elapsed = process.StartTime.ToUniversalTime() - epoch;
                var start = ((long)elapsed.TotalSeconds).ToString(CultureInfo.InvariantCulture) + "." +
                    ((elapsed.Ticks % 10000000) / 10).ToString("D6", CultureInfo.InvariantCulture);
#else
                var start = process.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture);
#endif
                return new JObject { ["pid"] = process.Id, ["startedAt"] = start,
                    ["project"] = Directory.GetParent(Application.dataPath).FullName };
            }
        }
        private static JObject Failure(string code, string message) => new JObject {
            ["success"] = false, ["error"] = code, ["errorDetails"] = message, ["diagnostics"] = new JArray(),
            ["compileMs"] = 0, ["executeMs"] = 0, ["assemblyName"] = null };
        private static string Hash(byte[] bytes)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
