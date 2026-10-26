using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class SmartBindingContractTests : SmartFixtureSupport
    {
        private string TypeName=>typeof(SmartBindingFixture).FullName;
        private SmartBindingFixture Target()=>Go("Target").AddComponent<SmartBindingFixture>();
        [Test] public void SerializedArrayPlanProvidesExactNativePayloadWithoutMutation()
        {
            var t=Target(); var a=Go("SourceA"); var b=Go("SourceB"); t.Objects=new[]{b,null,b}; EditorSceneManager.SaveScene(First); var group=Undo.GetCurrentGroup();
            var r=SmartBindingCommands.ReferencePlan("Target",TypeName,"Objects",sourceName:"Source",appendMode:true); Assert.That(r.Ok,Is.True); Assert.That(r.Result.Component,Is.EqualTo(Id(t))); Assert.That(r.Result.PropertyPath,Is.EqualTo("Objects")); Assert.That(r.Result.OriginalReferences,Is.EqualTo(new[]{Id(b),null,Id(b)})); Assert.That(r.Result.ProposedReferences,Is.EqualTo(new[]{Id(b),null,Id(b),Id(a)})); Assert.That(r.Result.PropertiesJson,Does.Contain("Objects")); Assert.That(r.Result.PropertiesJson,Does.Contain(Id(a))); Assert.That(t.Objects,Is.EqualTo(new[]{b,null,b})); Assert.That(First.isDirty,Is.False); Assert.That(Undo.GetCurrentGroup(),Is.EqualTo(group));
        }
        [Test] public void SerializedComponentListPlanSkipsMissingComponentAndPreservesAppendNulls()
        { var t=Target(); var a=Go("SourceA").AddComponent<Light>(); Go("SourceMissing"); var b=Go("SourceB").AddComponent<Light>(); t.Lights.Add(null); t.Lights.Add(b); var r=SmartBindingCommands.ReferencePlan(null,null,"Lights",sourceName:"Source",appendMode:true,target:Id(t)); Assert.That(r.Ok,Is.True); Assert.That(r.Result.ProposedReferences,Is.EqualTo(new[]{(string)null,Id(b),Id(a)})); Assert.That(r.Result.SkippedSources,Is.EqualTo(1)); Assert.That(t.Lights.Count,Is.EqualTo(2)); }
        [TestCase("aliased","m_Aliased")] [TestCase("other","_other")] public void SerializedPublicFieldAliasesResolveActualPropertyPath(string member,string path)
        { Target(); Go("Source"); var r=SmartBindingCommands.ReferencePlan("Target",TypeName,member,sourceName:"Source"); Assert.That(r.Ok,Is.True); Assert.That(r.Result.PropertyPath,Is.EqualTo(path)); }
        [Test] public void PlanRefusesVolatileFieldAndDoesNotExecutePropertyGetter()
        { var t=Target(); Go("Source"); Assert.That(SmartBindingCommands.ReferencePlan("Target",TypeName,"VolatileField",sourceName:"Source").Error.Code,Is.EqualTo("SMART_SERIALIZED_MEMBER_UNSUPPORTED")); Assert.That(SmartBindingCommands.ReferencePlan("Target",TypeName,"Backed",sourceName:"Source").Ok,Is.False); Assert.That(t.GetterCalls,Is.Zero); }
        [Test] public void DuplicateNamedTargetAndDuplicateComponentRefuseButExactComponentDisambiguates()
        { var t=Target(); var extra=t.gameObject.AddComponent<SmartBindingFixture>(); Go("Source"); Assert.That(SmartBindingCommands.ReferencePlan("Target",TypeName,"Objects",sourceName:"Source").Error.Code,Is.EqualTo("SMART_TARGET_AMBIGUOUS")); Assert.That(SmartBindingCommands.ReferencePlan(null,null,"Objects",sourceName:"Source",target:Id(extra)).Ok,Is.True); Go("Target").AddComponent<SmartBindingFixture>(); Assert.That(SmartBindingCommands.ReferencePlan("Target",TypeName,"Objects",sourceName:"Source").Error.Code,Is.EqualTo("SMART_TARGET_AMBIGUOUS")); }
        [Test] public void NameSourceExcludesInactiveAndTagNameUnionDeduplicatesTagFirst()
        { Target(); var tagged=Go("SourceTagged"); tagged.tag="Player"; var first=Go("SourceFirst"); var inactive=Go("SourceInactive"); inactive.SetActive(false); var r=SmartBindingCommands.ReferencePlan("Target",TypeName,"Objects","Player","Source"); Assert.That(r.Ok,Is.True); Assert.That(r.Result.ProposedReferences,Is.EqualTo(new[]{Id(tagged),Id(first)})); }
        [Test] public void InvalidTagRefusesEvenWhenNameWouldMatchAndNoSelectorRefuses()
        { Target(); Go("Source"); Assert.That(SmartBindingCommands.ReferencePlan("Target",TypeName,"Objects","__Missing3393","Source").Error.Code,Is.EqualTo("SMART_SOURCE_INVALID")); Assert.That(SmartBindingCommands.ReferencePlan("Target",TypeName,"Objects").Ok,Is.False); }
        [Test] public void PropertyRequiresExplicitOptInBeforeGetterAndDryRunWins()
        { var t=Target(); Go("Source"); var no=SmartBindingCommands.ReferenceProperty("Target",TypeName,"Backed",sourceName:"Source",appendMode:true); Assert.That(no.Ok,Is.False); Assert.That(no.Error.Code,Is.EqualTo("SMART_PROPERTY_ACCESS_REQUIRED")); Assert.That(t.GetterCalls,Is.Zero); var preview=SmartBindingCommands.ReferenceProperty("Target",TypeName,"Backed",sourceName:"Source",allowPropertyAccess:true,confirm:true); Assert.That(preview.Ok,Is.True); Assert.That(preview.Result.Preview,Is.True); Assert.That(t.ReadBacking(),Is.Empty); Assert.That(t.GetterCalls,Is.Zero); }
        [Test] public void SerializedBackedPropertyPersistsAndUndoesTargetSerializedState()
        { var t=Target(); var source=Go("Source"); var r=SmartBindingCommands.ReferenceProperty(null,null,"Backed",sourceName:"Source",allowPropertyAccess:true,dryRun:false,confirm:true,target:Id(t)); Assert.That(r.Ok,Is.True); Assert.That(r.Result.UndoScope,Is.EqualTo("TargetComponentSerializedState")); Assert.That(t.ReadBacking(),Is.EqualTo(new[]{source})); Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.That(t.ReadBacking(),Is.Empty); Undo.PerformRedo(); Assert.That(t.ReadBacking(),Is.EqualTo(new[]{source})); EditorSceneManager.SaveScene(First); var scene=EditorSceneManager.OpenScene(Folder+"/Base.unity"); var restored=scene.GetRootGameObjects().Single(x=>x.name=="Target").GetComponent<SmartBindingFixture>(); Assert.That(restored.ReadBacking().Single().name,Is.EqualTo("Source")); }
        [Test] public void VolatilePropertyTruthfullyReportsNoSerializedStateChange()
        { var t=Target(); Go("Source"); var r=SmartBindingCommands.ReferenceProperty("Target",TypeName,"Volatile",sourceName:"Source",allowPropertyAccess:true,dryRun:false,confirm:true); Assert.That(r.Ok,Is.True); Assert.That(t.Volatile.Length,Is.EqualTo(1)); Assert.That(r.Result.SerializedStateChanged,Is.False); Assert.That(r.Result.PersistenceScope,Is.EqualTo("TargetComponentSerializedStateOnly")); Assert.That(r.Result.ExternalSetterEffectsRollbackGuaranteed,Is.False); }
        [Test] public void AppendGetterFailureIsTruthfulAndReplaceDoesNotNeedPrivateGetter()
        { var t=Target(); var source=Go("Source"); var r=SmartBindingCommands.ReferenceProperty("Target",TypeName,"ThrowGetter",sourceName:"Source",appendMode:true,allowPropertyAccess:true,dryRun:false,confirm:true); Assert.That(r.Ok,Is.False); Assert.That(r.Error.Code,Is.EqualTo("SMART_PROPERTY_GETTER_FAILED")); Assert.That(t.ReadBacking(),Is.Empty); Assert.That(SmartBindingCommands.ReferenceProperty("Target",TypeName,"PrivateGetter",sourceName:"Source",appendMode:true,allowPropertyAccess:true).Ok,Is.False); var replace=SmartBindingCommands.ReferenceProperty("Target",TypeName,"PrivateGetter",sourceName:"Source",allowPropertyAccess:true,dryRun:false,confirm:true); Assert.That(replace.Ok,Is.True); Assert.That(t.ReadBacking(),Is.EqualTo(new[]{source})); }
        [Test] public void SetterFailureRestoresTargetSerializedStateAndAdmitsExternalBoundary()
        { var t=Target(); Go("Source"); var r=SmartBindingCommands.ReferenceProperty("Target",TypeName,"ThrowSetter",sourceName:"Source",allowPropertyAccess:true,dryRun:false,confirm:true); Assert.That(r.Ok,Is.False); Assert.That(r.Error.Code,Is.EqualTo("SMART_PROPERTY_SETTER_FAILED")); Assert.That(t.ReadBacking(),Is.Empty); Assert.That(r.Error.Details["targetSerializedRollback"],Is.EqualTo(true)); Assert.That(r.Error.Details["externalSetterEffectsRollbackGuaranteed"],Is.EqualTo(false)); }
        [TestCase("Readonly")] [TestCase("Static")] [TestCase("Item")] public void PropertyRejectsReadonlyStaticAndIndexer(string member)
        { Target(); Go("Source"); Assert.That(SmartBindingCommands.ReferenceProperty("Target",TypeName,member,sourceName:"Source",allowPropertyAccess:true).Error.Code,Is.EqualTo("SMART_MEMBER_INVALID")); }
        [Test] public void PropertyAppendPreservesNullsAndExistingDuplicatesWithoutDuplicatingNewItems()
        { var t=Target(); var a=Go("SourceA"); var b=Go("SourceB"); t.Backed=new[]{a,null,a}; var r=SmartBindingCommands.ReferenceProperty("Target",TypeName,"Backed",sourceName:"Source",appendMode:true,allowPropertyAccess:true,dryRun:false,confirm:true); Assert.That(r.Ok,Is.True); Assert.That(t.ReadBacking(),Is.EqualTo(new[]{a,null,a,b})); Assert.That(r.Result.BoundCount,Is.EqualTo(4)); }
        [Test] public void PropertyRefusesDuplicateComponentsAndCommitWithoutConfirmationBeforeGetter()
        { var t=Target(); Go("Source"); Assert.That(SmartBindingCommands.ReferenceProperty("Target",TypeName,"Backed",sourceName:"Source",appendMode:true,allowPropertyAccess:true,dryRun:false).Error.Code,Is.EqualTo("SMART_CONFIRM_REQUIRED")); Assert.That(t.GetterCalls,Is.Zero); t.gameObject.AddComponent<SmartBindingFixture>(); Assert.That(SmartBindingCommands.ReferenceProperty("Target",TypeName,"Backed",sourceName:"Source",appendMode:true,allowPropertyAccess:true).Error.Code,Is.EqualTo("SMART_TARGET_AMBIGUOUS")); Assert.That(t.GetterCalls,Is.Zero); }
        [TestCase("MutatingGetter", "SMART_PROPERTY_GETTER_MUTATED_TARGET", true)]
        [TestCase("MutatingThrowGetter", "SMART_PROPERTY_GETTER_FAILED", true)]
        [TestCase("MutatingThrowGetter", "SMART_PROPERTY_GETTER_FAILED", false)]
        [TestCase("ThrowSetter", "SMART_PROPERTY_SETTER_FAILED", false)]
        [TestCase("ExternalThrowSetter", "SMART_PROPERTY_SETTER_FAILED", false)]
        public void PropertyFailurePreservesPopulatedReferencesAcrossScenes(string member, string code, bool preview)
        {
            var t = Target();
            var local = Go("Local reference");
            var light = local.AddComponent<Light>();
            Go("Source replacement");
            var additive = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var external = new GameObject("External reference");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(external, additive);
            var externalLight = external.AddComponent<Light>();
            Assert.That(EditorSceneManager.SaveScene(First), Is.True);
            Assert.That(EditorSceneManager.SaveScene(additive, Folder + "/Additive.unity"), Is.True);
            t.External = external;
            t.Objects = new[] { local, null, local, external };
            t.Lights.AddRange(new[] { light, null, light, externalLight });
            t.Backed = new[] { external, null, external, local };
            Selection.objects = new UnityEngine.Object[] { local, external };
            var selection = Selection.objects;
            var random = UnityEngine.Random.state;
            var group = Undo.GetCurrentGroup();
            var externalId = external.GetEntityId();
            Assert.That(t.External, Is.SameAs(external), "Fixture must start with a live external reference.");

            var result = SmartBindingCommands.ReferenceProperty(null, null, member, sourceName: "Source replacement",
                appendMode: member != "ThrowSetter" && member != "ExternalThrowSetter", allowPropertyAccess: true,
                dryRun: preview, confirm: !preview, target: Id(t));

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(t.ReadBacking(), Is.EqualTo(new[] { external, null, external, local }), "Restore the actual array references, nulls and duplicates.");
            Assert.That(t.Objects, Is.EqualTo(new[] { local, null, local, external }));
            Assert.That(t.Lights, Is.EqualTo(new[] { light, null, light, externalLight }));
            Assert.That(t.External, Is.SameAs(external));
            Assert.That(t.External.GetEntityId(), Is.EqualTo(externalId));
            Assert.That(t.Marker, Is.Zero);
            Assert.That(result.Error.Details["targetSerializedRollback"], Is.EqualTo(true));
            Assert.That(result.Error.Details["externalSetterEffectsRollbackGuaranteed"], Is.EqualTo(false));
            Assert.That(external.name, Is.EqualTo(member == "ExternalThrowSetter" ? "External setter effect" : "External reference"));
            if (preview)
            {
                Assert.That(Undo.GetCurrentGroup(), Is.EqualTo(group));
                Assert.That(First.isDirty, Is.False);
                Assert.That(additive.isDirty, Is.False);
                Assert.That(EditorUtility.IsDirty(t), Is.False);
                Assert.That(Selection.objects, Is.EqualTo(selection));
                Assert.That(UnityEngine.Random.state, Is.EqualTo(random));
            }
        }
        [Test] public void ReferenceOnlyGetterMutationIsDetectedAndRestoredDuringPreview()
        {
            var t = Target(); var original = Go("Original reference"); Go("Source replacement");
            Assert.That(EditorSceneManager.SaveScene(First), Is.True); t.External = original;
            var group = Undo.GetCurrentGroup();
            var result = SmartBindingCommands.ReferenceProperty(null, null, "ReferenceOnlyGetter", sourceName: "Source replacement",
                appendMode: true, allowPropertyAccess: true, target: Id(t));
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("SMART_PROPERTY_GETTER_MUTATED_TARGET"));
            Assert.That(t.External, Is.SameAs(original));
            Assert.That(result.Error.Details["targetSerializedRollback"], Is.EqualTo(true));
            Assert.That(Undo.GetCurrentGroup(), Is.EqualTo(group));
            Assert.That(First.isDirty, Is.False);
        }
        [Test] public void ReferenceOnlySetterChangeIsReportedAndUndoesExactReference()
        {
            var t = Target(); var original = Go("Original reference"); var replacement = Go("Source replacement");
            t.External = original;
            var result = SmartBindingCommands.ReferenceProperty(null, null, "ReferenceOnlySetter", sourceName: "Source replacement",
                allowPropertyAccess: true, dryRun: false, confirm: true, target: Id(t));
            Assert.That(result.Ok, Is.True);
            Assert.That(t.External, Is.SameAs(replacement));
            Assert.That(result.Result.SerializedStateChanged, Is.True);
            Assert.That(First.isDirty, Is.True);
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
            Assert.That(t.External, Is.SameAs(original));
            Undo.PerformRedo(); Assert.That(t.External, Is.SameAs(replacement));
        }
        [TestCase("MutatingGetter", "SMART_PROPERTY_GETTER_MUTATED_TARGET", false)]
        [TestCase("MutatingGetter", "SMART_PROPERTY_GETTER_MUTATED_TARGET", true)]
        [TestCase("MutatingThrowGetter", "SMART_PROPERTY_GETTER_FAILED", false)]
        [TestCase("MutatingThrowGetter", "SMART_PROPERTY_GETTER_FAILED", true)]
        public void PreviewPreservesMissingReferencesAndOriginalDirtyState(string member, string code, bool initiallyDirty)
        {
            var t = Target(); var local = Go("Local reference"); var light = local.AddComponent<Light>();
            Go("Source replacement");
            Assert.That(EditorSceneManager.SaveScene(First), Is.True);
            var additive = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var external = new GameObject("External reference");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(external, additive);
            var externalLight = external.AddComponent<Light>();
            Assert.That(EditorSceneManager.SaveScene(First), Is.True);
            Assert.That(EditorSceneManager.SaveScene(additive, Folder + "/Additive.unity"), Is.True);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(First);
            t.External = external; t.Objects = new[] { local, null, local, external };
            t.Lights.AddRange(new[] { light, null, light, externalLight });
            t.Backed = new[] { external, null, external, local };
            var removed = Go("Removed reference"); t._other = new[] { removed, null, removed };
            UnityEngine.Object.DestroyImmediate(removed);
            using (var serialized = new SerializedObject(t))
            {
                var missing = serialized.FindProperty("_other.Array.data[0]");
                Assert.That(missing.objectReferenceValue, Is.Null);
                Assert.That(missing.objectReferenceEntityIdValue, Is.Not.EqualTo(default(EntityId)));
                Assert.That(serialized.FindProperty("_other.Array.data[1]").objectReferenceEntityIdValue, Is.EqualTo(default(EntityId)));
            }
            if (initiallyDirty) EditorUtility.SetDirty(t);
            Assert.That(EditorUtility.IsDirty(t), Is.EqualTo(initiallyDirty));
            Selection.objects = new UnityEngine.Object[] { local, external };
            var selection = Selection.objects; var random = UnityEngine.Random.state; var group = Undo.GetCurrentGroup();
            var sceneDirty = new[] { First.isDirty, additive.isDirty };
            Func<System.Collections.Generic.Dictionary<string, EntityId>> readReferences = () =>
            {
                var values = new System.Collections.Generic.Dictionary<string, EntityId>();
                using (var serialized = new SerializedObject(t))
                using (var property = serialized.GetIterator())
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            values.Add(property.propertyPath, property.objectReferenceEntityIdValue);
                return values;
            };
            var references = readReferences();
            var result = SmartBindingCommands.ReferenceProperty(null, null, member, sourceName: "Source replacement",
                appendMode: true, allowPropertyAccess: true, target: Id(t));
            Assert.That(result.Ok, Is.False); Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Details["targetSerializedRollback"], Is.EqualTo(true));
            Assert.That(result.Error.Details["externalSetterEffectsRollbackGuaranteed"], Is.EqualTo(false));
            Assert.That(readReferences(), Is.EquivalentTo(references));
            Assert.That(t.External, Is.SameAs(external)); Assert.That(t.Marker, Is.Zero);
            Assert.That(t.Objects, Is.EqualTo(new[] { local, null, local, external }));
            Assert.That(t.Lights, Is.EqualTo(new[] { light, null, light, externalLight }));
            Assert.That(t.ReadBacking(), Is.EqualTo(new[] { external, null, external, local }));
            Assert.That(t._other.Length, Is.EqualTo(3));
            using (var serialized = new SerializedObject(t))
            {
                Assert.That(serialized.FindProperty("_other.Array.data[0]").objectReferenceValue, Is.Null);
                Assert.That(serialized.FindProperty("_other.Array.data[1]").objectReferenceValue, Is.Null);
                Assert.That(serialized.FindProperty("_other.Array.data[2]").objectReferenceValue, Is.Null);
            }
            Assert.That(external.name, Is.EqualTo("External reference"));
            Assert.That(EditorUtility.IsDirty(t), Is.EqualTo(initiallyDirty));
            Assert.That(new[] { First.isDirty, additive.isDirty }, Is.EqualTo(sceneDirty));
            Assert.That(Undo.GetCurrentGroup(), Is.EqualTo(group));
            Assert.That(Selection.objects, Is.EqualTo(selection));
            Assert.That(UnityEngine.Random.state, Is.EqualTo(random));
        }
    }
}
