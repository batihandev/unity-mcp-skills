using System;
using System.IO;
using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;
using UnityEngine.TestTools;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class ValidationDiagnosticsContractTests
    {
        private const string Folder = "Assets/__ValidationDiagnostics3388";
        private UnityEngine.SceneManagement.Scene scene;
        [SetUp] public void Setup()
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); Assert.That(AssetDatabase.IsValidFolder(Folder), Is.False);
            AssetDatabase.CreateFolder("Assets", "__ValidationDiagnostics3388"); Assert.That(EditorSceneManager.SaveScene(scene, Folder + "/Base.unity"), Is.True);
        }
        [TearDown] public void Cleanup() { Undo.ClearAll(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); AssetDatabase.DeleteAsset(Folder); }
        private GameObject Go(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
        private void Missing(GameObject go)
        {
            var component = go.AddComponent<SceneContextReferenceFixture>(); using (var so = new SerializedObject(component)) { so.FindProperty("m_Script").objectReferenceValue = null; so.ApplyModifiedPropertiesWithoutUndo(); }
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go), Is.EqualTo(1)); Undo.ClearAll();
        }
        [Test] public void SceneDuplicateDefaultsExcludeInactiveAndEmptyChecksAreOptIn()
        {
            Go("Same"); Go("Same"); Go("Same").SetActive(false); var r = ValidationDiagnosticsCommands.Scene(); Assert.That(r.Ok, Is.True);
            Assert.That(r.Result.Findings.Single(x => x.Kind == "DuplicateName").Count, Is.EqualTo(2)); Assert.That(r.Result.Findings.Any(x => x.Kind == "EmptyGameObject"), Is.False);
            var empty = ValidationDiagnosticsCommands.Scene(checkDuplicateNames:false, checkEmptyGameObjects:true); Assert.That(empty.Result.Findings.Count(x => x.Kind == "EmptyGameObject"), Is.EqualTo(2)); Assert.That(empty.Result.Findings.Any(x => x.Kind == "DuplicateName"), Is.False);
        }
        [Test] public void SceneMissingScriptToggleUsesActualUnresolvedComponent()
        {
            Missing(Go("Missing")); var r = ValidationDiagnosticsCommands.Scene(); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Findings.Single().Kind, Is.EqualTo("MissingScript")); Assert.That(r.Result.Errors, Is.EqualTo(1)); Assert.That(ValidationDiagnosticsCommands.Scene(checkMissingScripts:false).Result.Findings, Is.Empty);
        }
        [Test] public void ConvexReportExcludesInactiveAndConvexAndCountsNullMeshAsZero()
        {
            Go("NonConvex").AddComponent<MeshCollider>(); var inactive = Go("Inactive"); inactive.AddComponent<MeshCollider>(); inactive.SetActive(false); Go("Convex").AddComponent<MeshCollider>().convex = true;
            var r = ValidationDiagnosticsCommands.MeshColliders(); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Total, Is.EqualTo(1)); Assert.That(r.Result.Colliders.Single().Path, Is.EqualTo("NonConvex")); Assert.That(r.Result.Colliders.Single().VertexCount, Is.Zero); var cap = ValidationDiagnosticsCommands.MeshColliders(0); Assert.That(cap.Result.Total, Is.EqualTo(1)); Assert.That(cap.Result.Colliders, Is.Empty); Assert.That(cap.Result.Truncated, Is.True);
        }
        [Test] public void TextureUsesImportedDimensionsStrictThresholdAndPreservesImporterBytes()
        {
            var texture = new Texture2D(8, 4); var path = Folder + "/Size.png"; File.WriteAllBytes(path, texture.EncodeToPNG()); UnityObject.DestroyImmediate(texture); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 32; importer.SaveAndReimport();
            var before = File.ReadAllBytes(path + ".meta"); var equal = ValidationDiagnosticsCommands.TextureSizes(8); Assert.That(equal.Ok, Is.True); Assert.That(equal.Result.Textures.Any(x => x.Path == path), Is.False);
            var r = ValidationDiagnosticsCommands.TextureSizes(7); Assert.That(r.Ok, Is.True); var row = r.Result.Textures.Single(x => x.Path == path); Assert.That(row.Width, Is.EqualTo(8)); Assert.That(row.Height, Is.EqualTo(4)); Assert.That(row.MaxTextureSize, Is.EqualTo(32)); Assert.That(row.Recommendation, Does.Contain("7")); Assert.That(File.ReadAllBytes(path + ".meta"), Is.EqualTo(before));
        }
        [Test] public void ShaderMessagesSeparateRealErrorsFromAllMessageCountAndPreserveSources()
        {
            var path = Folder + "/Broken.shader"; File.WriteAllText(path, "Shader \"Validation3388/Broken\" { SubShader { Pass { HLSLPROGRAM\n#pragma vertex vert\n#pragma fragment frag\nfloat4 vert(float4 p:POSITION):SV_POSITION { return unknown_identifier; }\nfloat4 frag():SV_Target { return 1; }\nENDHLSL\n} } }"); var priorIgnore = LogAssert.ignoreFailingMessages;
            try { LogAssert.ignoreFailingMessages = true; AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); } finally { LogAssert.ignoreFailingMessages = priorIgnore; }
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path); Assert.That(shader, Is.Not.Null); var before = File.ReadAllBytes(path);
            var r = ValidationDiagnosticsCommands.ShaderMessages(); Assert.That(r.Ok, Is.True); var row = r.Result.Shaders.Single(x => x.Path == path);
            var messages = ShaderUtil.GetShaderMessages(shader); Assert.That(messages.Length, Is.GreaterThan(0), "Actual compiler messages required before testing projection."); Assert.That(messages.Any(x => x.severity.ToString() == "Error" && x.message.Contains("unknown_identifier")), Is.True, "The owned shader must expose its actual compiler error.");
            Assert.That(row.Messages.Select(x => new { x.Severity, x.Message, x.File, x.Line }), Is.EqualTo(messages.Select(x => new { Severity = x.severity.ToString(), Message = x.message, File = x.file, Line = x.line })));
            Assert.That(row.MessageCount, Is.EqualTo(messages.Length)); Assert.That(row.ErrorCount, Is.EqualTo(messages.Count(x => x.severity.ToString() == "Error"))); Assert.That(row.WarningCount, Is.EqualTo(messages.Count(x => x.severity.ToString() == "Warning"))); Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
            var zero = ValidationDiagnosticsCommands.ShaderMessages(0); Assert.That(zero.Result.Shaders, Is.Empty); Assert.That(zero.Result.Total, Is.GreaterThan(0));
        }
        [Test] public void MissingPrefabScanPreservesBytesAndUnloadsReadOnlyContents()
        {
            var path = Folder + "/Missing.prefab"; Assert.That(AssetDatabase.CopyAsset("Packages/com.batihandev.unity-cli-commands/Tests/Fixtures/MissingScript.prefab", path), Is.True); var before = File.ReadAllBytes(path); var contents = PrefabUtility.LoadPrefabContents(path);
            try { Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(contents), Is.EqualTo(1)); } finally { PrefabUtility.UnloadPrefabContents(contents); }
            var r = SceneAnalysisCommands.Missing(false, true, false, searchInPrefabs:true); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Issues.Any(x => x.PrefabPath == path && x.Kind == "MissingScript"), Is.True); Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
            Assert.That(SceneAnalysisCommands.Missing(false, true, false, searchInPrefabs:false).Result.Issues.Any(x => x.PrefabPath == path), Is.False);
        }
        [Test] public void ReferencesFirstVisiblePerComponentAndStrictCapHaveCompleteTotals()
        {
            var go = Go("Many"); var removed = Go("Removed");
            for (var i = 0; i < 52; i++) { var fixture = go.AddComponent<SceneContextReferenceFixture>(); fixture.Assigned = removed; fixture.OtherAssigned = removed; }
            UnityObject.DestroyImmediate(removed); var r = SceneAnalysisCommands.Missing(false, false, true, true, true, 50); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Total, Is.EqualTo(52)); Assert.That(r.Result.Issues.Count, Is.EqualTo(50)); Assert.That(r.Result.Truncated, Is.True); Assert.That(r.Result.Issues.All(x => x.Property == "Assigned"), Is.True);
            Assert.That(SceneAnalysisCommands.Missing(false,false,true,false,false).Result.Total, Is.EqualTo(104));
        }
        [Test] public void SceneOnlyFixPreviewAndUndoRespectExplicitInactiveFalsePolicy()
        {
            var active = Go("Active"); Missing(active); var inactive = Go("Inactive"); Missing(inactive); inactive.SetActive(false);
            var preview = MissingScriptCommands.Fix(includeInactive:false); Assert.That(preview.Ok, Is.True); Assert.That(preview.Result.SelectedCount, Is.EqualTo(1)); Assert.That(preview.Result.RemovedComponents, Is.Zero); Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(active), Is.EqualTo(1));
            var applied = MissingScriptCommands.Fix(includeInactive:false,confirm:true,dryRun:false); Assert.That(applied.Ok, Is.True); Assert.That(applied.Result.RemovedComponents, Is.EqualTo(1)); Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(inactive), Is.EqualTo(1)); Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(active), Is.EqualTo(1));
        }
        [TestCase(-1)] public void ValidationCapsRefuseNegativeLimits(int limit)
        {
            Assert.That(ValidationDiagnosticsCommands.MeshColliders(limit).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID")); Assert.That(ValidationDiagnosticsCommands.ShaderMessages(limit).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID")); Assert.That(ValidationDiagnosticsCommands.TextureSizes(limit:limit).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID")); Assert.That(ValidationDiagnosticsCommands.TextureSizes(-1).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID"));
        }
    }
}
