# GameObjectFinder Utilities

Paste-in helpers for scene GameObject lookup. `FindHelper.FindAll<T>()` is a
Unity-version-compatible wrapper over `FindObjectsByType`. `GameObjectFinder`
caches scene traversal per request and supports lookup by name, instance ID,
hierarchy path, tag, or component.

## Call surface

- `FindHelper.FindAll<T>(bool includeInactive = false)` — all objects of type T.
- `GameObjectFinder.Find(name, instanceId, path, tag, componentType)` — returns `GameObject` or `null`.
- `GameObjectFinder.FindByPath(string path)` — returns `GameObject` or `null`.
- `GameObjectFinder.FindByNameCaseInsensitive(string)` / `FindByNameContains(string)`.
- `GameObjectFinder.FindOrError(...)` — returns `(GameObject, object error)`.
- `GameObjectFinder.FindComponentOrError<T>(name, instanceId, path)` — returns `(T, object error)`.
- `GameObjectFinder.GetPath(GameObject)` / `GetCachedPath(GameObject)` — full hierarchy path string.
- `GameObjectFinder.GetDepth(GameObject)` — hierarchy depth (0 for roots).
- `GameObjectFinder.GetSceneObjects()` / `InvalidateCache()`.

## Do not

- Do not nest cache helper classes as `private` inside a static class — the
  `Unity_RunCommand` code transformer duplicates them to namespace scope and
  compile fails (CS1527). Keep `_GameObjectFinderCache` top-level `internal`.
- Do not use `BindingFlags.Public | BindingFlags.Instance` in code pasted with
  this shim — same transformer fault.

## Paste-in

