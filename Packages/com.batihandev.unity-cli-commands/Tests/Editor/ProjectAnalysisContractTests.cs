using System.IO;
using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class ProjectAnalysisContractTests
    {
        private const string Folder = "Assets/__ProjectAnalysis3388";
        [SetUp] public void Setup() { Assert.That(AssetDatabase.IsValidFolder(Folder), Is.False); AssetDatabase.CreateFolder("Assets", "__ProjectAnalysis3388"); AssetDatabase.CreateFolder(Folder,"Child"); AssetDatabase.CreateFolder(Folder + "/Child","Grandchild"); File.WriteAllText(Folder + "/Child/Text.txt", "fixture"); AssetDatabase.ImportAsset(Folder + "/Child/Text.txt", ImportAssetOptions.ForceSynchronousImport); }
        [TearDown] public void Cleanup() { AssetDatabase.DeleteAsset(Folder); }
        [Test] public void StructureDefaultDepthTwoCountsActualFilesAndStopsGrandchildren()
        {
            var r = ProjectAnalysisCommands.Structure(Folder); Assert.That(r.Ok, Is.True); Assert.That(r.Result.MaxDepth, Is.EqualTo(2)); Assert.That(r.Result.TotalFiles, Is.EqualTo(1)); var child = r.Result.Structure.Single(); Assert.That(child.Name, Is.EqualTo("Child")); Assert.That(child.FileCount, Is.EqualTo(1)); Assert.That(child.Children.Single().Name, Is.EqualTo("Grandchild")); Assert.That(child.Children.Single().Children, Is.Null.Or.Empty);
            Assert.That(ProjectAnalysisCommands.Structure(Folder,0).Result.Structure, Is.Empty);
        }
        [TestCase("../")] [TestCase("Packages")] [TestCase("/tmp")] [TestCase("Assets/__Absent3388")]
        public void StructureRefusesOutsideOrNonexistentRoot(string path) { var r = ProjectAnalysisCommands.Structure(path); Assert.That(r.Ok, Is.False); Assert.That(r.Error.Code, Is.Not.EqualTo("NOT_IMPLEMENTED")); }
        [Test] public void StructureRefusesNegativeDepth() { Assert.That(ProjectAnalysisCommands.Structure(Folder,-1).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID")); }
        [Test] public void StructureRefusesNestedSymbolicLinkBeforeAnyRead()
        {
            if (UnityEngine.Application.platform == RuntimePlatform.WindowsEditor) Assert.Ignore("Unix link fixture requires Unix host.");
            var link = Folder + "/Link"; var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ln", "-s /tmp " + link) { UseShellExecute = false }); process.WaitForExit(); Assert.That(process.ExitCode, Is.Zero);
            try { Assert.That(ProjectAnalysisCommands.Structure(Folder).Error.Code, Is.EqualTo("PATH_REPARSE_POINT")); } finally { File.Delete(link); }
        }
        [Test] public void StackReportsVersionAndManifestRegisteredSignalsWithoutMutation()
        {
            var manifest = File.ReadAllBytes("Packages/manifest.json"); var selection = Selection.activeObject; var r = ProjectAnalysisCommands.Stack(); Assert.That(r.Ok, Is.True); Assert.That(r.Result.UnityVersion, Is.EqualTo(UnityEngine.Application.unityVersion)); Assert.That(r.Result.RegisteredPackages, Does.Contain("com.batihandev.unity-cli-commands")); Assert.That(r.Result.ManifestPackages, Is.Not.Null); Assert.That(r.Result.Signals.Any(x => x.Name == "inputSystem"), Is.True); Assert.That(r.Result.AuditStatus, Is.Not.Empty); Assert.That(File.ReadAllBytes("Packages/manifest.json"), Is.EqualTo(manifest)); Assert.That(Selection.activeObject, Is.SameAs(selection));
        }
        [Test] public void ValidationUnusedIncludesResourcesCandidatesAndResourcesDependencySources()
        {
            AssetDatabase.CreateFolder(Folder,"Resources"); var material = new Material(Shader.Find("Hidden/InternalErrorShader")); var unused = new Material(material); AssetDatabase.CreateAsset(material,Folder + "/Material.mat"); AssetDatabase.CreateAsset(unused,Folder + "/Resources/Unused.mat");
            var go = new GameObject("ResourcePrefab"); go.AddComponent<MeshRenderer>().sharedMaterial = material; PrefabUtility.SaveAsPrefabAsset(go,Folder + "/Resources/User.prefab"); UnityEngine.Object.DestroyImmediate(go);
            var r = AssetAnalysisCommands.Unused("Material", "Assets", 10000, true, false, false); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Assets.Any(x => x.Path == Folder + "/Resources/Unused.mat"), Is.True); Assert.That(r.Result.Assets.Any(x => x.Path == Folder + "/Material.mat"), Is.False); Assert.That(r.Result.Note, Does.Contain("runtime"));
        }
        [Test] public void EmptyFolderProjectionSelectsCurrentLeavesAndNeverRoot()
        {
            AssetDatabase.CreateFolder(Folder,"Empty"); AssetDatabase.CreateFolder(Folder + "/Empty","Leaf"); var r = AssetAnalysisCommands.Folders(Folder); Assert.That(r.Ok, Is.True); var leaves = r.Result.Folders.Where(x => x.Leaf && !x.Root).Select(x => x.Path).ToArray(); Assert.That(leaves, Is.EquivalentTo(new[] { Folder + "/Child/Grandchild", Folder + "/Empty/Leaf" })); Assert.That(r.Result.Folders.Any(x => x.Path == Folder + "/Empty" && !x.Leaf), Is.True);
        }
    }
}
