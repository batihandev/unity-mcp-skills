# _shared/perception_helpers

Scene-metrics snapshot + package / input / UI / profile classifiers + reflection helpers. Used by `perception/*` diagnostic recipes (`project_stack_detect`, `scene_analyze`, `scene_component_stats`, `scene_contract_validate`, `scene_find_hotspots`, `scene_health_check`, `scene_diff`).

## When to declare

Recipe references `PerceptionHelpers.*` or `_SceneMetricsSnapshot`.

Transitive dependency: requires `recipes/_shared/gameobject_finder.md` (for `GameObjectFinder.GetSceneObjects()` and `GameObjectFinder.GetDepth(go)`).

## Paste-in

```csharp
internal sealed class _SceneHotspot
{
    public string Type;
    public string Severity;
    public string Name;
    public string Path;
    public int Count;
    public int Depth;
    public string Message;
}

internal sealed class _SceneMetricsSnapshot
{
    public UnityEngine.SceneManagement.Scene Scene;
    public System.Collections.Generic.IReadOnlyList<UnityEngine.GameObject> Objects;
    public System.Collections.Generic.Dictionary<string, int> ComponentCounts = new System.Collections.Generic.Dictionary<string, int>();
    public int TotalObjects;
    public int ActiveObjects;
    public int DisabledObjects;
    public int RootObjects;
    public int MaxHierarchyDepth;
    public int Cameras;
    public int MainCameraCount;
    public int Lights;
    public int Canvases;
    public int EventSystems;
    public int AudioListeners;
    public int PrefabInstances;
    public bool HasUiGraphic;
    public bool HasUiToolkitDocument;
    public int EmptyLeafCount;
}

internal static class PerceptionHelpers
{
    public static _SceneMetricsSnapshot CollectSceneMetrics(bool includeComponentStats = true)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var allObjects = GameObjectFinder.GetSceneObjects();
        var snap = new _SceneMetricsSnapshot
        {
            Scene = scene,
            Objects = allObjects,
            TotalObjects = allObjects.Count,
            RootObjects = scene.rootCount,
        };
        var buf = new System.Collections.Generic.List<UnityEngine.Component>(8);
        var uiDocType = FindTypeInAssemblies("UnityEngine.UIElements.UIDocument");

        foreach (var go in allObjects)
        {
            if (go.activeInHierarchy) snap.ActiveObjects++; else snap.DisabledObjects++;
            var depth = GameObjectFinder.GetDepth(go);
            if (depth > snap.MaxHierarchyDepth) snap.MaxHierarchyDepth = depth;
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(go) && !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(go))
                snap.PrefabInstances++;

            buf.Clear();
            go.GetComponents(buf);
            if (buf.Count == 1 && go.transform.childCount == 0) snap.EmptyLeafCount++;

            foreach (var c in buf)
            {
                if (c == null) continue;
                var name = c.GetType().Name;
                if (includeComponentStats)
                    snap.ComponentCounts[name] = snap.ComponentCounts.TryGetValue(name, out var n) ? n + 1 : 1;
                if (c is UnityEngine.Camera) { snap.Cameras++; if (go.CompareTag("MainCamera")) snap.MainCameraCount++; }
                else if (c is UnityEngine.Light) snap.Lights++;
                else if (c is UnityEngine.Canvas) snap.Canvases++;
                else if (c is UnityEngine.EventSystems.EventSystem) snap.EventSystems++;
                else if (c is UnityEngine.AudioListener) snap.AudioListeners++;
                else if (c is UnityEngine.UI.Graphic) snap.HasUiGraphic = true;
            }
            if (uiDocType != null && go.GetComponent(uiDocType) != null) snap.HasUiToolkitDocument = true;
        }
        return snap;
    }

    public static System.Type FindTypeInAssemblies(string fullName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type t = null;
            try { t = asm.GetType(fullName, false); } catch { }
            if (t != null) return t;
        }
        return null;
    }

    // Returns List<string> (no HashSet<string>(StringComparer) — ISet<> assembly-ref gotcha).
    // Use ContainsIgnoreCase below for membership checks.
    public static System.Collections.Generic.List<string> ReadInstalledPackageIds()
    {
        var ids = new System.Collections.Generic.List<string>();
        var manifestPath = System.IO.Path.Combine("Packages", "manifest.json");
        if (!System.IO.File.Exists(manifestPath)) return ids;
        try
        {
            var json = System.IO.File.ReadAllText(manifestPath, System.Text.Encoding.UTF8);
            int i = json.IndexOf("\"dependencies\"", System.StringComparison.Ordinal);
            if (i < 0) return ids;
            int lb = json.IndexOf('{', i);
            if (lb < 0) return ids;
            int depth = 1; int j = lb + 1; int rb = -1;
            while (j < json.Length && depth > 0)
            {
                char c = json[j];
                if (c == '"') { j++; while (j < json.Length) { if (json[j] == '\\' && j + 1 < json.Length) { j += 2; continue; } if (json[j] == '"') break; j++; } }
                else if (c == '{') depth++;
                else if (c == '}') { depth--; if (depth == 0) { rb = j; break; } }
                j++;
            }
            if (rb < 0) return ids;
            var slice = json.Substring(lb + 1, rb - lb - 1);
            int k = 0;
            while (k < slice.Length)
            {
                while (k < slice.Length && slice[k] != '"') k++;
                if (k >= slice.Length) break;
                int ks = k + 1; int ke = ks;
                while (ke < slice.Length) { if (slice[ke] == '\\' && ke + 1 < slice.Length) { ke += 2; continue; } if (slice[ke] == '"') break; ke++; }
                ids.Add(slice.Substring(ks, ke - ks));
                k = ke + 1;
                while (k < slice.Length && slice[k] != ',' && slice[k] != '}') k++;
                if (k < slice.Length && slice[k] == ',') k++;
            }
        }
        catch { }
        return ids;
    }

    public static bool ContainsIgnoreCase(System.Collections.Generic.List<string> list, string val)
    {
        for (int k = 0; k < list.Count; k++)
            if (string.Equals(list[k], val, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public static string DetectInputHandling(System.Collections.Generic.List<string> packageIds)
    {
        var prop = typeof(UnityEditor.PlayerSettings).GetProperty("activeInputHandler");
        if (prop == null) prop = typeof(UnityEditor.PlayerSettings).GetProperty("activeInputHandling");
        if (prop != null)
        {
            try
            {
                var v = prop.GetValue(null);
                if (v != null) return v.ToString();
            }
            catch { }
        }
        return ContainsIgnoreCase(packageIds, "com.unity.inputsystem") ? "InputSystemPackageInstalled" : "LegacyInputManager";
    }

    public static string DetermineUiRoute(_SceneMetricsSnapshot metrics, bool hasUiToolkitAssets)
    {
        var usesUgui = metrics.Canvases > 0 || metrics.HasUiGraphic;
        var usesUiToolkit = metrics.HasUiToolkitDocument || hasUiToolkitAssets;
        if (usesUgui && usesUiToolkit) return "Both";
        if (usesUiToolkit) return "UIToolkit";
        if (usesUgui) return "UGUI";
        return "Unknown";
    }

    public static string DetermineProjectProfile(_SceneMetricsSnapshot metrics, bool xrDetected, string uiRoute)
    {
        if (xrDetected) return "XR";
        var sprites = metrics.ComponentCounts.TryGetValue("SpriteRenderer", out var s) ? s : 0;
        var meshes = metrics.ComponentCounts.TryGetValue("MeshRenderer", out var m) ? m : 0;
        if (uiRoute != "Unknown" && metrics.Canvases >= System.Math.Max(1, metrics.Cameras)) return "UI";
        if (sprites > meshes && sprites > 0) return "2D";
        return "3D";
    }

    public static object[] BuildTopComponents(_SceneMetricsSnapshot snapshot, int topComponentsLimit)
    {
        return System.Linq.Enumerable.ToArray(
            System.Linq.Enumerable.Select(
                System.Linq.Enumerable.Take(
                    System.Linq.Enumerable.OrderByDescending(
                        System.Linq.Enumerable.Where(snapshot.ComponentCounts, kv => kv.Key != "Transform"),
                        kv => kv.Value),
                    topComponentsLimit),
                kv => (object)new { component = kv.Key, count = kv.Value }));
    }

    public static int GetSeverityRank(string severity)
    {
        switch (severity) { case "Error": return 0; case "Warning": return 1; default: return 2; }
    }

    public static System.Collections.Generic.List<_SceneHotspot> CollectHotspots(
        System.Collections.Generic.IReadOnlyList<UnityEngine.GameObject> allObjects,
        int deepHierarchyThreshold,
        int largeChildCountThreshold,
        int maxResults)
    {
        var hotspots = new System.Collections.Generic.List<_SceneHotspot>();

        foreach (var go in allObjects)
        {
            if (go == null) continue;
            var depth = GameObjectFinder.GetDepth(go);
            if (depth >= deepHierarchyThreshold)
            {
                hotspots.Add(new _SceneHotspot
                {
                    Type = "DeepHierarchy",
                    Severity = depth >= deepHierarchyThreshold + 3 ? "Warning" : "Info",
                    Name = go.name,
                    Path = GameObjectFinder.GetCachedPath(go),
                    Depth = depth,
                    Count = depth,
                    Message = "Hierarchy depth " + depth + " exceeds threshold " + deepHierarchyThreshold + "."
                });
            }
            if (go.transform.childCount >= largeChildCountThreshold)
            {
                hotspots.Add(new _SceneHotspot
                {
                    Type = "LargeChildSet",
                    Severity = go.transform.childCount >= largeChildCountThreshold * 2 ? "Warning" : "Info",
                    Name = go.name,
                    Path = GameObjectFinder.GetCachedPath(go),
                    Count = go.transform.childCount,
                    Message = go.transform.childCount + " direct children under one node."
                });
            }
        }

        // Duplicate-name clusters — use List<string>/Dictionary, not HashSet<string>(StringComparer) (ISet<> gotcha).
        var nameGroups = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var go in allObjects)
        {
            if (go == null) continue;
            nameGroups[go.name] = nameGroups.TryGetValue(go.name, out var n) ? n + 1 : 1;
        }
        foreach (var kv in nameGroups)
        {
            if (kv.Value <= 1) continue;
            hotspots.Add(new _SceneHotspot
            {
                Type = "DuplicateNameCluster",
                Severity = kv.Value >= 5 ? "Warning" : "Info",
                Name = kv.Key,
                Count = kv.Value,
                Message = kv.Value + " objects share the name '" + kv.Key + "'."
            });
        }

        // Empty leaf clusters grouped by parent path.
        var emptyLeafParents = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var go in allObjects)
        {
            if (go == null || go.transform.childCount != 0) continue;
            if (go.GetComponents<UnityEngine.Component>().Length != 1) continue;
            var parentPath = go.transform.parent != null ? GameObjectFinder.GetCachedPath(go.transform.parent.gameObject) : "<root>";
            emptyLeafParents[parentPath] = emptyLeafParents.TryGetValue(parentPath, out var n) ? n + 1 : 1;
        }
        foreach (var kv in emptyLeafParents)
        {
            if (kv.Value < 3) continue;
            hotspots.Add(new _SceneHotspot
            {
                Type = "EmptyLeafCluster",
                Severity = "Info",
                Path = kv.Key,
                Count = kv.Value,
                Message = kv.Value + " empty leaf objects are grouped under '" + kv.Key + "'."
            });
        }

        hotspots.Sort((a, b) =>
        {
            int rankA = GetSeverityRank(a.Severity);
            int rankB = GetSeverityRank(b.Severity);
            if (rankA != rankB) return rankA - rankB;
            if (a.Count != b.Count) return b.Count - a.Count;
            return b.Depth - a.Depth;
        });
        if (hotspots.Count > maxResults) hotspots.RemoveRange(maxResults, hotspots.Count - maxResults);
        return hotspots;
    }

    public static System.Collections.Generic.IEnumerable<object> GetEnumerableProperty(object target, string name)
    {
        var value = GetPropertyValueObject(target, name);
        if (value is System.Collections.IEnumerable en && !(value is string))
            return System.Linq.Enumerable.Cast<object>(en);
        return System.Array.Empty<object>();
    }

    private static object GetPropertyValueObject(object target, string name)
    {
        if (target == null) return null;
        if (target is System.Collections.Generic.IDictionary<string, object> dict)
            return dict.TryGetValue(name, out var v) ? v : null;
        if (target is System.Collections.IDictionary legacy)
            return legacy.Contains(name) ? legacy[name] : null;
        var t = target.GetType();
        var pi = t.GetProperty(name);
        if (pi != null) { try { return pi.GetValue(target); } catch { } }
        var fi = t.GetField(name);
        if (fi != null) { try { return fi.GetValue(target); } catch { } }
        return null;
    }

    // Dedupe by (type, severity, path|name, message) composite.
    public static System.Collections.Generic.List<object> DeduplicateFindings(System.Collections.Generic.IEnumerable<object> findings)
    {
        var seen = new System.Collections.Generic.List<string>();
        var outList = new System.Collections.Generic.List<object>();
        foreach (var f in findings)
        {
            var key = GetPropertyValue<string>(f, "type", "") + "|" +
                      GetPropertyValue<string>(f, "severity", "") + "|" +
                      (GetPropertyValue<string>(f, "path", null) ?? GetPropertyValue<string>(f, "name", null) ?? "") + "|" +
                      GetPropertyValue<string>(f, "message", "");
            bool dup = false;
            for (int i = 0; i < seen.Count; i++) if (seen[i] == key) { dup = true; break; }
            if (dup) continue;
            seen.Add(key);
            outList.Add(f);
        }
        return outList;
    }

    // Minimal finding-type → recipe-name mapper.
    public static System.Collections.Generic.List<object> BuildSuggestedNextSkills(System.Collections.Generic.IEnumerable<object> findings)
    {
        var suggestions = new System.Collections.Generic.List<object>();
        var seen = new System.Collections.Generic.List<string>();
        foreach (var f in findings)
        {
            var type = GetPropertyValue<string>(f, "type", "");
            string skill = null, reason = null;
            switch (type)
            {
                case "MissingReference": skill = "cleaner_find_missing_references"; reason = "Scan and repair missing asset references"; break;
                case "MissingMainCamera": skill = "camera_create"; reason = "Create a MainCamera-tagged camera"; break;
                case "MissingLight": skill = "light_create"; reason = "Add a light to the scene"; break;
                case "MissingEventSystem": skill = "xr_setup_event_system"; reason = "Create an EventSystem for UI input routing"; break;
                case "MissingCanvas": skill = "ui_create_canvas"; reason = "Create a Canvas root for UGUI content"; break;
                case "MissingAudioListener": skill = "component_add"; reason = "Attach an AudioListener to the main camera"; break;
                case "DeepHierarchy":
                case "LargeChildSet":
                case "EmptyLeafCluster":
                case "DuplicateNameCluster": skill = "scene_find_hotspots"; reason = "Inspect clutter hotspots in detail"; break;
                case "MissingRoot":
                case "MissingTagDefinition":
                case "MissingLayerDefinition": skill = "scene_contract_validate"; reason = "Re-run after fixing the missing convention"; break;
            }
            if (skill == null) continue;
            var key = skill + "|" + reason;
            bool dup = false;
            for (int i = 0; i < seen.Count; i++) if (seen[i] == key) { dup = true; break; }
            if (dup) continue;
            seen.Add(key);
            suggestions.Add(new { skill, reason });
        }
        return suggestions;
    }

    // Hand-parse ["a","b","c"] JSON arrays (Newtonsoft.Json unavailable in Unity_RunCommand).
    public static string[] ParseOptionalStringArray(string rawJson, string[] defaults)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return defaults ?? System.Array.Empty<string>();
        var list = new System.Collections.Generic.List<string>();
        int i = rawJson.IndexOf('[');
        if (i < 0) return defaults ?? System.Array.Empty<string>();
        i++;
        while (i < rawJson.Length)
        {
            while (i < rawJson.Length && rawJson[i] != '"' && rawJson[i] != ']') i++;
            if (i >= rawJson.Length || rawJson[i] == ']') break;
            int ks = i + 1; int ke = ks;
            while (ke < rawJson.Length) { if (rawJson[ke] == '\\' && ke + 1 < rawJson.Length) { ke += 2; continue; } if (rawJson[ke] == '"') break; ke++; }
            var v = rawJson.Substring(ks, ke - ks).Trim();
            if (!string.IsNullOrWhiteSpace(v) && !ContainsIgnoreCase(list, v)) list.Add(v);
            i = ke + 1;
        }
        return list.ToArray();
    }

    public static T GetPropertyValue<T>(object target, string name, T fallback = default)
    {
        if (target == null) return fallback;
        object value = null;
        if (target is System.Collections.Generic.IDictionary<string, object> dict)
        {
            if (dict.TryGetValue(name, out var v)) value = v;
        }
        else if (target is System.Collections.IDictionary legacy)
        {
            if (legacy.Contains(name)) value = legacy[name];
        }
        if (value == null)
        {
            var t = target.GetType();
            var pi = t.GetProperty(name);
            if (pi != null) { try { value = pi.GetValue(target); } catch { } }
            else
            {
                var fi = t.GetField(name);
                if (fi != null) { try { value = fi.GetValue(target); } catch { } }
            }
        }
        if (value == null) return fallback;
        if (value is T typed) return typed;
        try { return (T)System.Convert.ChangeType(value, typeof(T)); } catch { return fallback; }
    }
}
```
