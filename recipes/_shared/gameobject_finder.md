# GameObjectFinder Utilities

Use these C# utility classes inside your `IRunCommand` scripts when performing complex targeted scene searches. 
This bypasses slow iteration via `FindObjectsOfType` by enforcing structural caching.

To use, just paste these static classes adjacent to your `CommandScript` inside the execution block.

```csharp
    /// <summary>
    /// Compatibility helper for FindObjectsByType (Unity 6+) / FindObjectsOfType fallback.
    /// </summary>
    internal static class FindHelper
    {
        internal static T[] FindAll<T>(bool includeInactive = false) where T : UnityEngine.Object
        {
#if UNITY_6000_0_OR_NEWER
            return includeInactive
                ? UnityEngine.Object.FindObjectsByType<T>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)
                : UnityEngine.Object.FindObjectsByType<T>(UnityEngine.FindObjectsSortMode.None);
#else
            return includeInactive
                ? UnityEngine.Resources.FindObjectsOfTypeAll<T>()
                : UnityEngine.Object.FindObjectsOfType<T>();
#endif
        }
    }

    /// <summary>
    /// Unified utility for finding GameObjects by multiple methods.
    /// Supports: name, instance ID, hierarchy path, tag, component type.
    /// Enhanced with intelligent fallback search strategies.
    /// </summary>
    public static class GameObjectFinder
    {
        private sealed class SceneObjectCache
        {
            public readonly System.Collections.Generic.List<GameObject> Objects = new System.Collections.Generic.List<GameObject>();
            public readonly System.Collections.Generic.Dictionary<int, string> PathsByInstanceId = new System.Collections.Generic.Dictionary<int, string>();
            public readonly System.Collections.Generic.Dictionary<int, int> DepthsByInstanceId = new System.Collections.Generic.Dictionary<int, int>();
            public readonly System.Collections.Generic.Dictionary<string, GameObject> PathLookup =
                new System.Collections.Generic.Dictionary<string, GameObject>(System.StringComparer.OrdinalIgnoreCase);
        }

        // Request-level cache for scene traversal metadata - invalidated after each request via InvalidateCache()
        private static SceneObjectCache _cachedSceneData;
        private static bool _cacheValid = false;

        public static void InvalidateCache()
        {
            _cachedSceneData = null;
            _cacheValid = false;
        }

        private static SceneObjectCache GetOrBuildSceneCache()
        {
            if (_cachedSceneData != null && _cacheValid)
                return _cachedSceneData;

            var cache = new SceneObjectCache();
            var roots = GetLoadedSceneRoots();
            var stack = new System.Collections.Generic.Stack<(Transform transform, string path, string sceneName, int depth)>();
            foreach (var root in roots)
                stack.Push((root.transform, root.name, root.scene.name, 0));

            while (stack.Count > 0)
            {
                var (transform, path, sceneName, depth) = stack.Pop();
                var gameObject = transform.gameObject;
                var instanceId = gameObject.GetInstanceID();

                cache.Objects.Add(gameObject);
                cache.PathsByInstanceId[instanceId] = path;
                cache.DepthsByInstanceId[instanceId] = depth;
                AddPathLookup(cache.PathLookup, path, gameObject);

                if (!string.IsNullOrEmpty(sceneName))
                    AddPathLookup(cache.PathLookup, sceneName + "/" + path, gameObject);

                foreach (Transform child in transform)
                    stack.Push((child, path + "/" + child.name, sceneName, depth + 1));
            }

            _cachedSceneData = cache;
            _cacheValid = true;
            return cache;
        }

        private static System.Collections.Generic.IEnumerable<GameObject> GetAllSceneObjects() => GetOrBuildSceneCache().Objects;
        public static System.Collections.Generic.IReadOnlyList<GameObject> GetSceneObjects() => GetOrBuildSceneCache().Objects;

        public static int GetDepth(GameObject go)
        {
            if (go == null) return 0;

            var instanceId = go.GetInstanceID();
            if (_cachedSceneData != null && _cacheValid &&
                _cachedSceneData.DepthsByInstanceId.TryGetValue(instanceId, out var depth))
                return depth;

            depth = 0;
            var parent = go.transform.parent;
            while (parent != null)
            {
                depth++;
                parent = parent.parent;
            }

            if (_cachedSceneData != null && _cacheValid)
                _cachedSceneData.DepthsByInstanceId[instanceId] = depth;

            return depth;
        }

        private static void AddPathLookup(System.Collections.Generic.Dictionary<string, GameObject> lookup, string path, GameObject go)
        {
            if (string.IsNullOrEmpty(path) || lookup.ContainsKey(path)) return;
            lookup[path] = go;
        }

        private static string NormalizePathKey(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            var parts = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(
                path.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries),
                part => !string.IsNullOrWhiteSpace(part)));

            return parts.Length == 0 ? null : string.Join("/", parts);
        }

        private static System.Collections.Generic.IEnumerable<GameObject> GetLoadedSceneRoots()
        {
            for (int sceneIndex = 0; sceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCount; sceneIndex++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                    yield return root;
            }
        }

        public static GameObject Find(string name = null, int instanceId = 0, string path = null, string tag = null, string componentType = null)
        {
            // Priority 1: Instance ID
            if (instanceId != 0)
            {
                var obj = UnityEditor.EditorUtility.InstanceIDToObject(instanceId);
                if (obj is GameObject idGo) return idGo;
            }

            // Priority 2: Hierarchy path
            if (!string.IsNullOrEmpty(path))
            {
                var pGo = FindByPath(path);
                if (pGo != null) return pGo;
            }

            // Priority 3: Simple name search
            if (!string.IsNullOrEmpty(name))
            {
                var nGo = FindByNameCaseInsensitive(name);
                if (nGo != null) return nGo;

                nGo = FindByNameContains(name);
                if (nGo != null) return nGo;
            }

            // Priority 4: Tag search
            if (!string.IsNullOrEmpty(tag))
            {
                var tGo = System.Linq.Enumerable.FirstOrDefault(GetAllSceneObjects(), candidate =>
                {
                    try { return candidate.CompareTag(tag); }
                    catch { return false; }
                });
                if (tGo != null) return tGo;
            }

            // Component Type omitted for generic raw copy since it relies on ComponentSkills 

            return null;
        }

        public static GameObject FindByPath(string path)
        {
            var normalizedPath = NormalizePathKey(path);
            if (string.IsNullOrEmpty(normalizedPath)) return null;

            var cache = GetOrBuildSceneCache();
            if (cache.PathLookup.TryGetValue(normalizedPath, out var cachedGo)) return cachedGo;

            var parts = normalizedPath.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;

            foreach (var scene in System.Linq.Enumerable.Where(
                System.Linq.Enumerable.Select(
                    System.Linq.Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount), 
                    UnityEngine.SceneManagement.SceneManager.GetSceneAt), 
                scene => scene.IsValid() && scene.isLoaded))
            {
                var rootObjects = scene.GetRootGameObjects();
                int partIndex = 0;

                if (parts.Length > 1 && scene.name.Equals(parts[0], System.StringComparison.OrdinalIgnoreCase))
                    partIndex = 1;

                if (partIndex >= parts.Length) continue;

                var current = System.Linq.Enumerable.FirstOrDefault(rootObjects, go =>
                    go.name.Equals(parts[partIndex], System.StringComparison.OrdinalIgnoreCase));
                if (current == null) continue;

                partIndex++;
                while (partIndex < parts.Length && current != null)
                {
                    current = FindDirectChild(current, parts[partIndex]);
                    partIndex++;
                }

                if (current != null) return current;
            }
            return null;
        }

        private static GameObject FindDirectChild(GameObject parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName)) return null;

            var exact = parent.transform.Find(childName);
            if (exact != null) return exact.gameObject;

            foreach (Transform child in parent.transform)
            {
                if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
                    return child.gameObject;
            }
            return null;
        }

        public static GameObject FindByNameCaseInsensitive(string name)
        {
            return System.Linq.Enumerable.FirstOrDefault(GetAllSceneObjects(), go => 
                go.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        }

        public static GameObject FindByNameContains(string name)
        {
            var exactWord = System.Linq.Enumerable.FirstOrDefault(GetAllSceneObjects(), go => 
                System.Linq.Enumerable.Any(go.name.Split(' ', '_', '-'), 
                    word => word.Equals(name, System.StringComparison.OrdinalIgnoreCase)));
            if (exactWord != null) return exactWord;

            return System.Linq.Enumerable.FirstOrDefault(GetAllSceneObjects(), go => 
                go.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
```
