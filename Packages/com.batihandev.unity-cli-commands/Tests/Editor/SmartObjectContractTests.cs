using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class SmartObjectContractTests : SmartFixtureSupport
    {
        [Test] public void ReplacementPreservesParentWorldRotationScaleAndUnselectedSiblingsThroughUndoRedo()
        {
            var path = Prefab();
            var parent = Go("Parent");
            parent.transform.rotation = Quaternion.Euler(20, 30, 10);
            parent.transform.localScale = new Vector3(2, 3, 4);
            var before = Go("Before", new Vector3(-1, 2, 3), parent);
            var original = Go("A", new Vector3(3, 4, 5), parent);
            original.transform.rotation = Quaternion.Euler(10, 40, 20);
            original.transform.localScale = new Vector3(3, 2, 1);
            var after = Go("After", new Vector3(9, 8, 7), parent);
            var originalId = Id(original);
            var children = new[] { before, original, after };
            var positions = children.Select(value => value.transform.position).ToArray();
            var rotations = children.Select(value => value.transform.rotation).ToArray();
            var scales = children.Select(value => value.transform.localScale).ToArray();
            var parentPosition = parent.transform.position;
            var parentRotation = parent.transform.rotation;
            var parentScale = parent.transform.localScale;

            void AssertHierarchy(string middleName, string middleIdentity)
            {
                Assert.That(parent.transform.childCount, Is.EqualTo(3));
                Assert.That(Enumerable.Range(0, 3).Select(index => parent.transform.GetChild(index).name),
                    Is.EqualTo(new[] { "Before", middleName, "After" }));
                Assert.That(Enumerable.Range(0, 3).Select(index => Id(parent.transform.GetChild(index).gameObject)),
                    Is.EqualTo(new[] { Id(before), middleIdentity, Id(after) }));
                Near(parent.transform.position, parentPosition);
                SameRotation(parent.transform.rotation, parentRotation);
                Near(parent.transform.localScale, parentScale);
                for (var index = 0; index < 3; index++)
                {
                    var child = parent.transform.GetChild(index);
                    Assert.That(child.parent, Is.SameAs(parent.transform));
                    Near(child.position, positions[index]);
                    SameRotation(child.rotation, rotations[index]);
                    Near(child.localScale, scales[index]);
                }
            }

            var result = SmartObjectCommands.Replace(path, false, true, Targets(original));
            Assert.That(result.Ok, Is.True);
            Assert.That(original == null, Is.True);
            var replacement = Selection.activeGameObject;
            var replacementId = Id(replacement);
            AssertHierarchy("Replacement", replacementId);
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(replacement), Is.True);
            Assert.That(result.Result.Replacements.Single().Original, Is.EqualTo(originalId));
            Assert.That(result.Result.SelectionUndoable, Is.False);

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            AssertHierarchy("A", originalId);
            Assert.That(replacement == null, Is.True);
            Undo.PerformRedo();
            AssertHierarchy("Replacement", replacementId);
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(parent.transform.GetChild(1).gameObject), Is.True);
        }
        [Test] public void ReplacementUsesEachOriginalSceneAndPreservesUnrelatedObjects()
        { var path=Prefab(); var a=Go("A"); var unrelated=Go("Unrelated"); var second=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive); var b=new GameObject("B"); SceneManager.MoveGameObjectToScene(b,second); SceneManager.SetActiveScene(First); var r=SmartObjectCommands.Replace(path,false,true,Targets(b,a)); Assert.That(r.Ok,Is.True); Assert.That(Selection.gameObjects.Length,Is.EqualTo(2)); Assert.That(Selection.gameObjects.Select(x=>x.scene.handle),Is.EquivalentTo(new[]{First.handle,second.handle})); Assert.That(unrelated!=null,Is.True); Assert.That(unrelated.scene,Is.EqualTo(First)); }
        [Test] public void ReplacementPreviewWinsWithNoCreationDirtyUndoOrSelectionChange()
        { var path=Prefab(); var a=Go("A"); Selection.activeGameObject=a; EditorSceneManager.SaveScene(First); var group=Undo.GetCurrentGroup(); var r=SmartObjectCommands.Replace(path,true,true,Targets(a)); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Preview,Is.True); Assert.That(First.GetRootGameObjects().Length,Is.EqualTo(1)); Assert.That(Selection.activeGameObject,Is.SameAs(a)); Assert.That(First.isDirty,Is.False); Assert.That(Undo.GetCurrentGroup(),Is.EqualTo(group)); }
        [Test] public void ReplacementOverlapRefusesWholeSetAndKeepsOriginalSelection()
        { var path=Prefab(); var p=Go("Parent"); var c=Go("Child",parent:p); Selection.objects=new UnityEngine.Object[]{c,p}; var r=SmartObjectCommands.Replace(path,false,true,Targets(c,p)); Assert.That(r.Ok,Is.False); Assert.That(r.Error.Code,Is.EqualTo("SMART_SELECTION_OVERLAP")); Assert.That(c.transform.parent,Is.SameAs(p.transform)); Assert.That(Selection.gameObjects,Is.EquivalentTo(new[]{c,p})); }
        [TestCase("../Outside.prefab")] [TestCase("Packages/Elsewhere.prefab")] [TestCase("Assets/__Missing3393.prefab")] public void ReplacementRefusesInvalidAssetBeforeDestroyingTargets(string path)
        { var a=Go("A"); Assert.That(SmartObjectCommands.Replace(path,false,true,Targets(a)).Ok,Is.False); Assert.That(a!=null,Is.True); }
        [Test] public void ReplacementRefusesSceneAssetAndStaleMixedTargets()
        { var path=Prefab(); var a=Go("A"); var b=Go("B"); var targets=Targets(a,b); UnityEngine.Object.DestroyImmediate(b); Assert.That(SmartObjectCommands.Replace(path,false,true,targets).Ok,Is.False); Assert.That(a!=null,Is.True); Assert.That(SmartObjectCommands.Replace(Folder+"/Base.unity",false,true,Targets(a)).Ok,Is.False); }
        [Test] public void ReplacementRequiresConfirmationAndDeduplicatesExactObjects()
        { var path=Prefab(); var a=Go("A"); Assert.That(SmartObjectCommands.Replace(path,false,false,Targets(a)).Error.Code,Is.EqualTo("SMART_CONFIRM_REQUIRED")); var r=SmartObjectCommands.Replace(path,targetsJson:Targets(a,a)); Assert.That(r.Ok,Is.True); Assert.That(r.Result.SelectedCount,Is.EqualTo(1)); }
        [Test] public void ConnectedTemplateChildRefusesBeforeAnyEarlierIndependentReplacement()
        {
            var template=Go("Template"); Go("TemplateChild",parent:template); var originalPath=Folder+"/Template.prefab"; PrefabUtility.SaveAsPrefabAsset(template,originalPath); UnityEngine.Object.DestroyImmediate(template);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(originalPath),First); var child=instance.transform.GetChild(0).gameObject; var ordinary=Go("Ordinary",Vector3.one*.3f); var replacement=Prefab();
            var r=SmartObjectCommands.Replace(replacement,false,true,Targets(ordinary,child)); Assert.That(r.Ok,Is.False); Assert.That(r.Error.Code,Is.EqualTo("SMART_TARGET_INELIGIBLE")); Assert.That(ordinary!=null,Is.True); Near(ordinary.transform.position,Vector3.one*.3f); Assert.That(child.transform.parent,Is.SameAs(instance.transform)); Assert.That(instance.transform.childCount,Is.EqualTo(1)); Assert.That(First.GetRootGameObjects().Select(x=>x.name),Is.EquivalentTo(new[]{"Template","Ordinary"}));
        }
        [Test] public void AddedGameObjectOverrideCanReplaceAndStructurallyUndo()
        {
            var originalPath=Prefab("Base"); var replacementPath=Prefab("AddedReplacement"); var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(originalPath),First); var added=Go("Added",new Vector3(3,4,5),instance); Assert.That(PrefabUtility.IsAddedGameObjectOverride(added),Is.True);
            var r=SmartObjectCommands.Replace(replacementPath,false,true,Targets(added)); Assert.That(r.Ok,Is.True); Assert.That(instance.transform.GetChild(0).name,Is.EqualTo("AddedReplacement")); Near(instance.transform.GetChild(0).position,new Vector3(3,4,5)); Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.That(instance.transform.GetChild(0).name,Is.EqualTo("Added")); Near(instance.transform.GetChild(0).position,new Vector3(3,4,5)); Undo.PerformRedo(); Assert.That(instance.transform.GetChild(0).name,Is.EqualTo("AddedReplacement")); Assert.That(PrefabUtility.IsPartOfPrefabInstance(instance),Is.True);
        }
        [Test] public void ReplacementPrefabConnectionPersistsSavedScene()
        { var path=Prefab(); var a=Go("A",new Vector3(3,4,5)); var r=SmartObjectCommands.Replace(path,false,true,Targets(a)); Assert.That(r.Ok,Is.True); EditorSceneManager.SaveScene(First); var opened=EditorSceneManager.OpenScene(Folder+"/Base.unity"); var instance=opened.GetRootGameObjects().Single(); Assert.That(PrefabUtility.IsPartOfPrefabInstance(instance),Is.True); Near(instance.transform.position,new Vector3(3,4,5)); }
    }
}
