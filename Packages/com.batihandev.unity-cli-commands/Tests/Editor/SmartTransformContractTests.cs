using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using BatihanDev.UnityCliCommands.Foundation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class SmartTransformContractTests : SmartFixtureSupport
    {
        [Test] public void SnapUsesWorldMidpointRoundingAndPreservesRotationScaleThroughUndoRedo()
        {
            var parent = Go("Parent"); parent.transform.rotation = Quaternion.Euler(0, 35, 0); parent.transform.localScale = new Vector3(2, 3, 4);
            var a = Go("A", new Vector3(.6f, 1.6f, -2.6f), parent); var midpoint = Go("Midpoint", new Vector3(.5f, 1.5f, -2.5f)); a.transform.localScale = new Vector3(3, 4, 5);
            var rotation = a.transform.rotation; var before = a.transform.position; var scale = a.transform.localScale;
            var r = SmartTransformCommands.SnapGrid(dryRun:false, confirm:true, targetsJson:Targets(a, midpoint)); Assert.That(r.Ok, Is.True);
            Near(a.transform.position, new Vector3(1, 2, -3)); Near(midpoint.transform.position, new Vector3(0, 2, -2)); SameRotation(a.transform.rotation, rotation); Near(a.transform.localScale, scale);
            Assert.That(r.Result.ChangedCount, Is.EqualTo(2)); Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Near(a.transform.position, before); Near(midpoint.transform.position, new Vector3(.5f, 1.5f, -2.5f)); Undo.PerformRedo(); Near(a.transform.position, new Vector3(1, 2, -3)); Near(midpoint.transform.position, new Vector3(0, 2, -2));
        }
        [TestCase("X", 4, 7, 9)] [TestCase("-x", 4, 7, 9)] [TestCase("Y", 99, 6, 9)] [TestCase("-Y", 99, 6, 9)] [TestCase("Z", 99, 7, 8)] [TestCase("-z", 99, 7, 8)]
        public void DistributeProjectsOnlyRequestedAxisAndCountsEndpointsHonestly(string axis, float x, float y, float z)
        {
            var a = Go("A", new Vector3(0, 2, 4)); var b = Go("B", new Vector3(99, 7, 9)); var c = Go("C", new Vector3(8, 10, 12));
            var r = SmartTransformCommands.Distribute(axis, false, true, Targets(c, a, b)); Assert.That(r.Ok, Is.True); Near(b.transform.position, new Vector3(x, y, z)); Near(a.transform.position, new Vector3(0, 2, 4)); Near(c.transform.position, new Vector3(8, 10, 12));
            Assert.That(r.Result.ProcessedCount, Is.EqualTo(3)); Assert.That(r.Result.ChangedCount, Is.EqualTo(1)); Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Near(b.transform.position, new Vector3(99, 7, 9));
        }
        [Test] public void LinearDefaultsStartAtSiblingFirstAndPreserveOtherChannels()
        {
            var a = Go("A", Vector3.one); var b = Go("B", Vector3.one * 9); var c = Go("C", Vector3.one * 20); b.transform.rotation = Quaternion.Euler(12, 34, 56); b.transform.localScale = Vector3.one * 3; var rotation = b.transform.rotation;
            var r = SmartTransformCommands.Layout(dryRun:false, confirm:true, targetsJson:Targets(c, b, a)); Assert.That(r.Ok, Is.True); Near(a.transform.position, Vector3.one); Near(b.transform.position, new Vector3(3, 1, 1)); Near(c.transform.position, new Vector3(5, 1, 1)); SameRotation(b.transform.rotation, rotation); Near(b.transform.localScale, Vector3.one * 3);
        }
        [Test] public void GridUsesPositiveXAndNegativeZWithThreeColumns()
        {
            var objects = Enumerable.Range(0, 5).Select(i => Go("Grid" + i, new Vector3(10, 3, 20))).ToArray();
            var r = SmartTransformCommands.Layout("Grid", "ignored", 2, 3, float.NaN, true, false, true, Targets(objects)); Assert.That(r.Ok, Is.True);
            Near(objects[0].transform.position, new Vector3(10, 3, 20)); Near(objects[2].transform.position, new Vector3(14, 3, 20)); Near(objects[3].transform.position, new Vector3(10, 3, 18)); Near(objects[4].transform.position, new Vector3(12, 3, 18));
        }
        [Test] public void CircleStartsForwardAndQuarterTurnsAroundY()
        {
            var objects = Enumerable.Range(0, 4).Select(i => Go("Circle" + i)).ToArray();
            var r = SmartTransformCommands.Layout("Circle", spacing:2, dryRun:false, confirm:true, targetsJson:Targets(objects)); Assert.That(r.Ok, Is.True);
            Near(objects[0].transform.position, new Vector3(0, 0, 2)); Near(objects[1].transform.position, new Vector3(2, 0, 0)); Near(objects[2].transform.position, new Vector3(0, 0, -2)); Near(objects[3].transform.position, new Vector3(-2, 0, 0));
        }
        [TestCase(0, 0, 2)] [TestCase(180, -2, 0)] [TestCase(360, 0, -2)]
        public void ArcSingletonUsesStartAngle(float angle, float x, float z)
        {
            var a = Go("A"); var r = SmartTransformCommands.Layout("Arc", spacing:2, arcAngle:angle, dryRun:false, confirm:true, targetsJson:Targets(a)); Assert.That(r.Ok, Is.True); Near(a.transform.position, new Vector3(x, 0, z));
        }
        [Test] public void ArcIncludesBothEndsAndFacesCenter()
        {
            var a = Go("A"); var b = Go("B"); var c = Go("C"); var r = SmartTransformCommands.Layout("Arc", spacing:2, lookAtCenter:true, dryRun:false, confirm:true, targetsJson:Targets(a,b,c)); Assert.That(r.Ok, Is.True); Near(a.transform.position, new Vector3(-2,0,0)); Near(b.transform.position, new Vector3(0,0,2)); Near(c.transform.position, new Vector3(2,0,0)); Near(b.transform.forward, Vector3.back);
        }
        [Test] public void ZeroRadiusSkipsCenterFacingWithoutInvalidRotation()
        {
            var a = Go("A"); a.transform.rotation = Quaternion.Euler(10,20,30); var old = a.transform.rotation;
            var r = SmartTransformCommands.Layout("Circle", spacing:0, lookAtCenter:true, dryRun:false, confirm:true, targetsJson:Targets(a)); Assert.That(r.Ok, Is.True); SameRotation(a.transform.rotation, old); Assert.That(r.Result.ChangedCount, Is.Zero);
        }
        [Test] public void EqualSiblingIndicesRetainSuppliedOrderAcrossParents()
        {
            var p = Go("P"); var q = Go("Q"); var a = Go("A", Vector3.one, p); var b = Go("B", Vector3.one * 9, q);
            var r = SmartTransformCommands.Layout(targetsJson:Targets(b,a), dryRun:false, confirm:true); Assert.That(r.Ok, Is.True); Near(b.transform.position, Vector3.one * 9); Near(a.transform.position, new Vector3(11,9,9));
        }
        [Test] public void RandomDefaultsSkipAllDrawsAndPreserveNonuniformScale()
        {
            var a = Go("A"); a.transform.localScale = new Vector3(2,3,4); var state = UnityEngine.Random.state;
            var r = SmartTransformCommands.RandomTransform(dryRun:false, confirm:true, targetsJson:Targets(a)); Assert.That(r.Ok, Is.True); Assert.That(r.Result.ChangedCount, Is.Zero); Near(a.transform.localScale, new Vector3(2,3,4)); Assert.That(UnityEngine.Random.state, Is.EqualTo(state));
        }
        [Test] public void RandomUsesPositionThenWorldEulerThenUniformScaleDrawsInSuppliedOrder()
        {
            var a = Go("A", Vector3.one); var b = Go("B", Vector3.one * 5); var state = UnityEngine.Random.state;
            var positionB = b.transform.position + new Vector3(UnityEngine.Random.Range(-2f,2f), UnityEngine.Random.Range(-2f,2f), UnityEngine.Random.Range(-2f,2f));
            var rotationB = Quaternion.Euler(new Vector3(UnityEngine.Random.Range(-10f,10f), UnityEngine.Random.Range(-10f,10f), UnityEngine.Random.Range(-10f,10f))); var scaleB = UnityEngine.Random.Range(.8f,1.2f);
            var positionA = a.transform.position + new Vector3(UnityEngine.Random.Range(-2f,2f), UnityEngine.Random.Range(-2f,2f), UnityEngine.Random.Range(-2f,2f));
            var rotationA = Quaternion.Euler(new Vector3(UnityEngine.Random.Range(-10f,10f), UnityEngine.Random.Range(-10f,10f), UnityEngine.Random.Range(-10f,10f))); var scaleA = UnityEngine.Random.Range(.8f,1.2f); var after = UnityEngine.Random.state; UnityEngine.Random.state = state;
            var r = SmartTransformCommands.RandomTransform(2,10,.8f,1.2f,false,true,Targets(b,a)); Assert.That(r.Ok, Is.True); Near(b.transform.position, positionB); SameRotation(b.transform.rotation, rotationB); Near(b.transform.localScale, Vector3.one * scaleB); Near(a.transform.position, positionA); SameRotation(a.transform.rotation, rotationA); Near(a.transform.localScale, Vector3.one * scaleA); Assert.That(UnityEngine.Random.state, Is.EqualTo(after)); Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Near(a.transform.position, Vector3.one); Near(b.transform.position, Vector3.one * 5);
        }
        [Test] public void RandomEqualNonunitBoundsSetUniformScale()
        { var a = Go("A"); var r = SmartTransformCommands.RandomTransform(scaleMin:2,scaleMax:2,dryRun:false,confirm:true,targetsJson:Targets(a)); Assert.That(r.Ok, Is.True); Near(a.transform.localScale, Vector3.one * 2); }
        [TestCase("snap")] [TestCase("layout")] [TestCase("random")] [TestCase("distribute")] [TestCase("ground")]
        public void SelectedAncestorOverlapRefusesReversedSelectionBeforeAnyChange(string operation)
        {
            var p = Go("Parent", Vector3.one); p.transform.rotation = Quaternion.Euler(10,30,20); p.transform.localScale = new Vector3(2,3,4); var c = Go("Child", new Vector3(2.3f,4.7f,6.1f), p); var extra = Go("Extra"); var beforeP = p.transform.position; var beforeC = c.transform.position; var local = c.transform.localPosition; var rotation = c.transform.rotation; var state = UnityEngine.Random.state;
            var r = Run(operation, Targets(c,extra,p), false,true); Assert.That(r.Ok, Is.False); Assert.That(r.Error.Code, Is.EqualTo("SMART_SELECTION_OVERLAP")); Near(p.transform.position,beforeP); Near(c.transform.position,beforeC); Near(c.transform.localPosition,local); SameRotation(c.transform.rotation,rotation); Assert.That(UnityEngine.Random.state, Is.EqualTo(state));
        }
        [TestCase("snap")] [TestCase("layout")] [TestCase("random")] [TestCase("distribute")] [TestCase("ground")]
        public void PreviewWinsAndDoesNotDirtyAdvanceRandomOrOpenUndo(string operation)
        {
            var a=Go("A", new Vector3(.2f,5,.6f)); var b=Go("B", new Vector3(99,6,7)); var c=Go("C",new Vector3(10,8,9)); Selection.objects = new UnityEngine.Object[]{c,a}; EditorSceneManager.SaveScene(First); var state=UnityEngine.Random.state; var group=Undo.GetCurrentGroup(); var before=a.transform.position;
            var r=Run(operation, Targets(a,b,c), true,true); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Preview, Is.True); Near(a.transform.position,before); Assert.That(First.isDirty, Is.False); Assert.That(UnityEngine.Random.state, Is.EqualTo(state)); Assert.That(Undo.GetCurrentGroup(), Is.EqualTo(group)); Assert.That(Selection.objects, Is.EqualTo(new UnityEngine.Object[]{c,a}));
        }
        [TestCase("snap")] [TestCase("layout")] [TestCase("random")] [TestCase("distribute")] [TestCase("ground")]
        public void CommitNeedsConfirmationForWholeSet(string operation)
        { var a=Go("A"); var b=Go("B"); var c=Go("C"); var r=Run(operation,Targets(a,b,c),false,false); Assert.That(r.Ok,Is.False); Assert.That(r.Error.Code,Is.EqualTo("SMART_CONFIRM_REQUIRED")); }
        [TestCase("snap")] [TestCase("layout")] [TestCase("random")] [TestCase("distribute")] [TestCase("ground")]
        public void StaleExactTargetRefusesWholeSet(string operation)
        { var a=Go("A",Vector3.one*.3f); var b=Go("B"); var c=Go("C"); var json=Targets(a,b,c); UnityEngine.Object.DestroyImmediate(b); var r=Run(operation,json,false,true); Assert.That(r.Ok,Is.False); Near(a.transform.position,Vector3.one*.3f); }
        [TestCase(0)] [TestCase(-1)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] public void SnapRefusesInvalidGrid(float grid)
        { var a=Go("A"); Assert.That(SmartTransformCommands.SnapGrid(grid,targetsJson:Targets(a)).Error.Code,Is.EqualTo("SMART_INPUT_INVALID")); }
        [TestCase("Bogus")] [TestCase("")] public void LayoutRefusesUnknownShape(string shape)
        { var a=Go("A"); Assert.That(SmartTransformCommands.Layout(shape,targetsJson:Targets(a)).Error.Code,Is.EqualTo("SMART_INPUT_INVALID")); }
        [Test] public void LayoutRefusesApplicableColumnAxisArcAndFiniteBounds()
        { var a=Go("A"); var t=Targets(a); Assert.That(SmartTransformCommands.Layout("Grid",columns:0,targetsJson:t).Ok,Is.False); Assert.That(SmartTransformCommands.Layout("Linear",axis:"bogus",targetsJson:t).Ok,Is.False); Assert.That(SmartTransformCommands.Layout("Arc",arcAngle:361,targetsJson:t).Ok,Is.False); Assert.That(SmartTransformCommands.Layout(spacing:float.NaN,targetsJson:t).Ok,Is.False); Assert.That(SmartTransformCommands.Layout(spacing:-1,targetsJson:t).Ok,Is.False); }
        [Test] public void RandomRefusesInvalidRangesBeforeDrawing()
        { var a=Go("A"); var t=Targets(a); var state=UnityEngine.Random.state; Assert.That(SmartTransformCommands.RandomTransform(posRange:-1,targetsJson:t).Ok,Is.False); Assert.That(SmartTransformCommands.RandomTransform(rotRange:float.NaN,targetsJson:t).Ok,Is.False); Assert.That(SmartTransformCommands.RandomTransform(scaleMin:0,targetsJson:t).Ok,Is.False); Assert.That(SmartTransformCommands.RandomTransform(scaleMin:2,scaleMax:1,targetsJson:t).Ok,Is.False); Assert.That(UnityEngine.Random.state,Is.EqualTo(state)); }
        [Test] public void DistributionRequiresThreeAndNoUnknownAxisFallback()
        { var a=Go("A"); var b=Go("B"); var c=Go("C"); Assert.That(SmartTransformCommands.Distribute(targetsJson:Targets(a,b)).Error.Code,Is.EqualTo("SMART_SELECTION_INVALID")); Assert.That(SmartTransformCommands.Distribute("bad",targetsJson:Targets(a,b,c)).Error.Code,Is.EqualTo("SMART_INPUT_INVALID")); }
        [Test] public void ExplicitDuplicatesAreDeduplicatedAndOmittedTargetsUseSelection()
        { var a=Go("A",Vector3.one*.2f); Selection.objects=new UnityEngine.Object[]{a}; var r=SmartTransformCommands.SnapGrid(targetsJson:Targets(a,a)); Assert.That(r.Ok,Is.True); Assert.That(r.Result.SelectedCount,Is.EqualTo(1)); Assert.That(SmartTransformCommands.SnapGrid().Result.SelectedCount,Is.EqualTo(1)); }
        [Test] public void ConnectedPrefabTransformOverridePersistsSaveReopen()
        { var path=Prefab(); var a=(GameObject)PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path),First); a.transform.position=Vector3.one*.6f; var r=SmartTransformCommands.SnapGrid(dryRun:false,confirm:true,targetsJson:Targets(a)); Assert.That(r.Ok,Is.True); Assert.That(PrefabUtility.GetPropertyModifications(a).Any(x=>x.propertyPath.StartsWith("m_LocalPosition")),Is.True); EditorSceneManager.SaveScene(First); var opened=EditorSceneManager.OpenScene(Folder+"/Base.unity"); Near(opened.GetRootGameObjects().Single().transform.position,Vector3.one); }
        [TestCase("snap")] [TestCase("layout")] [TestCase("random")] [TestCase("distribute")] [TestCase("ground")]
        public void HiddenObjectInSetRefusesBeforeAnyOrdinaryObjectChanges(string operation)
        { var a=Go("A",Vector3.one*.3f); var b=Go("Hidden"); b.hideFlags=HideFlags.HideAndDontSave; var c=Go("C"); try { var r=Run(operation,Targets(a,b,c),false,true); Assert.That(r.Ok,Is.False); Assert.That(r.Error.Code,Is.EqualTo("SMART_TARGET_INELIGIBLE")); Near(a.transform.position,Vector3.one*.3f); } finally { UnityEngine.Object.DestroyImmediate(b); } }
        [TestCase("snap")] [TestCase("layout")] [TestCase("random")] [TestCase("distribute")] [TestCase("ground")]
        public void PreviewSceneObjectInSetRefusesBeforeAnyOrdinaryObjectChanges(string operation)
        { var a=Go("A",Vector3.one*.3f); var c=Go("C"); var preview=EditorSceneManager.NewPreviewScene(); var b=new GameObject("Preview"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(b,preview); try { var r=Run(operation,Targets(a,b,c),false,true); Assert.That(r.Ok,Is.False); Assert.That(r.Error.Code,Is.EqualTo("SMART_TARGET_INELIGIBLE")); Near(a.transform.position,Vector3.one*.3f); } finally { EditorSceneManager.ClosePreviewScene(preview); } }
        [TestCase("snap")] [TestCase("layout")] [TestCase("random")] [TestCase("distribute")] [TestCase("ground")]
        public void PersistentPrefabTargetInSetRefusesBeforeAnyOrdinaryObjectChanges(string operation)
        { var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab()); var a=Go("A",Vector3.one*.3f); var c=Go("C"); var r=Run(operation,Targets(a,prefab,c),false,true); Assert.That(r.Ok,Is.False); Assert.That(r.Error.Code,Is.EqualTo("SMART_TARGET_INELIGIBLE")); Near(a.transform.position,Vector3.one*.3f); }
        [TestCase("snap")] [TestCase("layout")] [TestCase("random")] [TestCase("distribute")] [TestCase("ground")]
        public void EmptyAndMalformedExplicitSetsRefuse(string operation)
        { Assert.That(Run(operation,"[]",true,false).Error.Code,Is.EqualTo("SMART_SELECTION_INVALID")); Assert.That(Run(operation,"[1]",true,false).Error.Code,Is.EqualTo("SMART_INPUT_INVALID")); Assert.That(Run(operation,"{",true,false).Error.Code,Is.EqualTo("SMART_INPUT_INVALID")); }
        private CommandResult<SmartTransformReport> Run(string op,string targets,bool dryRun,bool confirm)
        { switch(op) { case "snap":return SmartTransformCommands.SnapGrid(1,dryRun,confirm,targets); case "layout":return SmartTransformCommands.Layout(dryRun:dryRun,confirm:confirm,targetsJson:targets); case "random":return SmartTransformCommands.RandomTransform(1,10,.8f,1.2f,dryRun,confirm,targets); case "distribute":return SmartTransformCommands.Distribute("X",dryRun,confirm,targets); default:return SmartTransformCommands.AlignGround(100,false,dryRun,confirm,targets); } }
    }
}
