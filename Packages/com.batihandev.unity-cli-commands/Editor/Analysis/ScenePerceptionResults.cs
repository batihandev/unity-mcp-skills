using System;
using System.Collections.Generic;
namespace BatihanDev.UnityCliCommands.Analysis
{
    [Serializable] public sealed class ScenePerceptionCount
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
    [Serializable] public sealed class ScenePerceptionMetrics
    {
        public int TotalObjects { get; set; }
        public int ActiveObjects { get; set; }
        public int InactiveObjects { get; set; }
        public int RootObjects { get; set; }
        public int MaxHierarchyDepth { get; set; }
        public int PrefabInstances { get; set; }
        public int EmptyLeafObjects { get; set; }
        public int Cameras { get; set; }
        public int MainCameras { get; set; }
        public int Lights { get; set; }
        public int Canvases { get; set; }
        public int EventSystems { get; set; }
        public int AudioListeners { get; set; }
        public bool HasUgui { get; set; }
        public bool HasUiToolkit { get; set; }
        public double DisabledRatio { get; set; }
        public int DisabledObjects => InactiveObjects;
        public bool HasMainCamera => MainCameras > 0;
        public bool HasLight => Lights > 0;
        public bool HasCanvas => Canvases > 0;
        public bool HasEventSystem => EventSystems > 0;
        public bool HasAudioListener => AudioListeners > 0;
    }
    [Serializable] public sealed class ScenePerceptionSummary
    {
        public string SceneName { get; set; }
        public string ScenePath { get; set; }
        public bool IsDirty { get; set; }
        public string Scope { get; set; }
        public ScenePerceptionMetrics Stats { get; set; }
        public List<ScenePerceptionCount> TopComponents { get; set; } = new List<ScenePerceptionCount>();
    }
    [Serializable] public sealed class ScenePerceptionFinding
    {
        public string Kind { get; set; }
        public string Severity { get; set; }
        public string Name { get; set; }
        public string Target { get; set; }
        public string Path { get; set; }
        public string ScenePath { get; set; }
        public string Message { get; set; }
        public int Count { get; set; }
        public int Depth { get; set; }
    }
    [Serializable] public sealed class ScenePerceptionFindings
    {
        public string SceneName { get; set; }
        public int Total { get; set; }
        public bool Truncated { get; set; }
        public List<ScenePerceptionFinding> Findings { get; set; } = new List<ScenePerceptionFinding>();
        public List<ScenePerceptionFinding> Hotspots { get; set; } = new List<ScenePerceptionFinding>();
        public int Errors { get; set; }
        public int Warnings { get; set; }
        public int Info { get; set; }
        public int ShownErrors { get; set; }
        public int ShownWarnings { get; set; }
        public int ShownInfo { get; set; }
        public List<string> SuggestedGuides { get; set; } = new List<string>();
        public int HotspotTotal { get; set; }
        public bool HotspotsTruncated { get; set; }
        public int DeepHierarchyThreshold { get; set; }
        public int LargeChildCountThreshold { get; set; }
    }
    [Serializable] public sealed class ScenePerceptionContract
    {
        public string[] CheckedRoots { get; set; }
        public string[] CheckedTags { get; set; }
        public string[] CheckedLayers { get; set; }
        public bool Passed { get; set; }
        public int Errors { get; set; }
        public int Warnings { get; set; }
        public int Info { get; set; }
        public List<ScenePerceptionFinding> Findings { get; set; } = new List<ScenePerceptionFinding>();
    }
    [Serializable] public sealed class ScenePerceptionTagLayers
    {
        public int TotalObjects { get; set; }
        public int UntaggedCount { get; set; }
        public List<ScenePerceptionCount> Tags { get; set; } = new List<ScenePerceptionCount>();
        public List<ScenePerceptionCount> Layers { get; set; } = new List<ScenePerceptionCount>();
        public List<string> EmptyDefinedLayers { get; set; } = new List<string>();
    }
    [Serializable] public sealed class ScenePerceptionSpatialHit
    {
        public string Target { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ScenePath { get; set; }
        public float Distance { get; set; }
        public float[] Position { get; set; }
    }
    [Serializable] public sealed class ScenePerceptionSpatial
    {
        public float[] Center { get; set; }
        public float Radius { get; set; }
        public int TotalFound { get; set; }
        public bool Truncated { get; set; }
        public List<ScenePerceptionSpatialHit> Results { get; set; } = new List<ScenePerceptionSpatialHit>();
    }
    [Serializable] public sealed class ScenePerceptionShaderProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
    [Serializable] public sealed class ScenePerceptionMaterial
    {
        public string Target { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public int RenderQueue { get; set; }
        public int UserCount { get; set; }
        public List<string> Users { get; set; } = new List<string>();
        public List<ScenePerceptionShaderProperty> Properties { get; set; } = new List<ScenePerceptionShaderProperty>();
    }
    [Serializable] public sealed class ScenePerceptionShaderGroup
    {
        public string Shader { get; set; }
        public int MaterialCount { get; set; }
        public List<ScenePerceptionMaterial> Materials { get; set; } = new List<ScenePerceptionMaterial>();
    }
    [Serializable] public sealed class ScenePerceptionMaterials
    {
        public int TotalMaterials { get; set; }
        public int TotalShaders { get; set; }
        public List<ScenePerceptionShaderGroup> Shaders { get; set; } = new List<ScenePerceptionShaderGroup>();
    }
    [Serializable] public sealed class ScenePerceptionHint
    {
        public int Priority { get; set; }
        public string Category { get; set; }
        public string Issue { get; set; }
        public string Suggestion { get; set; }
        public string FixGuide { get; set; }
    }
    [Serializable] public sealed class ScenePerceptionPerformance
    {
        public string Note { get; set; }
        public List<ScenePerceptionHint> Hints { get; set; } = new List<ScenePerceptionHint>();
    }
    [Serializable] public sealed class SceneContextScalar
    {
        public string Path { get; set; }
        public string Kind { get; set; }
        public string Value { get; set; }
        public bool Supported { get; set; }
    }
    [Serializable] public sealed class SceneContextComponent
    {
        public string Target { get; set; }
        public string Type { get; set; }
        public bool Missing { get; set; }
        public string SerializedJson { get; set; }
        public List<SceneContextScalar> Values { get; set; } = new List<SceneContextScalar>();
        public string SourcePath { get; set; }
        public string Assembly { get; set; }
        public string SourceClassification { get; set; }
    }
    [Serializable] public sealed class SceneContextObject
    {
        public string Target { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ScenePath { get; set; }
        public string SceneHandle { get; set; }
        public int Depth { get; set; }
        public List<SceneContextComponent> Components { get; set; } = new List<SceneContextComponent>();
    }
    [Serializable] public sealed class SceneContextReference
    {
        public string SourceObject { get; set; }
        public string SourceComponent { get; set; }
        public string SourceComponentType { get; set; }
        public string SourceSceneHandle { get; set; }
        public string TargetSceneHandle { get; set; }
        public string SourcePath { get; set; }
        public string SourceScenePath { get; set; }
        public string Property { get; set; }
        public string Target { get; set; }
        public string TargetObject { get; set; }
        public string TargetPath { get; set; }
        public string TargetScenePath { get; set; }
        public string TargetAssetPath { get; set; }
        public string TargetType { get; set; }
        public bool Missing { get; set; }
        public bool Null { get; set; }
    }
    [Serializable] public sealed class SceneContextReport
    {
        public string SceneName { get; set; }
        public string Scope { get; set; }
        public int TotalObjects { get; set; }
        public int ScopeObjects { get; set; }
        public int ExportedObjects { get; set; }
        public int DepthOmitted { get; set; }
        public int CountOmitted { get; set; }
        public bool Truncated { get; set; }
        public List<SceneContextObject> Objects { get; set; } = new List<SceneContextObject>();
        public List<SceneContextReference> References { get; set; } = new List<SceneContextReference>();
        public List<ScriptAnalysisEdge> CodeDependencies { get; set; } = new List<ScriptAnalysisEdge>();
    }
    [Serializable] public sealed class SceneContextDependencies
    {
        public string Target { get; set; }
        public int ObjectsAnalyzed { get; set; }
        public int TotalReferences { get; set; }
        public List<SceneContextReference> Edges { get; set; } = new List<SceneContextReference>();
        public List<SceneContextReference> Incoming { get; set; } = new List<SceneContextReference>();
        public List<SceneContextReference> Outgoing { get; set; } = new List<SceneContextReference>();
        public List<SceneContextReference> Internal { get; set; } = new List<SceneContextReference>();
        public List<SceneContextReference> Boundary { get; set; } = new List<SceneContextReference>();
        public string Markdown { get; set; }
        public string Scope { get; set; }
    }
    [Serializable] public sealed class SceneContextMarkdown
    {
        public string Markdown { get; set; }
        public SceneContextReport Context { get; set; }
        public int UserScriptCount { get; set; }
        public int UnknownScriptCount { get; set; }
        public int ReferenceCount { get; set; }
        public int CodeReferenceCount { get; set; }
    }
    [Serializable] public sealed class SceneContextSnapshotEntry
    {
        public string Target { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ScenePath { get; set; }
        public string[] Components { get; set; }
        public float[] Position { get; set; }
        public float[] Rotation { get; set; }
        public float[] Scale { get; set; }
    }
    [Serializable] public sealed class SceneContextSnapshot
    {
        public int Version { get; set; }
        public string EditorSession { get; set; }
        public List<SceneContextSnapshotEntry> Objects { get; set; } = new List<SceneContextSnapshotEntry>();
    }
    [Serializable] public sealed class SceneContextModified
    {
        public string Target { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public List<string> Changes { get; set; } = new List<string>();
    }
    [Serializable] public sealed class SceneContextDiff
    {
        public string Mode { get; set; }
        public SceneContextSnapshot Snapshot { get; set; }
        public string SnapshotJson { get; set; }
        public List<SceneContextSnapshotEntry> Added { get; set; } = new List<SceneContextSnapshotEntry>();
        public List<SceneContextSnapshotEntry> Removed { get; set; } = new List<SceneContextSnapshotEntry>();
        public List<SceneContextModified> Modified { get; set; } = new List<SceneContextModified>();
    }
    [Serializable] public sealed class ScriptAnalysisField
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DeclaringType { get; set; }
        public bool SerializationCandidate { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsStatic { get; set; }
    }
    [Serializable] public sealed class ScriptAnalysisProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool CanWrite { get; set; }
        public bool IsPrivate { get; set; }
    }
    [Serializable] public sealed class ScriptAnalysisMethod
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public string Parameters { get; set; }
        public bool IsPrivate { get; set; }
    }
    [Serializable] public sealed class ScriptAnalysisInspection
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Assembly { get; set; }
        public string Kind { get; set; }
        public string BaseClass { get; set; }
        public List<ScriptAnalysisField> Fields { get; set; } = new List<ScriptAnalysisField>();
        public List<ScriptAnalysisProperty> Properties { get; set; } = new List<ScriptAnalysisProperty>();
        public List<ScriptAnalysisMethod> Methods { get; set; } = new List<ScriptAnalysisMethod>();
        public List<string> UnityCallbacks { get; set; } = new List<string>();
    }
    [Serializable] public sealed class ScriptAnalysisEdge
    {
        public string From { get; set; }
        public string To { get; set; }
        public string SourceType { get; set; }
        public string DeclaringType { get; set; }
        public string Field { get; set; }
        public string FieldType { get; set; }
    }
    [Serializable] public sealed class ScriptAnalysisNode
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Assembly { get; set; }
        public int Hop { get; set; }
        public string Kind { get; set; }
        public string BaseClass { get; set; }
        public string FilePath { get; set; }
        public List<string> DependsOn { get; set; } = new List<string>();
        public List<string> DependedBy { get; set; } = new List<string>();
        public List<ScriptAnalysisField> Fields { get; set; } = new List<ScriptAnalysisField>();
        public List<string> UnityCallbacks { get; set; } = new List<string>();
    }
    [Serializable] public sealed class ScriptAnalysisGraph
    {
        public string EntryScript { get; set; }
        public int MaxHops { get; set; }
        public int TotalScriptsReached { get; set; }
        public string Scope { get; set; }
        public List<ScriptAnalysisNode> Scripts { get; set; } = new List<ScriptAnalysisNode>();
        public List<ScriptAnalysisEdge> Edges { get; set; } = new List<ScriptAnalysisEdge>();
        public List<string> SuggestedReadOrder { get; set; } = new List<string>();
        public List<string> CyclicRemainder { get; set; } = new List<string>();
    }
    [Serializable] public sealed class ProjectAnalysisFolder
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int FileCount { get; set; }
        public List<ProjectAnalysisFolder> Children { get; set; } = new List<ProjectAnalysisFolder>();
    }
    [Serializable] public sealed class ProjectAnalysisStructure
    {
        public string RootPath { get; set; }
        public int MaxDepth { get; set; }
        public int TotalFiles { get; set; }
        public List<ScenePerceptionCount> AssetCounts { get; set; } = new List<ScenePerceptionCount>();
        public List<ProjectAnalysisFolder> Structure { get; set; } = new List<ProjectAnalysisFolder>();
    }
    [Serializable] public sealed class ProjectAnalysisSignal
    {
        public string Name { get; set; }
        public bool Detected { get; set; }
        public List<string> Evidence { get; set; } = new List<string>();
    }
    [Serializable] public sealed class ProjectAnalysisStack
    {
        public string UnityVersion { get; set; }
        public string PackageVersion { get; set; }
        public string RenderPipeline { get; set; }
        public string DefaultShader { get; set; }
        public string UnlitShader { get; set; }
        public string InputMode { get; set; }
        public string InputModeSource { get; set; }
        public bool InputSystemInstalled { get; set; }
        public bool? LegacyInputManagerAvailable { get; set; }
        public string UiRoute { get; set; }
        public bool UguiDetected { get; set; }
        public bool UiToolkitDetected { get; set; }
        public bool TestsDetected { get; set; }
        public bool NUnitLoaded { get; set; }
        public string ProjectProfile { get; set; }
        public List<string> ManifestPackages { get; set; } = new List<string>();
        public List<string> RegisteredPackages { get; set; } = new List<string>();
        public List<ProjectAnalysisSignal> Signals { get; set; } = new List<ProjectAnalysisSignal>();
        public List<ProjectAnalysisSignal> Folders { get; set; } = new List<ProjectAnalysisSignal>();
        public string AuditStatus { get; set; }
    }
    [Serializable] public sealed class ValidationDiagnosticsCollider
    {
        public string Target { get; set; }
        public string Path { get; set; }
        public string ScenePath { get; set; }
        public int VertexCount { get; set; }
    }
    [Serializable] public sealed class ValidationDiagnosticsColliders
    {
        public int Total { get; set; }
        public bool Truncated { get; set; }
        public List<ValidationDiagnosticsCollider> Colliders { get; set; } = new List<ValidationDiagnosticsCollider>();
    }
    [Serializable] public sealed class ValidationDiagnosticsShaderMessage
    {
        public string Severity { get; set; }
        public string Message { get; set; }
        public string File { get; set; }
        public int Line { get; set; }
    }
    [Serializable] public sealed class ValidationDiagnosticsShader
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int MessageCount { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public List<ValidationDiagnosticsShaderMessage> Messages { get; set; } = new List<ValidationDiagnosticsShaderMessage>();
    }
    [Serializable] public sealed class ValidationDiagnosticsShaders
    {
        public int Total { get; set; }
        public int MessageCount { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public bool Truncated { get; set; }
        public List<ValidationDiagnosticsShader> Shaders { get; set; } = new List<ValidationDiagnosticsShader>();
    }
    [Serializable] public sealed class ValidationDiagnosticsTexture
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MaxTextureSize { get; set; }
        public string Format { get; set; }
        public string Recommendation { get; set; }
    }
    [Serializable] public sealed class ValidationDiagnosticsTextures
    {
        public int MaxRecommendedSize { get; set; }
        public int Total { get; set; }
        public bool Truncated { get; set; }
        public List<ValidationDiagnosticsTexture> Textures { get; set; } = new List<ValidationDiagnosticsTexture>();
    }
}
