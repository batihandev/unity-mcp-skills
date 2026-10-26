using System;
using System.Collections.Generic;
namespace BatihanDev.UnityCliCommands.Analysis
{
    [Serializable] public sealed class AnalysisAsset
    {
        public string Path { get; set; }
        public string Guid { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public long SizeBytes { get; set; }
        public long SizeKB => SizeBytes / 1024;
        public double SizeMB => SizeBytes / (1024.0 * 1024.0);
    }
    [Serializable] public sealed class AssetListReport
    {
        public AnalysisAsset Source { get; set; }
        public List<AnalysisAsset> Assets { get; set; } = new List<AnalysisAsset>();
        public int Total { get; set; }
        public bool Truncated { get; set; }
        public string Note { get; set; }
    }
    [Serializable] public sealed class AssetDuplicateGroup
    {
        public List<AnalysisAsset> Files { get; set; } = new List<AnalysisAsset>();
        public long SizeBytes { get; set; }
        public long WastedBytes { get; set; }
    }
    [Serializable] public sealed class AssetDuplicateReport
    {
        public List<AssetDuplicateGroup> Groups { get; set; } = new List<AssetDuplicateGroup>();
        public int Total { get; set; }
        public long TotalWastedBytes { get; set; }
        public bool Truncated { get; set; }
    }
    [Serializable] public sealed class FolderFact
    {
        public string Path { get; set; }
        public bool Leaf { get; set; }
        public bool Root { get; set; }
    }
    [Serializable] public sealed class FolderReport
    {
        public List<FolderFact> Folders { get; set; } = new List<FolderFact>();
    }
    [Serializable] public sealed class SceneIssue
    {
        public string Target { get; set; }
        public string Path { get; set; }
        public string Kind { get; set; }
        public string ScenePath { get; set; }
        public string PrefabPath { get; set; }
        public string Component { get; set; }
        public string Property { get; set; }
        public int ComponentIndex { get; set; }
        public long Triangles { get; set; }
        public int MaterialCount { get; set; }
        public string Material { get; set; }
        public string Shader { get; set; }
        public int RenderQueue { get; set; }
    }
    [Serializable] public sealed class SceneMissingReport
    {
        public int Total { get; set; }
        public bool Truncated { get; set; }
        public int MissingScripts { get; set; }
        public int MissingReferences { get; set; }
        public List<SceneIssue> Issues { get; set; } = new List<SceneIssue>();
    }
    [Serializable] public sealed class SceneRenderingReport
    {
        public int TotalRenderers { get; set; }
        public long TotalTriangles { get; set; }
        public int TotalMaterialSlots { get; set; }
        public List<SceneIssue> Issues { get; set; } = new List<SceneIssue>();
    }
    [Serializable] public sealed class SceneTransparentReport
    {
        public int Total { get; set; }
        public bool Truncated { get; set; }
        public string Note { get; set; }
        public List<SceneIssue> Objects { get; set; } = new List<SceneIssue>();
    }
    [Serializable] public sealed class MaterialEquivalentGroup
    {
        public string Shader { get; set; }
        public string Color { get; set; }
        public int RenderQueue { get; set; }
        public List<AnalysisAsset> Materials { get; set; } = new List<AnalysisAsset>();
    }
    [Serializable] public sealed class MaterialEquivalentReport
    {
        public int Total { get; set; }
        public bool Truncated { get; set; }
        public string Note { get; set; }
        public List<MaterialEquivalentGroup> Groups { get; set; } = new List<MaterialEquivalentGroup>();
    }
    [Serializable] public sealed class SceneMutationReport
    {
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public int SelectedCount { get; set; }
        public int AppliedCount { get; set; }
        public int RemovedComponents { get; set; }
        public string Target { get; set; }
        public List<string> Targets { get; set; } = new List<string>();
        public float[] Distances { get; set; }
        public int RendererCount { get; set; }
    }
}
