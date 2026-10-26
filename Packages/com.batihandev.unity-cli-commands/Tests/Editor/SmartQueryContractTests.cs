using System;
using System.Globalization;
using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class SmartQueryContractTests : SmartFixtureSupport
    {
        private string TypeName => typeof(SmartQueryFixture).FullName;
        [TestCase("==","2",1)] [TestCase("!=","2",2)] [TestCase(">","2",1)] [TestCase("<","2",1)] [TestCase(">=","2",2)] [TestCase("<=","2",2)] [TestCase("contains","2",1)]
        public void QuerySupportsAllSevenOperatorsAndRetainsDistinctComponentInstances(string op,string value,int count)
        {
            var go=Go("Multi"); go.AddComponent<SmartQueryFixture>().Number=1; go.AddComponent<SmartQueryFixture>().Number=2; go.AddComponent<SmartQueryFixture>().Number=3;
            var r=SmartQueryCommands.Query(TypeName,"Number",op,value); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Total,Is.EqualTo(count)); Assert.That(r.Result.Items.Select(x=>x.Component).Distinct().Count(),Is.EqualTo(count)); Assert.That(r.Result.Items.Select(x=>x.GameObject).Distinct().Count(),Is.EqualTo(1));
        }
        [TestCase("==","0.0001",0)] [TestCase("!=","0.0001",1)] [TestCase("==","0.000099",1)] [TestCase("!=","0.000099",0)] [TestCase("==","-0.0001",0)]
        public void NumericEqualityHasStrictPoint0001Boundary(string op,string operand,int count)
        { Go("Zero").AddComponent<SmartQueryFixture>().Number=0; var r=SmartQueryCommands.Query(TypeName,"Number",op,operand); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Total,Is.EqualTo(count)); }
        [Test] public void QueryUsesInvariantNumericRepresentationUnderCommaCulture()
        { var culture=CultureInfo.CurrentCulture; try { CultureInfo.CurrentCulture=new CultureInfo("fr-FR"); Go("Fraction").AddComponent<SmartQueryFixture>().Number=1.5f; var r=SmartQueryCommands.Query(TypeName,"Number","==","1.5"); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Items.Single().Value,Is.EqualTo("1.5")); } finally { CultureInfo.CurrentCulture=culture; } }
        [TestCase("==","alpha",0)] [TestCase("==","Alpha",1)] [TestCase("contains","ALP",1)] [TestCase("contains","",1)]
        public void StringEqualityIsOrdinalAndContainsFoldsCase(string op,string value,int count)
        { Go("Text").AddComponent<SmartQueryFixture>().Text="Alpha"; var r=SmartQueryCommands.Query(TypeName,"Text",op,value); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Total,Is.EqualTo(count)); }
        [Test] public void NullMembersSkipAndNullOperandsRefuseForEveryOperator()
        { Go("Null").AddComponent<SmartQueryFixture>(); var r=SmartQueryCommands.Query(TypeName,"Optional","contains",""); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Total,Is.Zero); foreach(var op in new[]{"==","!=",">","<",">=","<=","contains"}) Assert.That(SmartQueryCommands.Query(TypeName,"Text",op,null).Error.Code,Is.EqualTo("SMART_INPUT_INVALID")); }
        [TestCase("bogus")] [TestCase("NaN")] [TestCase("Infinity")] public void RelationalOperandMustBeFiniteInvariantNumber(string value)
        { Go("Number").AddComponent<SmartQueryFixture>(); Assert.That(SmartQueryCommands.Query(TypeName,"Number",">",value).Error.Code,Is.EqualTo("SMART_INPUT_INVALID")); }
        [Test] public void LiteralNaNStringRemainsOrdinaryStringEquality()
        { Go("Text").AddComponent<SmartQueryFixture>().Text="NaN"; var r=SmartQueryCommands.Query(TypeName,"Text","==","NaN"); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Total,Is.EqualTo(1)); }
        [Test] public void FullScanTotalsAndGetterDiagnosticsSurviveZeroCap()
        { Go("A").AddComponent<SmartQueryFixture>().Number=3; Go("B").AddComponent<SmartQueryFixture>().Number=3; var r=SmartQueryCommands.Query(TypeName,"Number",">","2",0); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Items,Is.Empty); Assert.That(r.Result.Total,Is.EqualTo(2)); Assert.That(r.Result.Truncated,Is.True); var throwing=SmartQueryCommands.Query(TypeName,"Throwing",">","2",0); Assert.That(throwing.Ok,Is.True); Assert.That(throwing.Result.Diagnostics.Count,Is.EqualTo(2)); Assert.That(throwing.Result.Diagnostics.All(x=>x.Code=="GETTER_FAILED"),Is.True); }
        [Test] public void QueryRefusesUnknownTypeMemberOperatorShorthandAndNegativeCap()
        { Assert.That(SmartQueryCommands.Query("Missing3393","Number","==","1").Error.Code,Is.EqualTo("SMART_TYPE_INVALID")); Assert.That(SmartQueryCommands.Query("System.String","Length","==","1").Ok,Is.False); Assert.That(SmartQueryCommands.Query(TypeName,"Missing","==","1").Error.Code,Is.EqualTo("SMART_MEMBER_INVALID")); Assert.That(SmartQueryCommands.Query(TypeName,"Number","like","1").Ok,Is.False); Assert.That(SmartQueryCommands.Query(query:"Light.intensity > 2").Ok,Is.False); Assert.That(SmartQueryCommands.Query(TypeName,"Number","==","1",-1).Ok,Is.False); }
        [Test] public void QueryScansActiveOrdinaryAcrossTwoScenesWithoutAuthoringSideEffects()
        {
            var a=Go("A"); a.AddComponent<SmartQueryFixture>().Number=2; Go("Inactive").AddComponent<SmartQueryFixture>().Number=2; First.GetRootGameObjects().Single(x=>x.name=="Inactive").SetActive(false);
            var second=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive); var b=new GameObject("Other"); SceneManager.MoveGameObjectToScene(b,second); b.AddComponent<SmartQueryFixture>().Number=2; SceneManager.SetActiveScene(First); Selection.activeGameObject=a; EditorSceneManager.SaveScene(First); var group=Undo.GetCurrentGroup(); var r=SmartQueryCommands.Query(TypeName,"Number","==","2"); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Total,Is.EqualTo(2)); Assert.That(First.isDirty,Is.False); Assert.That(Selection.activeGameObject,Is.SameAs(a)); Assert.That(Undo.GetCurrentGroup(),Is.EqualTo(group));
        }
        [Test] public void SphereRetainsColliderOccurrencesSurfaceOverlapAndNativeOrder()
        {
            var a=Go("Double"); a.AddComponent<BoxCollider>(); a.AddComponent<SphereCollider>(); var far=Go("Surface",new Vector3(5,0,0)); far.AddComponent<BoxCollider>().size=new Vector3(10,1,1); Go("NoCollider"); Physics.SyncTransforms(); var observed=Physics.OverlapSphere(Vector3.zero,1).Select(x=>Id(x)).ToArray();
            var r=SmartQueryCommands.Spatial(0,0,0,1); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Total,Is.EqualTo(3)); Assert.That(r.Result.Items.Select(x=>x.Collider),Is.EqualTo(observed)); Assert.That(r.Result.Items.Count(x=>x.GameObject==Id(a)),Is.EqualTo(2)); Assert.That(r.Result.Items.Single(x=>x.GameObject==Id(far)).Distance,Is.EqualTo(5));
        }
        [Test] public void SphereComponentFilterAndZeroCapKeepFullTotals()
        { var a=Go("Light"); a.AddComponent<Light>(); a.AddComponent<BoxCollider>(); var b=Go("Other"); b.AddComponent<BoxCollider>(); Physics.SyncTransforms(); var r=SmartQueryCommands.Spatial(0,0,0,1,"unityengine.light",0); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Items,Is.Empty); Assert.That(r.Result.Total,Is.EqualTo(1)); Assert.That(r.Result.Truncated,Is.True); }
        [TestCase(-1)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] public void SphereRejectsInvalidRadius(float radius)
        { Assert.That(SmartQueryCommands.Spatial(0,0,0,radius).Error.Code,Is.EqualTo("SMART_INPUT_INVALID")); }
        [Test] public void SphereRejectsInvalidCenterFilterAndCap()
        { Assert.That(SmartQueryCommands.Spatial(float.NaN,0,0).Ok,Is.False); Assert.That(SmartQueryCommands.Spatial(0,0,0,componentFilter:"Missing3393").Error.Code,Is.EqualTo("SMART_TYPE_INVALID")); Assert.That(SmartQueryCommands.Spatial(0,0,0,componentFilter:"System.String").Ok,Is.False); Assert.That(SmartQueryCommands.Spatial(0,0,0,limit:-1).Ok,Is.False); }
        [Test] public void SphereZeroRadiusAndTriggerGlobalPolicyRemainPhysicsQueries()
        { var a=Go("A"); var collider=a.AddComponent<BoxCollider>(); collider.isTrigger=true; Physics.SyncTransforms(); var old=Physics.queriesHitTriggers; try { Physics.queriesHitTriggers=false; var no=SmartQueryCommands.Spatial(0,0,0,0); Assert.That(no.Ok,Is.True); Assert.That(no.Result.Total,Is.Zero); Physics.queriesHitTriggers=true; Assert.That(SmartQueryCommands.Spatial(0,0,0,0).Result.Total,Is.EqualTo(1)); } finally { Physics.queriesHitTriggers=old; } }
    }
}