```csharp
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

    internal sealed class _GameObjectFinderCache
    {
        public readonly System.Collections.Generic.List<UnityEngine.GameObject> Objects = new System.Collections.Generic.List<UnityEngine.GameObject>();
        public readonly System.Collections.Generic.Dictionary<int, string> PathsByInstanceId = new System.Collections.Generic.Dictionary<int, string>();
        public readonly System.Collections.Generic.Dictionary<int, int> DepthsByInstanceId = new System.Collections.Generic.Dictionary<int, int>();
        public readonly System.Collections.Generic.Dictionary<string, UnityEngine.GameObject> PathLookup =
            new System.Collections.Generic.Dictionary<string, UnityEngine.GameObject>(System.StringComparer.OrdinalIgnoreCase);
    }

    public static class GameObjectFinder
    {
        private static _GameObjectFinderCache _cachedSceneData;
        private static bool _cacheValid = false;

        public static void InvalidateCache()
        {
            _cachedSceneData = null;
            _cacheValid = false;
        }

        private static _GameObjectFinderCache GetOrBuildSceneCache()
        {
            if (_cachedSceneData != null && _cacheValid)
                return _cachedSceneData;

            var cache = new _GameObjectFinderCache();
            var roots = GetLoadedSceneRoots();
            var stack = new System.Collections.Generic.Stack<(UnityEngine.Transform transform, string path, string sceneName, int depth)>();
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

                foreach (UnityEngine.Transform child in transform)
                    stack.Push((child, path + "/" + child.name, sceneName, depth + 1));
            }

            _cachedSceneData = cache;
            _cacheValid = true;
            return cache;
        }

        private static System.Collections.Generic.IEnumerable<UnityEngine.GameObject> GetAllSceneObjects() => GetOrBuildSceneCache().Objects;
        public static System.Collections.Generic.IReadOnlyList<UnityEngine.GameObject> GetSceneObjects() => GetOrBuildSceneCache().Objects;

        public static int GetDepth(UnityEngine.GameObject go)
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

        private static void AddPathLookup(System.Collections.Generic.Dictionary<string, UnityEngine.GameObject> lookup, string path, UnityEngine.GameObject go)
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

        private static System.Collections.Generic.IEnumerable<UnityEngine.GameObject> GetLoadedSceneRoots()
        {
            for (int sceneIndex = 0; sceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCount; sceneIndex++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                    yield return root;
            }
        }

        public static UnityEngine.GameObject Find(string name = null, int instanceId = 0, string path = null, string tag = null, string componentType = null)
        {
            if (instanceId != 0)
            {
                var obj = UnityEditor.EditorUtility.InstanceIDToObject(instanceId);
                if (obj is UnityEngine.GameObject idGo) return idGo;
            }

            if (!string.IsNullOrEmpty(path))
            {
                var pGo = FindByPath(path);
                if (pGo != null) return pGo;
            }

            if (!string.IsNullOrEmpty(name))
            {
                var nGo = FindByNameCaseInsensitive(name);
                if (nGo != null) return nGo;

                nGo = FindByNameContains(name);
                if (nGo != null) return nGo;
            }

            if (!string.IsNullOrEmpty(tag))
            {
                var tGo = System.Linq.Enumerable.FirstOrDefault(GetAllSceneObjects(), candidate =>
                {
                    try { return candidate.CompareTag(tag); }
                    catch { return false; }
                });
                if (tGo != null) return tGo;
            }

            return null;
        }

        public static UnityEngine.GameObject FindByPath(string path)
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

        private static UnityEngine.GameObject FindDirectChild(UnityEngine.GameObject parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName)) return null;

            var exact = parent.transform.Find(childName);
            if (exact != null) return exact.gameObject;

            foreach (UnityEngine.Transform child in parent.transform)
            {
                if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
                    return child.gameObject;
            }
            return null;
        }

        public static UnityEngine.GameObject FindByNameCaseInsensitive(string name)
        {
            return System.Linq.Enumerable.FirstOrDefault(GetAllSceneObjects(), go =>
                go.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        }

        public static UnityEngine.GameObject FindByNameContains(string name)
        {
            var exactWord = System.Linq.Enumerable.FirstOrDefault(GetAllSceneObjects(), go =>
                System.Linq.Enumerable.Any(go.name.Split(' ', '_', '-'),
                    word => word.Equals(name, System.StringComparison.OrdinalIgnoreCase)));
            if (exactWord != null) return exactWord;

            return System.Linq.Enumerable.FirstOrDefault(GetAllSceneObjects(), go =>
                go.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static string GetPath(UnityEngine.GameObject go)
        {
            if (go == null) return null;

            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        public static string GetCachedPath(UnityEngine.GameObject go)
        {
            if (go == null) return null;

            var instanceId = go.GetInstanceID();
            var cache = GetOrBuildSceneCache();
            if (cache.PathsByInstanceId.TryGetValue(instanceId, out var cachedPath))
                return cachedPath;

            var path = GetPath(go);
            cache.PathsByInstanceId[instanceId] = path;
            return path;
        }

        public static (UnityEngine.GameObject go, object error) FindOrError(
            string name = null, int instanceId = 0, string path = null, string tag = null, string componentType = null)
        {
            var go = Find(name, instanceId, path, tag, componentType);
            if (go == null)
            {
                var identifier = instanceId != 0 ? $"instanceId {instanceId}" :
                    !string.IsNullOrEmpty(path) ? $"path '{path}'" :
                    !string.IsNullOrEmpty(tag) ? $"tag '{tag}'" :
                    !string.IsNullOrEmpty(componentType) ? $"component '{componentType}'" :
                    $"name '{name}'";

                var suggestions = GetSuggestions(name);

                return (null, new
                {
                    error = $"GameObject not found: {identifier}",
                    suggestions = suggestions.Length > 0 ? suggestions : null
                });
            }
            return (go, null);
        }

        public static (T component, object error) FindComponentOrError<T>(
            string name = null, int instanceId = 0, string path = null) where T : UnityEngine.Component
        {
            var (go, err) = FindOrError(name, instanceId, path);
            if (err != null) return (null, err);
            var comp = go.GetComponent<T>();
            if (comp == null) return (null, new { error = $"No {typeof(T).Name} component on {go.name}" });
            return (comp, null);
        }

        private static string[] GetSuggestions(string name)
        {
            if (string.IsNullOrEmpty(name)) return System.Array.Empty<string>();

            var prefix = name.Substring(0, System.Math.Min(3, name.Length));
            return System.Linq.Enumerable.ToArray(
                System.Linq.Enumerable.Select(
                    System.Linq.Enumerable.Take(
                        System.Linq.Enumerable.Where(
                            GetAllSceneObjects(),
                            go => go.name.IndexOf(prefix, System.StringComparison.OrdinalIgnoreCase) >= 0),
                        5),
                    go => $"'{go.name}' (path: {GetPath(go)})"));
        }
    }
```
