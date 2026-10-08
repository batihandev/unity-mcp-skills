using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class ProjectAnalysisCommands
    {
        [CliCommand("analysis.project-structure", "Project shared validated directory/file facts into a bounded folder and asset-type report.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<ProjectAnalysisStructure> Structure([CliArg("rootPath", "Existing confined Assets directory.")] string rootPath = "Assets", [CliArg("maxDepth", "Nonnegative descendant depth.")] int maxDepth = 2) => ScenePerceptionFacts.Run("analysis.project-structure", () =>
        {
            ScenePerceptionFacts.Bounds(maxDepth); var scan = AssetAnalysisFacts.Scan(rootPath); if (!scan.Ok) throw new ScenePerceptionRefusal(scan.Error.Code, scan.Error.Message);
            var paths = scan.Result.Directories; var normalized = paths[0]; var parentGroups = paths.Skip(1).GroupBy(path => path.Substring(0, path.LastIndexOf('/')), StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.OrderBy(path => path, StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
            var files = scan.Result.Files.GroupBy(file => file.Path.Substring(0, file.Path.LastIndexOf('/')), StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            Func<string, int, List<ProjectAnalysisFolder>> tree = null;
            tree = (parent, depth) => depth >= maxDepth || !parentGroups.TryGetValue(parent, out var children) ? new List<ProjectAnalysisFolder>() : children.Select(path => new ProjectAnalysisFolder { Name = System.IO.Path.GetFileName(path), Path = path, FileCount = files.TryGetValue(path, out var count) ? count : 0, Children = depth + 1 >= maxDepth ? null : tree(path, depth + 1) }).ToList();
            var types = new[] { "Material", "Prefab", "Script", "Texture2D", "AudioClip", "Scene", "Shader" };
            return new ProjectAnalysisStructure { RootPath = normalized, MaxDepth = maxDepth, TotalFiles = scan.Result.Files.Count, AssetCounts = types.Select(type => new ScenePerceptionCount { Name = type, Count = AssetDatabase.FindAssets("t:" + type, new[] { normalized }).Length }).ToList(), Structure = tree(normalized, 0) };
        });
        [CliCommand("analysis.project-stack", "Report evidence-backed pipeline/input/UI/package/test/folder heuristics.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<ProjectAnalysisStack> Stack() => ScenePerceptionFacts.Run("analysis.project-stack", () =>
        {
            var manifestPath = System.IO.Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, "Packages", "manifest.json");
            var manifest = new List<string>();
            if (File.Exists(manifestPath))
            {
                var document = SceneContextJson.Parse(File.ReadAllText(manifestPath));
                if (!(document is JObject) || !(document["dependencies"] is JObject dependencies)) throw new ScenePerceptionRefusal("ANALYSIS_INPUT_INVALID", "Package manifest dependencies are unavailable or malformed.");
                manifest = dependencies.Properties().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).ToList();
            }
            var registered = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Select(package => package.name).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToList();
            var types = AnalysisSceneObjects.LoadedTypes(); var facts = ScenePerceptionFacts.Collect(); var signals = new List<ProjectAnalysisSignal>();
            Func<string, string[], string[], string[], bool> detect = (name, packages, classNames, assets) =>
            {
                var evidence = packages.Where(package => manifest.Contains(package, StringComparer.OrdinalIgnoreCase)).Select(package => "manifest:" + package)
                    .Concat(classNames.Where(className => types.Any(type => type.FullName == className)).Select(className => "loadedType:" + className))
                    .Concat(assets.Where(assetType => AssetDatabase.FindAssets("t:" + assetType, new[] { "Assets" }).Length > 0).Select(assetType => "AssetsType:" + assetType)).ToList();
                signals.Add(new ProjectAnalysisSignal { Name = name, Detected = evidence.Count > 0, Evidence = evidence }); return evidence.Count > 0;
            };
            detect("cinemachine", new[] { "com.unity.cinemachine" }, new[] { "Cinemachine.CinemachineBrain", "Unity.Cinemachine.CinemachineBrain" }, Array.Empty<string>());
            detect("timeline", new[] { "com.unity.timeline" }, Array.Empty<string>(), new[] { "TimelineAsset" });
            detect("navMesh", new[] { "com.unity.ai.navigation" }, new[] { "Unity.AI.Navigation.NavMeshSurface" }, Array.Empty<string>());
            var xr = detect("xr", new[] { "com.unity.xr.interaction.toolkit", "com.unity.xr.management" }, new[] { "UnityEngine.XR.Interaction.Toolkit.XRInteractionManager" }, Array.Empty<string>());
            detect("proBuilder", new[] { "com.unity.probuilder" }, new[] { "UnityEngine.ProBuilder.ProBuilderMesh" }, Array.Empty<string>());
            var input = detect("inputSystem", new[] { "com.unity.inputsystem" }, Array.Empty<string>(), Array.Empty<string>());
            var toolkit = facts.Metrics.HasUiToolkit || AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { "Assets" }).Length > 0 || AssetDatabase.FindAssets("t:PanelSettings", new[] { "Assets" }).Length > 0;
            var uiRoute = facts.Metrics.HasUgui && toolkit ? "Both" : toolkit ? "UIToolkit" : facts.Metrics.HasUgui ? "UGUI" : "Unknown";
            var folders = new[] { "Scripts", "Scenes", "Prefabs", "Materials", "Tests" }.Select(name => new ProjectAnalysisSignal { Name = name, Detected = Directory.Exists(System.IO.Path.Combine(UnityEngine.Application.dataPath, name)), Evidence = new List<string> { "directory:Assets/" + name } }).ToList();
            var testAsmdef = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Any(path => System.IO.Path.GetFileNameWithoutExtension(path).IndexOf("Test", StringComparison.OrdinalIgnoreCase) >= 0);
            var mode = InputMode(out var inputModeSource); var pipeline = GraphicsSettings.currentRenderPipeline;
            var pipelineName = pipeline == null ? "Built-in" : pipeline.GetType().FullName;
            var urp = pipelineName.IndexOf("Universal", StringComparison.OrdinalIgnoreCase) >= 0; var hdrp = pipelineName.IndexOf("HDRender", StringComparison.OrdinalIgnoreCase) >= 0;
            var defaultShader = pipeline == null ? Shader.Find("Standard") : pipeline.defaultShader;
            var unlitName = urp ? "Universal Render Pipeline/Unlit" : hdrp ? "HDRP/Unlit" : "Unlit/Color"; var unlitShader = Shader.Find(unlitName);
            var sprites = facts.Components.TryGetValue("SpriteRenderer", out var spriteCount) ? spriteCount : 0; var meshes = facts.Components.TryGetValue("MeshRenderer", out var meshCount) ? meshCount : 0;
            var profile = xr ? "XR" : uiRoute != "Unknown" && facts.Metrics.Canvases >= Math.Max(1, facts.Metrics.Cameras) ? "UI" : sprites > meshes && sprites > 0 ? "2D" : "3D";
            var auditor = types.Any(type => type.FullName == "Unity.ProjectAuditor.Editor.ProjectAuditor");
            return new ProjectAnalysisStack { UnityVersion = UnityEngine.Application.unityVersion, PackageVersion = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ProjectAnalysisCommands).Assembly)?.version ?? "unmapped", RenderPipeline = pipelineName, DefaultShader = defaultShader == null ? null : defaultShader.name, UnlitShader = unlitShader == null ? null : unlitShader.name, InputMode = mode, InputModeSource = inputModeSource, InputSystemInstalled = input, LegacyInputManagerAvailable = mode == "LegacyInputManager" || mode == "Both" ? (bool?)true : mode == "InputSystem" ? (bool?)false : null, UiRoute = uiRoute, UguiDetected = facts.Metrics.HasUgui || manifest.Contains("com.unity.ugui", StringComparer.OrdinalIgnoreCase), UiToolkitDetected = toolkit, TestsDetected = folders.Single(folder => folder.Name == "Tests").Detected || testAsmdef, NUnitLoaded = AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name.IndexOf("nunit", StringComparison.OrdinalIgnoreCase) >= 0), ProjectProfile = profile, ManifestPackages = manifest, RegisteredPackages = registered, Signals = signals, Folders = folders, AuditStatus = auditor ? "NotRun: ProjectAuditor type loaded; rules/usability require native audit discovery." : "Unavailable: optional ProjectAuditor type not loaded; no installation requested." };
        });
        private static string InputMode(out string source)
        {
            const string path = "ProjectSettings/ProjectSettings.asset";
            const string propertyName = "activeInputHandler";
            source = "Unavailable";
            try
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path).OfType<PlayerSettings>())
                    using (var serialized = new SerializedObject(asset))
                    {
                        var property = serialized.FindProperty(propertyName);
                        if (property == null || property.propertyType != SerializedPropertyType.Integer) continue;
                        var value = property.intValue; source = path + ":" + propertyName;
                        if (value == 0) return "LegacyInputManager";
                        if (value == 1) return "InputSystem";
                        if (value == 2) return "Both";
                        return "UnknownSetting:" + value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
            }
            catch (Exception) { source = "Unavailable"; }
            return "Unknown";
        }
    }
}
