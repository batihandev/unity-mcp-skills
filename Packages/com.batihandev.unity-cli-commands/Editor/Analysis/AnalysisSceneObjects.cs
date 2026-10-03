using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class AnalysisSceneObjects
    {
        public static IReadOnlyList<GameObject> Loaded(bool includeInactive = true, bool visibleOnly = false)
        {
            var result = new List<GameObject>();
            foreach (var scene in LoadedScenes())
                foreach (var root in scene.GetRootGameObjects()) Visit(root, includeInactive, visibleOnly, result);
            return result;
        }
        public static IReadOnlyList<UnityEngine.Component> NativeComponents(Type type, bool includeInactive = false, FindObjectsSortMode sortMode = FindObjectsSortMode.None)
        {
            if (type == null || !typeof(UnityEngine.Component).IsAssignableFrom(type)) throw new ArgumentException("Supply a Component type.", nameof(type));
            var scenes = new HashSet<UnityEngine.SceneManagement.Scene>(LoadedScenes());
            return UnityEngine.Object.FindObjectsByType(type, includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, sortMode)
                .OfType<UnityEngine.Component>().Where(component => !EditorUtility.IsPersistent(component) && scenes.Contains(component.gameObject.scene) &&
                    (includeInactive || component.gameObject.activeInHierarchy)).ToArray();
        }
        public static IReadOnlyList<T> NativeComponents<T>(bool includeInactive = false, FindObjectsSortMode sortMode = FindObjectsSortMode.None) where T : UnityEngine.Component =>
            NativeComponents(typeof(T), includeInactive, sortMode).Cast<T>().ToArray();
        public static IReadOnlyList<GameObject> NativeGameObjects(bool includeInactive = false, FindObjectsSortMode sortMode = FindObjectsSortMode.None)
        {
            var scenes = new HashSet<UnityEngine.SceneManagement.Scene>(LoadedScenes());
            return UnityEngine.Object.FindObjectsByType<GameObject>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, sortMode)
                .Where(go => !EditorUtility.IsPersistent(go) && scenes.Contains(go.scene) && (includeInactive || go.activeInHierarchy)).ToArray();
        }
        private static IReadOnlyList<UnityEngine.SceneManagement.Scene> LoadedScenes()
        {
            var result = new List<UnityEngine.SceneManagement.Scene>();
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded && !EditorSceneManager.IsPreviewScene(scene) && (stage == null || stage.scene != scene)) result.Add(scene);
            }
            return result;
        }
        private static void Visit(GameObject value, bool inactive, bool visible, List<GameObject> result)
        {
            if (value == null || EditorUtility.IsPersistent(value)) return;
            if ((inactive || value.activeInHierarchy) && (!visible || value.hideFlags == HideFlags.None)) result.Add(value);
            for (var index = 0; index < value.transform.childCount; index++) Visit(value.transform.GetChild(index).gameObject, inactive, visible, result);
        }
        public static string Path(GameObject target)
        {
            if (target == null) throw new ArgumentException("Select an existing scene object.");
            var names = new List<string>();
            for (var transform = target.transform; transform != null; transform = transform.parent) names.Add(transform.name);
            names.Reverse(); return string.Join("/", names);
        }
        public static IReadOnlyList<Type> LoadedTypes()
        {
            var types = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                try { types.AddRange(assembly.GetTypes()); }
                catch (ReflectionTypeLoadException exception) { types.AddRange(exception.Types.Where(type => type != null)); }
            }
            return types.Distinct().ToArray();
        }
        public static Type ResolveType(string name, Type baseType = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Supply a unique or qualified type name.");
            var matches = LoadedTypes().Where(type => string.Equals(type.FullName, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type.AssemblyQualifiedName, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase)).Where(type => baseType == null || baseType.IsAssignableFrom(type)).ToArray();
            if (matches.Length != 1) throw new ArgumentException(matches.Length == 0 ? "The type was not found; supply a loaded qualified type." : "The type is ambiguous; supply its qualified name.");
            if (baseType != null && !baseType.IsAssignableFrom(matches[0])) throw new ArgumentException("The selected type is incompatible with this operation.");
            return matches[0];
        }
    }
}
