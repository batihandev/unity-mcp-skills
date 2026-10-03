using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using UnityEditor;
using UnityEngine;
using UnityComponent = UnityEngine.Component;
using UnityObject = UnityEngine.Object;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class SmartBindingCommands
    {
        [CliCommand("smart.reference-plan", "Smart reference-plan operation.", Tags = new[] { "unity-cli-commands", "smart" })]
        public static CommandResult<SmartReferencePlan> ReferencePlan([CliArg("targetName", "targetName")] string targetName = null, [CliArg("componentName", "Unique qualified Component type name.")] string componentName = null, [CliArg("fieldName", "Exact public collection member name.")] string fieldName = null, [CliArg("sourceTag", "Existing Unity tag selecting active ordinary loaded sources.")] string sourceTag = null, [CliArg("sourceName", "Ordinal case-sensitive name substring selecting active ordinary loaded sources.")] string sourceName = null, [CliArg("appendMode", "Preserve existing collection elements and deduplicate new additions.")] bool appendMode = false, [CliArg("target", "Exact Component or GameObject reference; Component references disambiguate multiple instances.")] string target = null)
        {
            const string schema = "unity.smart.reference-plan@1";
            try
            {
                SmartSelectionFacts.Authoring();
                var component = ResolveTarget(target, targetName, componentName); RequiredMember(fieldName);
                var field = FindField(component.GetType(), fieldName);
                if (field == null) throw new SmartFailure("SMART_SERIALIZED_MEMBER_UNSUPPORTED", "Select a public serialized collection field; opt-in properties use smart.reference-property.");
                if (field.IsStatic || field.IsInitOnly || field.IsLiteral) throw new SmartFailure("SMART_MEMBER_INVALID", "Select a writable public instance collection field.");
                var element = ElementType(field.FieldType);
                using (var serialized = new SerializedObject(component))
                {
                    var property = serialized.FindProperty(field.Name);
                    if (property == null || !property.isArray || property.propertyType == SerializedPropertyType.String || !property.arrayElementType.StartsWith("PPtr<", StringComparison.Ordinal))
                        throw new SmartFailure("SMART_SERIALIZED_MEMBER_UNSUPPORTED", "The field must expose a native serialized ObjectReference array/List property.");
                    var originals = new List<UnityObject>();
                    for (var i = 0; i < property.arraySize; i++)
                    {
                        var item = property.GetArrayElementAtIndex(i);
                        if (item.propertyType != SerializedPropertyType.ObjectReference) throw new SmartFailure("SMART_SERIALIZED_MEMBER_UNSUPPORTED", "Every native collection element must be an ObjectReference.");
                        originals.Add(item.objectReferenceValue);
                    }
                    var sources = Sources(sourceTag, sourceName); var proposed = Proposal(element, appendMode ? originals : new List<UnityObject>(), sources, out var skipped);
                    var references = proposed.Select(Exact.ExactId).ToArray();
                    var properties = new JObject { [property.propertyPath] = new JArray(references.Select(reference => reference == null ? JValue.CreateNull() : new JValue(reference))) };
                    return CommandResult<SmartReferencePlan>.Success(schema, new SmartReferencePlan
                    {
                        Component = Exact.ExactId(component), PropertyPath = property.propertyPath, OriginalReferences = originals.Select(Exact.ExactId).ToArray(), ProposedReferences = references,
                        PropertiesJson = properties.ToString(Formatting.None), ElementType = element.FullName, SourceCount = sources.Count, SkippedSources = skipped, AppendMode = appendMode, AssignmentOwner = "set_component_properties"
                    });
                }
            }
            catch (Exception failure) { return SmartSelectionFacts.Failure<SmartReferencePlan>(schema, failure); }
        }
        [CliCommand("smart.reference-property", "Smart reference-property operation.", Tags = new[] { "unity-cli-commands", "smart" })]
        public static CommandResult<SmartReferencePropertyReport> ReferenceProperty([CliArg("targetName", "targetName")] string targetName = null, [CliArg("componentName", "Unique qualified Component type name.")] string componentName = null, [CliArg("fieldName", "Exact public collection member name.")] string fieldName = null, [CliArg("sourceTag", "Existing Unity tag selecting active ordinary loaded sources.")] string sourceTag = null, [CliArg("sourceName", "Ordinal case-sensitive name substring selecting active ordinary loaded sources.")] string sourceName = null, [CliArg("appendMode", "Preserve existing collection elements and deduplicate new additions.")] bool appendMode = false, [CliArg("allowPropertyAccess", "Explicitly opt into execution of public getter/setter code.")] bool allowPropertyAccess = false, [CliArg("dryRun", "Preview only; wins over confirmation and preserves authored state.")] bool dryRun = true, [CliArg("confirm", "Commit the reviewed operation only when dryRun=false.")] bool confirm = false, [CliArg("target", "Exact Component or GameObject reference; Component references disambiguate multiple instances.")] string target = null)
        {
            const string schema = "unity.smart.reference-property@1";
            UnityComponent component = null; TargetSerializedSnapshot before = null; AuthoringUndoScope scope = null; var group = -1;
            var randomBefore = UnityEngine.Random.state; UnityObject[] selectionBefore = null;
            try
            {
                SmartSelectionFacts.Authoring(dryRun, confirm);
                selectionBefore = Selection.objects;
                if (!allowPropertyAccess) throw new SmartFailure("SMART_PROPERTY_ACCESS_REQUIRED", "Property access executes user code; explicitly set allowPropertyAccess=true to opt in.");
                component = ResolveTarget(target, targetName, componentName); RequiredMember(fieldName);
                var property = component.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                if (property == null || property.GetIndexParameters().Length != 0 || property.GetSetMethod(false) == null || property.GetSetMethod(false).IsStatic || (appendMode && (property.GetGetMethod(false) == null || property.GetGetMethod(false).IsStatic)))
                    throw new SmartFailure("SMART_MEMBER_INVALID", "Choose a public writable instance nonindexer collection property; append also requires a public getter.");
                var element = ElementType(property.PropertyType); var sources = Sources(sourceTag, sourceName);
                before = TargetSerializedSnapshot.Capture(component);
                if (!dryRun) { scope = new AuthoringUndoScope("Smart Reference Property"); group = Undo.GetCurrentGroup(); Undo.RecordObject(component, "Smart Reference Property"); }
                var originals = new List<UnityObject>();
                if (appendMode)
                {
                    object old;
                    try { old = property.GetValue(component, null); }
                    catch (Exception failure) { throw new SmartFailure("SMART_PROPERTY_GETTER_FAILED", "The opted-in public getter failed: " + (failure.InnerException ?? failure).Message); }
                    if (old is IEnumerable sequence) foreach (var item in sequence) originals.Add(item as UnityObject);
                }
                var proposed = Proposal(element, originals, sources, out _);
                var report = new SmartReferencePropertyReport
                {
                    Preview = dryRun, Component = Exact.ExactId(component), Property = property.Name, ProposedReferences = proposed.Select(Exact.ExactId).ToArray(), BoundCount = proposed.Count,
                    UndoScope = "TargetComponentSerializedState", PersistenceScope = "TargetComponentSerializedStateOnly", ExternalSetterEffectsRollbackGuaranteed = false, UndoGroup = group
                };
                if (dryRun)
                {
                    if (!before.Matches(component))
                    {
                        before.Restore(component);
                        throw new SmartFailure("SMART_PROPERTY_GETTER_MUTATED_TARGET", "The getter altered target serialized state during preview; target restoration was attempted.");
                    }
                    return CommandResult<SmartReferencePropertyReport>.Success(schema, report);
                }
                object assignment;
                if (property.PropertyType.IsArray)
                {
                    var array = Array.CreateInstance(element, proposed.Count); for (var i = 0; i < proposed.Count; i++) array.SetValue(proposed[i], i); assignment = array;
                }
                else
                {
                    var list = (IList)Activator.CreateInstance(property.PropertyType); foreach (var item in proposed) list.Add(item); assignment = list;
                }
                try { property.SetValue(component, assignment, null); }
                catch (Exception failure) { throw new SmartFailure("SMART_PROPERTY_SETTER_FAILED", "The opted-in public setter failed: " + (failure.InnerException ?? failure).Message); }
                report.SerializedStateChanged = !before.Matches(component);
                if (report.SerializedStateChanged) SmartSelectionFacts.Changed(component, component.gameObject);
                Undo.FlushUndoRecordObjects();
                return CommandResult<SmartReferencePropertyReport>.Success(schema, report);
            }
            catch (Exception failure)
            {
                if (before == null || component == null) return SmartSelectionFacts.Failure<SmartReferencePropertyReport>(schema, failure);
                Exception rollbackFailure = null;
                try
                {
                    if (scope != null) { Undo.FlushUndoRecordObjects(); Undo.RevertAllDownToGroup(group); scope.Cancel(); }
                    if (!before.Matches(component)) before.Restore(component);
                }
                catch (Exception rollback) { rollbackFailure = rollback; scope?.Cancel(); }
                var details = new Dictionary<string, object>
                {
                    ["targetSerializedRollback"] = rollbackFailure == null && component != null && before.Matches(component),
                    ["externalSetterEffectsRollbackGuaranteed"] = false, ["rollbackFailure"] = rollbackFailure?.Message,
                    ["targetDirtyBefore"] = before.TargetDirty, ["targetDirtyAfter"] = component != null && EditorUtility.IsDirty(component),
                    ["sceneDirtyBefore"] = before.SceneDirty, ["sceneDirtyAfter"] = component != null && component.gameObject.scene.isDirty,
                    ["persistenceScope"] = "TargetComponentSerializedStateOnly", ["message"] = failure.Message
                };
                return CommandResult<SmartReferencePropertyReport>.Failure(schema, failure is SmartFailure refusal ? refusal.Code : "SMART_PROPERTY_ACCESS_FAILED", "Public property access failed; inspect target rollback and external effects before further authoring.", details);
            }
            finally
            {
                scope?.Dispose();
                if (dryRun) { UnityEngine.Random.state = randomBefore; if (selectionBefore != null && !Selection.objects.SequenceEqual(selectionBefore)) Selection.objects = selectionBefore.Where(value => value != null).ToArray(); }
            }
        }

        private sealed class TargetSerializedSnapshot
        {
            private readonly string json;
            private readonly string runtimeJson;
            private readonly Dictionary<string, SerializedReference> references;
            internal readonly bool TargetDirty;
            internal readonly bool SceneDirty;

            private TargetSerializedSnapshot(UnityComponent component)
            {
                json = EditorJsonUtility.ToJson(component);
                runtimeJson = component is MonoBehaviour ? JsonUtility.ToJson(component) : null;
                references = ReadReferences(component);
                TargetDirty = EditorUtility.IsDirty(component);
                SceneDirty = component.gameObject.scene.isDirty;
            }

            internal static TargetSerializedSnapshot Capture(UnityComponent component) => new TargetSerializedSnapshot(component);

            internal bool Matches(UnityComponent component)
            {
                if (component == null || !string.Equals(EditorJsonUtility.ToJson(component), json, StringComparison.Ordinal)) return false;
                var current = ReadReferences(component);
                return current.Count == references.Count && references.All(entry =>
                    current.TryGetValue(entry.Key, out var value) && entry.Value.Matches(value));
            }

            internal void Restore(UnityComponent component)
            {
                EditorJsonUtility.FromJsonOverwrite(json, component);
                if (runtimeJson != null) JsonUtility.FromJsonOverwrite(runtimeJson, component);
                using (var serialized = new SerializedObject(component))
                {
                    serialized.Update();
                    foreach (var entry in references)
                    {
                        using (var property = serialized.FindProperty(entry.Key))
                        {
                            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                                throw new InvalidOperationException("Target reference property was not restored: " + entry.Key);
                            var saved = entry.Value;
                            if (saved.Kind == ReferenceKind.Live && saved.Value == null)
                                throw new InvalidOperationException("A referenced object was destroyed during property access: " + entry.Key);
                            if (!saved.Matches(SerializedReference.Read(property)))
                                property.objectReferenceEntityIdValue = saved.EntityId;
                        }
                    }
                    if (serialized.hasModifiedProperties) serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                if (!Matches(component)) throw new InvalidOperationException("Target serialized state differs after reference restoration.");
            }

            private static Dictionary<string, SerializedReference> ReadReferences(UnityComponent component)
            {
                var result = new Dictionary<string, SerializedReference>(StringComparer.Ordinal);
                using (var serialized = new SerializedObject(component))
                {
                    serialized.Update();
                    using (var property = serialized.GetIterator())
                    {
                        while (property.Next(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference)
                                result.Add(property.propertyPath, SerializedReference.Read(property));
                    }
                }
                return result;
            }

            internal enum ReferenceKind { Null, Live, Missing }
            private sealed class SerializedReference
            {
                internal readonly UnityObject Value;
                internal readonly EntityId EntityId;
                internal readonly ReferenceKind Kind;

                private SerializedReference(UnityObject value, EntityId entityId)
                {
                    Value = value; EntityId = entityId;
                    Kind = value != null ? ReferenceKind.Live : UnityEngine.EntityId.ToULong(entityId) == 0 ? ReferenceKind.Null : ReferenceKind.Missing;
                }

                internal static SerializedReference Read(SerializedProperty property)
                {
                    var value = property.objectReferenceValue;
                    var entityId = property.objectReferenceEntityIdValue;
                    return new SerializedReference(value, entityId);
                }

                internal bool Matches(SerializedReference current) => Kind == current.Kind && EntityId.Equals(current.EntityId) &&
                    (Kind != ReferenceKind.Live || Value != null && current.Value != null && Value == current.Value);
            }
        }

        private static void RequiredMember(string member)
        { if (string.IsNullOrEmpty(member)) throw new SmartFailure("SMART_MEMBER_INVALID", "Supply fieldName for the target collection."); }
        private static UnityComponent ResolveTarget(string exact, string name, string componentName)
        {
            Type type = null;
            if (!string.IsNullOrWhiteSpace(componentName))
            {
                try { type = AnalysisSceneObjects.ResolveType(componentName, typeof(UnityComponent)); }
                catch (Exception failure) { throw new SmartFailure("SMART_TYPE_INVALID", failure.Message); }
            }
            var eligible = SmartSelectionFacts.Ordinary();
            if (exact != null)
            {
                var resolved = Exact.Resolve<UnityObject>(exact);
                if (resolved is UnityComponent component)
                {
                    if (!eligible.Contains(component.gameObject) || EditorUtility.IsPersistent(component) || component.hideFlags != HideFlags.None) throw new SmartFailure("SMART_TARGET_INELIGIBLE", "Use an exact ordinary loaded scene Component.");
                    if (type != null && !type.IsInstanceOfType(component)) throw new SmartFailure("SMART_TYPE_INVALID", "componentName is incompatible with the exact Component target.");
                    return component;
                }
                if (!(resolved is GameObject go) || !eligible.Contains(go)) throw new SmartFailure("SMART_TARGET_INELIGIBLE", "Use an exact ordinary loaded scene GameObject or Component.");
                return UniqueComponent(go, type);
            }
            if (string.IsNullOrEmpty(name)) throw new SmartFailure("SMART_TARGET_INVALID", "Supply an exact target reference or a unique scene targetName.");
            var matches = eligible.Where(go => string.Equals(go.name, name, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1) throw new SmartFailure(matches.Length > 1 ? "SMART_TARGET_AMBIGUOUS" : "SMART_TARGET_INVALID", "targetName must identify exactly one ordinary loaded scene GameObject.");
            return UniqueComponent(matches[0], type);
        }
        private static UnityComponent UniqueComponent(GameObject go, Type type)
        {
            if (type == null) throw new SmartFailure("SMART_TYPE_INVALID", "Supply a unique qualified componentName when target is a GameObject.");
            var components = go.GetComponents(type).OfType<UnityComponent>().ToArray();
            if (components.Length != 1) throw new SmartFailure(components.Length > 1 ? "SMART_TARGET_AMBIGUOUS" : "SMART_TARGET_INVALID", "The GameObject must have exactly one matching Component; use an exact Component reference to disambiguate.");
            if (components[0].hideFlags != HideFlags.None) throw new SmartFailure("SMART_TARGET_INELIGIBLE", "Use an ordinary scene Component.");
            return components[0];
        }
        private static FieldInfo FindField(Type type, string name)
        {
            foreach (var candidate in new[] { name, "m_" + char.ToUpperInvariant(name[0]) + name.Substring(1), "_" + name })
            {
                var field = type.GetField(candidate, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                if (field != null) return field;
            }
            return null;
        }
        private static Type ElementType(Type collection)
        {
            Type element;
            if (collection.IsArray && collection.GetArrayRank() == 1) element = collection.GetElementType();
            else if (collection.IsGenericType && collection.GetGenericTypeDefinition() == typeof(List<>)) element = collection.GetGenericArguments()[0];
            else throw new SmartFailure("SMART_MEMBER_INVALID", "Choose a one-dimensional array or List of GameObject or Component.");
            if (element != typeof(GameObject) && !typeof(UnityComponent).IsAssignableFrom(element)) throw new SmartFailure("SMART_MEMBER_INVALID", "Collection elements must be GameObject or Component references.");
            return element;
        }
        private static List<GameObject> Sources(string tag, string name)
        {
            if (string.IsNullOrEmpty(tag) && string.IsNullOrEmpty(name)) throw new SmartFailure("SMART_SOURCE_INVALID", "Supply a sourceTag or case-sensitive sourceName selector.");
            if (!string.IsNullOrEmpty(tag) && !UnityEditorInternal.InternalEditorUtility.tags.Contains(tag)) throw new SmartFailure("SMART_SOURCE_INVALID", "sourceTag is undefined; choose an existing Unity tag.");
            var eligible = AnalysisSceneObjects.Loaded(false); var sources = new List<GameObject>();
            if (!string.IsNullOrEmpty(tag)) sources.AddRange(GameObject.FindGameObjectsWithTag(tag).Where(go => eligible.Contains(go)));
            if (!string.IsNullOrEmpty(name)) sources.AddRange(AnalysisSceneObjects.NativeGameObjects().Where(go => go.name.IndexOf(name, StringComparison.Ordinal) >= 0));
            sources = sources.Distinct().ToList();
            if (sources.Count == 0) throw new SmartFailure("SMART_SOURCE_INVALID", "No active ordinary loaded source objects match the selectors.");
            return sources;
        }
        private static List<UnityObject> Proposal(Type element, List<UnityObject> originals, List<GameObject> sources, out int skipped)
        {
            var proposed = new List<UnityObject>(originals); skipped = 0;
            foreach (var source in sources)
            {
                var converted = element == typeof(GameObject) ? (UnityObject)source : source.GetComponent(element);
                if (converted == null) { skipped++; continue; }
                if (!proposed.Contains(converted)) proposed.Add(converted);
            }
            return proposed;
        }
    }
}
