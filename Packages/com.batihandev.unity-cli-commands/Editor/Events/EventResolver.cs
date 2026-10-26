using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BatihanDev.UnityCliCommands.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityObject = UnityEngine.Object;
using UnityComponent = UnityEngine.Component;

namespace BatihanDev.UnityCliCommands.Events
{
    internal sealed class ResolvedEvent
    {
        internal UnityComponent Component;
        internal string Name;
        internal Type MemberType;
        internal UnityEventBase Value;
        internal string SerializedPath;
    }

    internal static class EventResolver
    {
        private const BindingFlags PublicHere = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        private const BindingFlags AllFieldsHere = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        internal static bool TryComponent(string reference, out UnityComponent component, out string error)
        {
            component = ExactObjectReference.Resolve<UnityComponent>(reference);
            if (!IsRegularSceneObject(component))
            {
                component = null;
                error = "Target must identify one Component in a regular loaded scene.";
                return false;
            }
            error = null;
            return true;
        }

        internal static bool TryListener(string reference, out UnityObject listener, out string error)
        {
            listener = ExactObjectReference.Resolve<UnityObject>(reference);
            if (!(listener is UnityComponent) && !(listener is GameObject) || !IsRegularSceneObject(listener))
            {
                listener = null;
                error = "Listener target must identify one Component or GameObject in a regular loaded scene.";
                return false;
            }
            error = null;
            return true;
        }

        private static bool IsRegularSceneObject(UnityObject value)
        {
            if (value == null || EditorUtility.IsPersistent(value)) return false;
            var go = value as GameObject ?? (value as UnityComponent)?.gameObject;
            if (go == null) return false;
            var scene = go.scene;
            return scene.IsValid() && scene.isLoaded && !EditorSceneManager.IsPreviewScene(scene);
        }

        internal static List<ResolvedEvent> List(UnityComponent component)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var found = new List<ResolvedEvent>();
            for (var type = component.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (var field in type.GetFields(PublicHere).OrderBy(member => member.MetadataToken))
                {
                    if (!seen.Add(field.Name) || !typeof(UnityEventBase).IsAssignableFrom(field.FieldType)) continue;
                    try
                    {
                        var value = field.GetValue(component) as UnityEventBase;
                        found.Add(new ResolvedEvent { Component = component, Name = field.Name, MemberType = field.FieldType, Value = value });
                    }
                    catch (Exception) { }
                }
                foreach (var property in type.GetProperties(PublicHere).OrderBy(member => member.MetadataToken))
                {
                    if (!seen.Add(property.Name) || property.GetIndexParameters().Length != 0 ||
                        property.GetGetMethod() == null || !typeof(UnityEventBase).IsAssignableFrom(property.PropertyType)) continue;
                    try
                    {
                        var value = property.GetValue(component, null) as UnityEventBase;
                        found.Add(new ResolvedEvent { Component = component, Name = property.Name, MemberType = property.PropertyType, Value = value });
                    }
                    catch (Exception) { }
                }
            }
            return found;
        }

        internal static bool TryEvent(UnityComponent component, string name, bool mutation, out ResolvedEvent resolved, out string error)
        {
            resolved = null;
            if (string.IsNullOrWhiteSpace(name)) { error = "An exact eventName is required."; return false; }
            var candidates = List(component).Where(item => item.Name == name).ToArray();
            if (candidates.Length != 1) { error = "A public, readable UnityEventBase member with that exact name was not found."; return false; }
            resolved = candidates[0];
            if (resolved.Value == null) { error = "The selected event member is null."; resolved = null; return false; }
            if (mutation && !TryBackingField(resolved, out error)) return false;
            error = null;
            return true;
        }

        private static bool TryBackingField(ResolvedEvent resolved, out string error)
        {
            var matches = new List<FieldInfo>();
            for (var type = resolved.Component.GetType(); type != null && type != typeof(object); type = type.BaseType)
                foreach (var field in type.GetFields(AllFieldsHere))
                {
                    if (field.IsStatic || field.IsInitOnly || field.IsNotSerialized || !typeof(UnityEventBase).IsAssignableFrom(field.FieldType)) continue;
                    if (!field.IsPublic && !Attribute.IsDefined(field, typeof(SerializeField))) continue;
                    if (ReferenceEquals(field.GetValue(resolved.Component), resolved.Value)) matches.Add(field);
                }
            if (matches.Count != 1)
            {
                error = "The event does not have one unique serialized backing field.";
                return false;
            }
            var serialized = new SerializedObject(resolved.Component);
            var fieldPath = matches[0].Name;
            if (serialized.FindProperty(fieldPath) == null)
            {
                error = "The event backing field is not available in Unity serialization.";
                return false;
            }
            resolved.SerializedPath = fieldPath;
            error = null;
            return true;
        }

        internal static bool TryState(string name, out UnityEventCallState state)
        {
            foreach (var value in new[] { UnityEventCallState.Off, UnityEventCallState.RuntimeOnly, UnityEventCallState.EditorAndRuntime })
                if (string.Equals(name, value.ToString(), StringComparison.OrdinalIgnoreCase)) { state = value; return true; }
            state = default;
            return false;
        }

        internal static bool TryMethod(UnityObject target, string name, Type argument, out MethodInfo method, out string error)
        {
            method = null;
            if (string.IsNullOrWhiteSpace(name)) { error = "A methodName is required."; return false; }
            var types = argument == null ? Type.EmptyTypes : new[] { argument };
            var candidates = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(item => item.Name == name && !item.IsStatic && !item.ContainsGenericParameters &&
                    item.ReturnType == typeof(void) && item.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(types))
                .ToArray();
            if (candidates.Length != 1)
            {
                error = "No unique public instance void method matches the exact name and parameter signature.";
                return false;
            }
            method = candidates[0];
            error = null;
            return true;
        }

        internal static void Dirty(UnityComponent component)
        {
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
        }

        internal static int PersistentMode(ResolvedEvent resolved, int index)
        {
            var serialized = new SerializedObject(resolved.Component);
            serialized.Update();
            var property = serialized.FindProperty(resolved.SerializedPath + ".m_PersistentCalls.m_Calls.Array.data[" + index + "].m_Mode");
            return property == null ? -1 : property.intValue;
        }
    }
}
