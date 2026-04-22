# scene_diff

**Skill:** `scene_diff`
**C# method:** `PerceptionSkills.SceneDiff`

## Signature

```
SceneDiff(string snapshotJson = null)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `snapshotJson` | `string` | `null` | JSON snapshot from a previous `scene_diff` call. When null, the skill captures and returns the current state |

## Return Shape

**Snapshot mode** (snapshotJson is null): returns `success`, `mode="snapshot"`, `sceneName`, `objectCount`, `snapshot` array (store this for later comparison).

**Diff mode** (snapshotJson provided): returns `success`, `mode="diff"`, `sceneName`, `summary` (addedCount, removedCount, modifiedCount), `added` array, `removed` array, `modified` array (each with instanceId, name, path, changes[]).

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

## RunCommand Recipe

Two entry modes driven by `snapshotJson`:
- **Snapshot** (`snapshotJson = null`): walk the active scene and return a flat list of objects (name, path, component list, transform). Agents save the entire JSON log output.
- **Diff** (`snapshotJson` set): parse the prior snapshot, walk the scene again, emit `added` / `removed` / `modified` relative to the prior state.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

internal sealed class _SceneDiffEntry
{
    public int instanceId;
    public string name;
    public string path;
    public string componentList;
    public float px, py, pz;
    public float rx, ry, rz;
    public float sx, sy, sz;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string snapshotJson = null;

        var current = CaptureSceneSnapshot();

        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            result.SetResult(new
            {
                success = true,
                mode = "snapshot",
                sceneName = SceneManager.GetActiveScene().name,
                objectCount = current.Count,
                snapshot = current
            });
            return;
        }

        List<_SceneDiffEntry> previous;
        try { previous = ParseSnapshot(snapshotJson); }
        catch (Exception ex) { result.SetResult(new { error = "Invalid snapshotJson: " + ex.Message }); return; }

        var prevMap = new Dictionary<int, _SceneDiffEntry>();
        foreach (var e in previous) if (e.instanceId != 0) prevMap[e.instanceId] = e;

        var curMap = new Dictionary<int, _SceneDiffEntry>();
        foreach (var e in current) if (e.instanceId != 0) curMap[e.instanceId] = e;

        var added = new List<object>();
        var removed = new List<object>();
        var modified = new List<object>();

        foreach (var kv in curMap)
            if (!prevMap.ContainsKey(kv.Key))
                added.Add(new { instanceId = kv.Key, name = kv.Value.name, path = kv.Value.path });

        foreach (var kv in prevMap)
            if (!curMap.ContainsKey(kv.Key))
                removed.Add(new { instanceId = kv.Key, name = kv.Value.name, path = kv.Value.path });

        foreach (var kv in curMap)
        {
            if (!prevMap.TryGetValue(kv.Key, out var prev)) continue;
            var cur = kv.Value;
            var changes = new List<string>();
            if (!string.Equals(cur.name, prev.name, StringComparison.Ordinal)) changes.Add("name");
            if (!string.Equals(cur.path, prev.path, StringComparison.Ordinal)) changes.Add("path");
            if (!string.Equals(cur.componentList, prev.componentList, StringComparison.Ordinal)) changes.Add("components");
            if (VectorDiffers(cur.px, cur.py, cur.pz, prev.px, prev.py, prev.pz)) changes.Add("position");
            if (VectorDiffers(cur.rx, cur.ry, cur.rz, prev.rx, prev.ry, prev.rz)) changes.Add("rotation");
            if (VectorDiffers(cur.sx, cur.sy, cur.sz, prev.sx, prev.sy, prev.sz)) changes.Add("scale");
            if (changes.Count > 0)
                modified.Add(new { instanceId = kv.Key, name = cur.name, path = cur.path, changes = changes.ToArray() });
        }

        result.SetResult(new
        {
            success = true,
            mode = "diff",
            sceneName = SceneManager.GetActiveScene().name,
            summary = new { addedCount = added.Count, removedCount = removed.Count, modifiedCount = modified.Count },
            added = added.ToArray(),
            removed = removed.ToArray(),
            modified = modified.ToArray()
        });
    }

    private static List<_SceneDiffEntry> CaptureSceneSnapshot()
    {
        var list = new List<_SceneDiffEntry>();
        foreach (var go in GameObjectFinder.GetSceneObjects())
        {
            if (go == null) continue;
            var comps = go.GetComponents<Component>();
            var names = new string[comps.Length];
            for (int i = 0; i < comps.Length; i++) names[i] = comps[i] != null ? comps[i].GetType().Name : "null";
            var t = go.transform;
            list.Add(new _SceneDiffEntry
            {
                instanceId = go.GetInstanceID(),
                name = go.name,
                path = GameObjectFinder.GetPath(go),
                componentList = string.Join(",", names),
                px = t.position.x, py = t.position.y, pz = t.position.z,
                rx = t.eulerAngles.x, ry = t.eulerAngles.y, rz = t.eulerAngles.z,
                sx = t.localScale.x, sy = t.localScale.y, sz = t.localScale.z,
            });
        }
        return list;
    }

    private static bool VectorDiffers(float ax, float ay, float az, float bx, float by, float bz)
    {
        const float eps = 0.0001f;
        return Mathf.Abs(ax - bx) > eps || Mathf.Abs(ay - by) > eps || Mathf.Abs(az - bz) > eps;
    }

    // Newtonsoft.Json is unavailable in Unity_RunCommand dynamic compile context.
    // Hand-parse the snapshot JSON emitted by MiniJson (_shared/execution_result).
    private static List<_SceneDiffEntry> ParseSnapshot(string json)
    {
        var list = new List<_SceneDiffEntry>();
        int i = json.IndexOf("\"snapshot\"", StringComparison.Ordinal);
        if (i >= 0) { i = json.IndexOf('[', i); if (i < 0) return list; i++; }
        else { i = json.IndexOf('['); if (i < 0) return list; i++; }
        while (i < json.Length)
        {
            while (i < json.Length && (json[i] == ' ' || json[i] == ',' || json[i] == '\n' || json[i] == '\r' || json[i] == '\t')) i++;
            if (i >= json.Length || json[i] == ']') break;
            if (json[i] != '{') { i++; continue; }
            int end = FindObjectEnd(json, i);
            if (end < 0) break;
            list.Add(ParseOne(json.Substring(i, end - i + 1)));
            i = end + 1;
        }
        return list;
    }

    private static int FindObjectEnd(string s, int start)
    {
        int depth = 0; bool inStr = false;
        for (int i = start; i < s.Length; i++)
        {
            char c = s[i];
            if (inStr) { if (c == '\\' && i + 1 < s.Length) { i++; continue; } if (c == '"') inStr = false; continue; }
            if (c == '"') { inStr = true; continue; }
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) return i; }
        }
        return -1;
    }

    private static _SceneDiffEntry ParseOne(string obj)
    {
        return new _SceneDiffEntry
        {
            instanceId = (int)ReadNumberField(obj, "instanceId"),
            name = ReadStringField(obj, "name"),
            path = ReadStringField(obj, "path"),
            componentList = ReadStringField(obj, "componentList"),
            px = ReadNumberField(obj, "px"), py = ReadNumberField(obj, "py"), pz = ReadNumberField(obj, "pz"),
            rx = ReadNumberField(obj, "rx"), ry = ReadNumberField(obj, "ry"), rz = ReadNumberField(obj, "rz"),
            sx = ReadNumberField(obj, "sx"), sy = ReadNumberField(obj, "sy"), sz = ReadNumberField(obj, "sz"),
        };
    }

    private static string ReadStringField(string obj, string key)
    {
        var marker = "\"" + key + "\"";
        int k = obj.IndexOf(marker, StringComparison.Ordinal);
        if (k < 0) return "";
        int q = obj.IndexOf('"', k + marker.Length);
        if (q < 0) return "";
        int end = q + 1;
        while (end < obj.Length)
        {
            if (obj[end] == '\\' && end + 1 < obj.Length) { end += 2; continue; }
            if (obj[end] == '"') break;
            end++;
        }
        return obj.Substring(q + 1, end - q - 1);
    }

    private static float ReadNumberField(string obj, string key)
    {
        var marker = "\"" + key + "\"";
        int k = obj.IndexOf(marker, StringComparison.Ordinal);
        if (k < 0) return 0f;
        int c = obj.IndexOf(':', k + marker.Length);
        if (c < 0) return 0f;
        int s = c + 1;
        while (s < obj.Length && (obj[s] == ' ' || obj[s] == '\t')) s++;
        int e = s;
        while (e < obj.Length && (obj[e] == '-' || obj[e] == '+' || obj[e] == '.' || (obj[e] >= '0' && obj[e] <= '9') || obj[e] == 'e' || obj[e] == 'E')) e++;
        if (e == s) return 0f;
        float.TryParse(obj.Substring(s, e - s), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v);
        return v;
    }
}
```

## Workflow

1. Call with `snapshotJson = null` before making changes — save the returned `snapshot` JSON array.
2. Make your scene changes.
3. Call again with `snapshotJson = <saved snapshot>` — receive the diff.

## Notes

- Tracked change properties: `name`, `path`, `components`, `position`, `rotation`, `scale`.
- Objects are matched by `instanceId`; renamed or moved objects still match correctly.
- Useful for validating that a tool call produced exactly the expected changes and nothing else.
