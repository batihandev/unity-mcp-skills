using System;
using System.IO;
using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using BatihanDev.UnityCliCommands.Foundation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class AssetAnalysisContractTests
    {
        private string root;
        private string project;
        [SetUp] public void Create() { root = "Assets/AnalysisContract_" + Guid.NewGuid().ToString("N"); project = Directory.GetParent(UnityEngine.Application.dataPath).FullName; Directory.CreateDirectory(root); AssetDatabase.Refresh(); }
        [TearDown] public void Remove() { AssetDatabase.DeleteAsset(root); AssetDatabase.Refresh(); }
        private string Bytes(string name, byte[] bytes) { var p=root+"/"+name; File.WriteAllBytes(p,bytes); AssetDatabase.ImportAsset(p); return p; }
        private string Material(string name) { var p=root+"/"+name+".mat"; AssetDatabase.CreateAsset(new UnityEngine.Material(Shader.Find("Hidden/InternalErrorShader")),p); return p; }
        private void Reference(string folder, string name, string dependency)
        {
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var go=new GameObject(name); try { go.AddComponent<AnalysisReferenceFixture>().Assigned=AssetDatabase.LoadMainAssetAtPath(dependency); PrefabUtility.SaveAsPrefabAsset(go,folder+"/"+name+".prefab"); } finally { UnityObject.DestroyImmediate(go); }
        }
        [Test] public void ExactAssetsReadOnlyRootSucceedsWhileAuthoringStillRefuses()
        {
            Assert.That(ProjectPathPolicy.ValidateReadOnlyDirectory("Assets",project).Ok,Is.True);
            Assert.That(ProjectPathPolicy.Validate("Assets").Error.Code,Is.EqualTo("PATH_ROOT_FORBIDDEN"));
            Assert.That(AssetAnalysisCommands.Large().Ok,Is.True);
        }
        [TestCase("Assets/../outside","PATH_TRAVERSAL")]
        [TestCase("Library","PATH_OUTSIDE_ROOT")]
        [TestCase("Assets/DefinitelyAbsentAnalysisFolder","FILE_NOT_FOUND")]
        public void ScanRootRefusalsPreserveFiles(string path,string code)
        {
            var p=Bytes("sentinel.bytes",new byte[]{1,2,3}); var r=AssetAnalysisCommands.Folders(path);
            Assert.That(r.Ok,Is.False); Assert.That(r.Error.Code,Is.EqualTo(code)); Assert.That(File.ReadAllBytes(p),Is.EqualTo(new byte[]{1,2,3}));
        }
        [Test] public void ReadOnlyDirectoryRefusesExistingFile()
        {
            var p=Bytes("notfolder.bytes",new byte[]{1}); var r=ProjectPathPolicy.ValidateReadOnlyDirectory(p,project); Assert.That(r.Ok,Is.False);
        }
        [Test] public void FoldersUseOrdinalSiblingPostorderAndRetainLeafTreeDistinction()
        {
            Directory.CreateDirectory(root+"/Empty/Z"); Directory.CreateDirectory(root+"/Empty/A"); Directory.CreateDirectory(root+"/NotEmpty"); File.WriteAllText(root+"/NotEmpty/content.txt","present"); AssetDatabase.Refresh();
            var meta=root+"/Empty/A/orphan.meta";File.WriteAllText(meta,"meta-only scan candidate");
            try {var r=AssetAnalysisCommands.Folders(root); Assert.That(r.Ok,Is.True);
            Assert.That(r.Result.Folders.Select(x=>x.Path),Is.EqualTo(new[]{root+"/Empty/A",root+"/Empty/Z",root+"/Empty"}));
            Assert.That(r.Result.Folders.Select(x=>x.Leaf),Is.EqualTo(new[]{true,true,false}));
            var facts=AssetAnalysisFacts.Scan(root);Assert.That(facts.Ok,Is.True);
            Assert.That(facts.Result.Directories,Is.EqualTo(new[]{root,root+"/Empty",root+"/Empty/A",root+"/Empty/Z",root+"/NotEmpty"}));}
            finally {File.Delete(meta);}
        }
        [Test] public void EmptyScanRootIsReportedAsRootAndNeverConfusedWithLeafChild()
        {
            var r=AssetAnalysisCommands.Folders(root); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Folders.Single().Path,Is.EqualTo(root)); Assert.That(r.Result.Folders.Single().Root,Is.True);
            var facts=AssetAnalysisFacts.Scan(root);Assert.That(facts.Ok,Is.True);Assert.That(facts.Result.Directories,Is.EqualTo(new[]{root}));
        }
        [Test] public void LargeUsesStrictBytesMetaExclusionSortAndTotalBeforeCap()
        {
            Bytes("zero.bytes",new byte[0]); var small=Bytes("small.bytes",new byte[3]); var large=Bytes("large.bytes",new byte[7]); Bytes("equal.bytes",new byte[3]);
            var r=AssetAnalysisCommands.Large(root,1,3); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Assets.Select(x=>x.Path),Is.EqualTo(new[]{large})); Assert.That(r.Result.Total,Is.EqualTo(1)); Assert.That(r.Result.Assets[0].SizeBytes,Is.EqualTo(7));
            var capped=AssetAnalysisCommands.Large(root,1); Assert.That(capped.Ok,Is.True); Assert.That(capped.Result.Total,Is.EqualTo(3)); Assert.That(capped.Result.Truncated,Is.True); Assert.That(capped.Result.Assets[0].Guid,Is.EqualTo(AssetDatabase.AssetPathToGUID(large))); Assert.That(capped.Result.Assets[0].Type,Is.EqualTo("TextAsset"));
        }
        [Test] public void OptimizationLargePreservesInclusiveThresholdAndAssetDatabaseOrder()
        {
            Bytes("first.bytes",new byte[2048]); Bytes("second.bytes",new byte[1024]); Bytes("below.bytes",new byte[1023]);
            var expected=AssetDatabase.FindAssets("t:TextAsset",new[]{root}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>new FileInfo(p).Length>=1024).ToArray();
            var r=AssetAnalysisCommands.Large(root,50,1024,true,false,"UnityEngine.TextAsset",true); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Assets.Select(x=>x.Path),Is.EqualTo(expected)); Assert.That(r.Result.Assets.Select(x=>x.SizeKB),Does.Contain(1));
        }
        [Test] public void DuplicatesUseExactBytesWithTypedIdentitiesAndGroupCaps()
        {
            Bytes("a.bytes",new byte[]{1,2}); Bytes("b.bytes",new byte[]{1,2}); Bytes("sameSizeDifferent.bytes",new byte[]{2,1}); Bytes("c.bytes",new byte[]{3,4,5}); Bytes("d.bytes",new byte[]{3,4,5});
            var r=AssetAnalysisCommands.Duplicates("UnityEngine.TextAsset",root,1); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Total,Is.EqualTo(2)); Assert.That(r.Result.Groups.Count,Is.EqualTo(1)); Assert.That(r.Result.TotalWastedBytes,Is.EqualTo(5)); Assert.That(r.Result.Truncated,Is.True); Assert.That(r.Result.Groups[0].Files.Count,Is.EqualTo(2)); Assert.That(r.Result.Groups[0].Files.All(x=>x.Type=="TextAsset" && !string.IsNullOrEmpty(x.Guid)),Is.True);
        }
        [Test] public void DuplicateDefaultTextureTypeAndFiftyGroupLimitAreUsable()
        {
            var texture=new Texture2D(2,2); try {var bytes=texture.EncodeToPNG(); Bytes("a.png",bytes);Bytes("b.png",bytes);} finally {UnityObject.DestroyImmediate(texture);}
            var r=AssetAnalysisCommands.Duplicates(searchPath:root); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Total,Is.EqualTo(1)); Assert.That(r.Result.Groups[0].Files.All(x=>x.Type=="Texture2D"),Is.True);
        }
        [Test] public void UnusedExcludesResourcesCandidatesAndResourcesSourcesAndOutsideSources()
        {
            var used=Material("used"); var runtimeOnly=Material("runtimeOnly"); var outsideOnly=Material("outsideOnly"); var scope=root+"/Scope"; Directory.CreateDirectory(scope+"/Resources");AssetDatabase.Refresh();
            AssetDatabase.MoveAsset(used,scope+"/used.mat");used=scope+"/used.mat";AssetDatabase.MoveAsset(runtimeOnly,scope+"/runtimeOnly.mat");runtimeOnly=scope+"/runtimeOnly.mat";AssetDatabase.MoveAsset(outsideOnly,scope+"/outsideOnly.mat");outsideOnly=scope+"/outsideOnly.mat";
            var excluded=new UnityEngine.Material(Shader.Find("Hidden/InternalErrorShader"));AssetDatabase.CreateAsset(excluded,scope+"/Resources/excluded.mat");
            Reference(scope,"ordinary",used);Reference(scope+"/Resources","runtime",runtimeOnly);Reference(root,"outside",outsideOnly);
            var r=AssetAnalysisCommands.Unused("Material",scope);Assert.That(r.Ok,Is.True);Assert.That(r.Result.Assets.Select(x=>x.Path),Is.EquivalentTo(new[]{runtimeOnly,outsideOnly}));Assert.That(r.Result.Note,Does.Contain("runtime"));
        }
        [Test] public void GlobalUnusedCandidatesUseAssetsSourcesOutsideSelectedSearchRoot()
        {
            var outside=Material("outsideCandidate");var scope=root+"/Scope";Directory.CreateDirectory(scope);AssetDatabase.Refresh();
            var r=AssetAnalysisCommands.Unused("Material",scope,100,true,false,false);Assert.That(r.Ok,Is.True);Assert.That(r.Result.Assets.Select(x=>x.Path),Does.Contain(outside));
            Reference(root,"usingOutside",outside);var used=AssetAnalysisCommands.Unused("Material",scope,100,true,false,false);Assert.That(used.Ok,Is.True);Assert.That(used.Result.Assets.Select(x=>x.Path),Does.Not.Contain(outside));
        }
        [Test] public void ValidationUnusedCanIncludeResourcesCandidatesAndSources()
        {
            var scope=root+"/Scope";Directory.CreateDirectory(scope+"/Resources");AssetDatabase.Refresh();var target=Material("runtimeTarget");AssetDatabase.MoveAsset(target,scope+"/runtimeTarget.mat");target=scope+"/runtimeTarget.mat";
            var runtime=new UnityEngine.Material(Shader.Find("Hidden/InternalErrorShader"));AssetDatabase.CreateAsset(runtime,scope+"/Resources/runtime.mat");Reference(scope+"/Resources","usingRuntime",target);
            var r=AssetAnalysisCommands.Unused("Material",scope,100,false,false,false);Assert.That(r.Ok,Is.True);Assert.That(r.Result.Assets.Select(x=>x.Path),Does.Contain(scope+"/Resources/runtime.mat"));Assert.That(r.Result.Assets.Select(x=>x.Path),Does.Not.Contain(target));
        }
        [Test] public void UsageUsesDirectReverseDependenciesAndTotalBeforeCap()
        {
            var target=Material("target");Reference(root,"first",target);Reference(root,"second",target);
            var r=AssetAnalysisCommands.Usage(target,1);Assert.That(r.Ok,Is.True);Assert.That(r.Result.Total,Is.EqualTo(2));Assert.That(r.Result.Assets.Count,Is.EqualTo(1));Assert.That(r.Result.Truncated,Is.True);Assert.That(r.Result.Source,Is.Not.Null);Assert.That(r.Result.Source.Path,Is.EqualTo(target));Assert.That(r.Result.Source.Guid,Is.EqualTo(AssetDatabase.AssetPathToGUID(target)));Assert.That(r.Result.Source.Type,Is.EqualTo("Material"));
        }
        [Test] public void DependenciesAcceptFilesAndFoldersExcludeSourceAndRefuseAbsent()
        {
            var material=Material("dependency");Reference(root,"owner",material);
            var file=AssetAnalysisCommands.Dependencies(root+"/owner.prefab",false);Assert.That(file.Ok,Is.True);Assert.That(file.Result.Assets.Select(x=>x.Path),Does.Contain(material));Assert.That(file.Result.Assets.Select(x=>x.Path),Does.Not.Contain(root+"/owner.prefab"));Assert.That(file.Result.Source,Is.Not.Null);Assert.That(file.Result.Source.Path,Is.EqualTo(root+"/owner.prefab"));
            Assert.That(AssetAnalysisCommands.Dependencies(root).Ok,Is.True);Assert.That(AssetAnalysisCommands.Dependencies(root+"/absent.mat").Error.Code,Is.EqualTo("FILE_NOT_FOUND"));
        }
        [TestCase(false)] [TestCase(true)]
        public void DependenciesIgnoreLinksOutsideSelectedFileOrFolderScope(bool folderTarget)
        {
            if(Environment.OSVersion.Platform==PlatformID.Win32NT)Assert.Ignore("Unix link fixture; root retains Windows junction probe.");
            var material=Material("dependencyScope");var scope=root+"/Clean";Reference(scope,"owner",material);var owner=scope+"/owner.prefab";var target=folderTarget?scope:owner;
            var expected=AssetDatabase.GetDependencies(target,true).Where(x=>x!=target).ToArray();if(!folderTarget)Assert.That(expected,Does.Contain(material));
            var beforeMaterial=File.ReadAllBytes(material);var beforeOwner=File.ReadAllBytes(owner);
            var external=Path.Combine(Path.GetTempPath(),"dependency-external-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(external);var sentinel=Path.Combine(external,"outside.txt");File.WriteAllText(sentinel,"outside");
            var link=root+"/unrelated";var info=new System.Diagnostics.ProcessStartInfo("ln", "-s \""+external+"\" \""+Path.GetFullPath(link)+"\""){UseShellExecute=false};using(var process=System.Diagnostics.Process.Start(info)){process.WaitForExit();Assert.That(process.ExitCode,Is.Zero);}
            try
            {
                var result=AssetAnalysisCommands.Dependencies(target);Assert.That(result.Ok,Is.True);
                Assert.That(result.Result.Source.Path,Is.EqualTo(target));Assert.That(result.Result.Source.Guid,Is.EqualTo(AssetDatabase.AssetPathToGUID(target)));Assert.That(result.Result.Source.Type,Is.EqualTo(AssetDatabase.GetMainAssetTypeAtPath(target)?.Name));Assert.That(result.Result.Assets.Select(x=>x.Path),Is.EqualTo(expected));Assert.That(result.Result.Assets.Select(x=>x.Path),Does.Not.Contain(target));
                if(!folderTarget){var dependency=result.Result.Assets.Single(x=>x.Path==material);Assert.That(dependency.Guid,Is.EqualTo(AssetDatabase.AssetPathToGUID(material)));Assert.That(dependency.Type,Is.EqualTo("Material"));}
                Assert.That(File.ReadAllBytes(material),Is.EqualTo(beforeMaterial));Assert.That(File.ReadAllBytes(owner),Is.EqualTo(beforeOwner));Assert.That(File.ReadAllText(sentinel),Is.EqualTo("outside"));
            }
            finally {File.Delete(link);Directory.Delete(external,true);}
        }
        [Test] public void DependencyFolderRefusesLinkedDescendantWithoutChangingSources()
        {
            if(Environment.OSVersion.Platform==PlatformID.Win32NT)Assert.Ignore("Unix link fixture; root retains Windows junction probe.");
            var material=Material("linkedDependency");var scope=root+"/Selected";Reference(scope,"owner",material);var owner=scope+"/owner.prefab";var beforeMaterial=File.ReadAllBytes(material);var beforeOwner=File.ReadAllBytes(owner);
            var external=Path.Combine(Path.GetTempPath(),"dependency-external-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(external);var sentinel=Path.Combine(external,"outside.txt");File.WriteAllText(sentinel,"outside");
            var link=scope+"/linked";var info=new System.Diagnostics.ProcessStartInfo("ln", "-s \""+external+"\" \""+Path.GetFullPath(link)+"\""){UseShellExecute=false};using(var process=System.Diagnostics.Process.Start(info)){process.WaitForExit();Assert.That(process.ExitCode,Is.Zero);}
            try
            {
                var result=AssetAnalysisCommands.Dependencies(scope);Assert.That(result.Ok,Is.False);Assert.That(result.Error.Code,Is.EqualTo("PATH_REPARSE_POINT"));
                Assert.That(File.ReadAllBytes(material),Is.EqualTo(beforeMaterial));Assert.That(File.ReadAllBytes(owner),Is.EqualTo(beforeOwner));Assert.That(File.ReadAllText(sentinel),Is.EqualTo("outside"));
            }
            finally {File.Delete(link);Directory.Delete(external,true);}
        }
        [TestCase(-1,0)] [TestCase(1,-1)]
        public void InvalidLimitOrThresholdRefuses(int limit,long minimum)
        {
            var r=AssetAnalysisCommands.Large(root,limit,minimum);Assert.That(r.Ok,Is.False);Assert.That(r.Error.Code,Is.EqualTo("ANALYSIS_INPUT_INVALID"));
        }
        [Test] public void NativePrefabCategoryPreservesDiscoveryOrderAndUnusedCandidates()
        {
            var go=new GameObject("CategoryPrefab");var path=root+"/category.prefab";
            try {Assert.That(PrefabUtility.SaveAsPrefabAsset(go,path),Is.Not.Null);}finally{UnityObject.DestroyImmediate(go);}
            var expected=AssetDatabase.FindAssets("t:Prefab",new[]{root}).Select(AssetDatabase.GUIDToAssetPath).ToArray();Assert.That(expected,Does.Contain(path));
            var before=File.ReadAllBytes(path);var large=AssetAnalysisCommands.Large(root,100,0,false,false,"Prefab",true);
            Assert.That(large.Ok,Is.True);Assert.That(large.Result.Assets.Select(x=>x.Path),Is.EqualTo(expected));Assert.That(large.Result.Total,Is.EqualTo(expected.Length));
            var unused=AssetAnalysisCommands.Unused("Prefab",root,100);Assert.That(unused.Ok,Is.True);Assert.That(unused.Result.Assets.Select(x=>x.Path),Does.Contain(path));Assert.That(File.ReadAllBytes(path),Is.EqualTo(before));
        }
        [Test] public void QualifiedAssetTypeFiltersActualObjectsBeforeScopedTotalsAndCaps()
        {
            var a=root+"/collisionA.asset";var b=root+"/collisionB.asset";
            AssetDatabase.CreateAsset(UnityEngine.ScriptableObject.CreateInstance<AssetFilter3388A.AssetFilter3388Collision>(),a);
            AssetDatabase.CreateAsset(UnityEngine.ScriptableObject.CreateInstance<AssetFilter3388B.AssetFilter3388Collision>(),b);
            Assert.That(AssetDatabase.LoadMainAssetAtPath(a),Is.TypeOf<AssetFilter3388A.AssetFilter3388Collision>());Assert.That(AssetDatabase.LoadMainAssetAtPath(b),Is.TypeOf<AssetFilter3388B.AssetFilter3388Collision>());
            var native=AssetDatabase.FindAssets("",new[]{root}).Select(AssetDatabase.GUIDToAssetPath).Where(x=>!AssetDatabase.IsValidFolder(x)).ToArray();Assert.That(native,Is.EquivalentTo(new[]{a,b}),"Uncapped native discovery must expose both loadable CLR fixtures before testing typed analysis.");
            var beforeA=File.ReadAllBytes(a);var beforeB=File.ReadAllBytes(b);var type=typeof(AssetFilter3388A.AssetFilter3388Collision).FullName;
            var large=AssetAnalysisCommands.Large(root,100,0,false,false,type,true);Assert.That(large.Ok,Is.True);Assert.That(large.Result.Assets.Select(x=>x.Path),Is.EqualTo(native.Where(x=>x==a)));Assert.That(large.Result.Total,Is.EqualTo(1));
            var capped=AssetAnalysisCommands.Large(root,0,0,false,false,type,true);Assert.That(capped.Ok,Is.True);Assert.That(capped.Result.Total,Is.EqualTo(1));Assert.That(capped.Result.Assets,Is.Empty);Assert.That(capped.Result.Truncated,Is.True);
            var unused=AssetAnalysisCommands.Unused(type,root,100);Assert.That(unused.Ok,Is.True);Assert.That(unused.Result.Assets.Select(x=>x.Path),Is.EqualTo(new[]{a}));
            Assert.That(File.ReadAllBytes(a),Is.EqualTo(beforeA));Assert.That(File.ReadAllBytes(b),Is.EqualTo(beforeB));
        }
        [Test] public void QualifiedAssetTypeFiltersGlobalUnusedActualObjectsAndRetainsCapTotals()
        {
            var a=root+"/globalA.asset";var b=root+"/globalB.asset";
            AssetDatabase.CreateAsset(UnityEngine.ScriptableObject.CreateInstance<AssetFilter3388A.AssetFilter3388Collision>(),a);AssetDatabase.CreateAsset(UnityEngine.ScriptableObject.CreateInstance<AssetFilter3388B.AssetFilter3388Collision>(),b);
            Assert.That(AssetDatabase.LoadMainAssetAtPath(a),Is.TypeOf<AssetFilter3388A.AssetFilter3388Collision>());Assert.That(AssetDatabase.LoadMainAssetAtPath(b),Is.TypeOf<AssetFilter3388B.AssetFilter3388Collision>());
            var discovery=AssetDatabase.FindAssets("").Select(AssetDatabase.GUIDToAssetPath).ToArray();Assert.That(discovery,Does.Contain(a));Assert.That(discovery,Does.Contain(b));
            var beforeA=File.ReadAllBytes(a);var beforeB=File.ReadAllBytes(b);var type=typeof(AssetFilter3388A.AssetFilter3388Collision).FullName;var native=discovery.Where(x=>AssetDatabase.LoadMainAssetAtPath(x) is AssetFilter3388A.AssetFilter3388Collision).ToArray();
            var r=AssetAnalysisCommands.Unused(type,root,10000,true,false,false);Assert.That(r.Ok,Is.True);Assert.That(r.Result.Assets.Select(x=>x.Path),Is.EqualTo(native));Assert.That(r.Result.Assets.Select(x=>x.Path),Does.Not.Contain(b));
            var capped=AssetAnalysisCommands.Unused(type,root,0,true,false,false);Assert.That(capped.Ok,Is.True);Assert.That(capped.Result.Total,Is.EqualTo(native.Length));Assert.That(capped.Result.Assets,Is.Empty);Assert.That(capped.Result.Truncated,Is.True);
            Assert.That(File.ReadAllBytes(a),Is.EqualTo(beforeA));Assert.That(File.ReadAllBytes(b),Is.EqualTo(beforeB));
        }
        [TestCase("DefinitelyAbsentAnalysisType")] [TestCase("AnalysisTypeCollision")]
        public void UnknownOrAmbiguousAssetTypeRefusesRatherThanUnfiltering(string type)
        {
            var r=AssetAnalysisCommands.Duplicates(type,root);Assert.That(r.Ok,Is.False);Assert.That(r.Error.Code,Is.EqualTo("ANALYSIS_TYPE_INVALID"));
        }
        [TestCase(false)] [TestCase(true)]
        public void ChildAndAncestorDirectoryLinksRefuseBeforeTraversal(bool asRoot)
        {
            if(Environment.OSVersion.Platform==PlatformID.Win32NT)Assert.Ignore("Unix link fixture; root retains Windows junction probe.");
            var external=Path.Combine(Path.GetTempPath(),"analysis-external-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(external);File.WriteAllText(Path.Combine(external,"outside.txt"),"outside");
            var link=root+"/linked";var info=new System.Diagnostics.ProcessStartInfo("ln", "-s \""+external+"\" \""+Path.GetFullPath(link)+"\""){UseShellExecute=false};using(var process=System.Diagnostics.Process.Start(info)){process.WaitForExit();Assert.That(process.ExitCode,Is.Zero);}
            try {var r=AssetAnalysisCommands.Large(asRoot?link:root);Assert.That(r.Ok,Is.False);Assert.That(r.Error.Code,Is.EqualTo("PATH_REPARSE_POINT"));Assert.That(File.ReadAllText(Path.Combine(external,"outside.txt")),Is.EqualTo("outside"));}
            finally {File.Delete(link);Directory.Delete(external,true);}
        }
    }
}
namespace BatihanDev.UnityCliCommands.Tests.Analysis.CollisionA { public sealed class AnalysisTypeCollision : UnityEngine.ScriptableObject {} }
namespace BatihanDev.UnityCliCommands.Tests.Analysis.CollisionB { public sealed class AnalysisTypeCollision : UnityEngine.ScriptableObject {} }
