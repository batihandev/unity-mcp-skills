using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class SmartGroundContractTests : SmartFixtureSupport
    {
        [Test] public void GroundFiltersLargeSelfAndChildCollidersChoosesNearestExternalAndPreservesMiss()
        {
            var ground=Go("Ground",new Vector3(0,-.5f,0)); ground.AddComponent<BoxCollider>().size=new Vector3(20,1,20);
            var upper=Go("Upper",new Vector3(0,1,0)); upper.AddComponent<BoxCollider>().size=new Vector3(4,.2f,4);
            var a=Go("Selected",new Vector3(0,5,0)); a.AddComponent<BoxCollider>().size=new Vector3(2,20,2); var child=Go("SelfChild",new Vector3(0,3,0),a); child.AddComponent<BoxCollider>().size=Vector3.one*2;
            var miss=Go("Miss",new Vector3(50,7,0)); Physics.SyncTransforms(); var r=SmartTransformCommands.AlignGround(dryRun:false,confirm:true,targetsJson:Targets(a,miss)); Assert.That(r.Ok,Is.True); Near(a.transform.position,new Vector3(0,1.1f,0)); Near(miss.transform.position,new Vector3(50,7,0)); Assert.That(r.Result.HitCount,Is.EqualTo(1)); Assert.That(r.Result.ProcessedCount,Is.EqualTo(2)); Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Near(a.transform.position,new Vector3(0,5,0)); Near(miss.transform.position,new Vector3(50,7,0));
        }
        [Test] public void SlopedGroundActuallyAlignsUpToNormal()
        {
            var ground=Go("Slope"); ground.transform.rotation=Quaternion.Euler(0,0,20); ground.AddComponent<BoxCollider>().size=new Vector3(20,.1f,20); var a=Go("A",new Vector3(0,5,0)); Physics.SyncTransforms(); var normal=ground.transform.up;
            var r=SmartTransformCommands.AlignGround(alignRotation:true,dryRun:false,confirm:true,targetsJson:Targets(a)); Assert.That(r.Ok,Is.True); Near(a.transform.up,normal); Assert.That(a.transform.position.y,Is.LessThan(.1f));
        }
        [Test] public void RayDistanceOriginAndNoAlignmentDefaultsAreMeasured()
        {
            var ground=Go("Ground",new Vector3(0,-.5f,0)); ground.AddComponent<BoxCollider>().size=new Vector3(20,1,20); var a=Go("A",new Vector3(0,5,0)); a.transform.rotation=Quaternion.Euler(10,20,30); var rotation=a.transform.rotation; Physics.SyncTransforms();
            var miss=SmartTransformCommands.AlignGround(5,false,false,true,Targets(a)); Assert.That(miss.Ok,Is.True); Assert.That(miss.Result.HitCount,Is.Zero); Near(a.transform.position,new Vector3(0,5,0)); var hit=SmartTransformCommands.AlignGround(5.2f,false,false,true,Targets(a)); Assert.That(hit.Ok,Is.True); Near(a.transform.position,Vector3.zero); SameRotation(a.transform.rotation,rotation);
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] public void GroundDistanceMustBePositiveFinite(float distance)
        { var a=Go("A"); Assert.That(SmartTransformCommands.AlignGround(distance,targetsJson:Targets(a)).Error.Code,Is.EqualTo("SMART_INPUT_INVALID")); }
        [Test] public void GroundRetainsGlobalTriggerPolicy()
        {
            var old=Physics.queriesHitTriggers; try { var trigger=Go("Trigger",new Vector3(0,-.5f,0)); var col=trigger.AddComponent<BoxCollider>(); col.size=new Vector3(20,1,20); col.isTrigger=true; var a=Go("A",new Vector3(0,5,0)); Physics.SyncTransforms(); Physics.queriesHitTriggers=false; var r=SmartTransformCommands.AlignGround(targetsJson:Targets(a)); Assert.That(r.Ok,Is.True); Assert.That(r.Result.HitCount,Is.Zero); Physics.queriesHitTriggers=true; Assert.That(SmartTransformCommands.AlignGround(targetsJson:Targets(a)).Result.HitCount,Is.EqualTo(1)); } finally { Physics.queriesHitTriggers=old; }
        }
    }
}
