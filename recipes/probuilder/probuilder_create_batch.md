# probuilder_create_batch

Create multiple ProBuilder shapes in one call. Preferred for scene blockout with 2+ shapes.

**Signature:** `ProBuilderCreateBatch(string items, string defaultParent = null)`

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, name, instanceId, shape }] }`

## Notes

- `items`: JSON array; each element accepts `shape`, `name`, `x`, `y`, `z`, `sizeX`, `sizeY`, `sizeZ`, `rotX`, `rotY`, `rotZ`, `parent`, `materialPath`.
- `defaultParent`: applied to items that omit their own `parent`.
- `y` is the center of each shape — set `y = -sizeY/2` to place the bottom at world origin.
- Supported shapes: `Cube`, `Sphere`, `Cylinder`, `Cone`, `Torus`, `Prism`, `Arch`, `Pipe`, `Stairs`, `Door`, `Plane`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.probuilder` package.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.ProBuilder.Shapes;
using System;
using System.Collections.Generic;

internal sealed class _PBBatchItem
{
    public string shape = "Cube";
    public string name;
    public float x, y, z;
    public float sizeX = 1, sizeY = 1, sizeZ = 1;
    public float rotX, rotY, rotZ;
    public string parent;
    public string materialPath;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Minimal inline parse — agents should replace with real JSON payload.
        string items = "[{\"shape\":\"Cube\",\"name\":\"Box1\",\"x\":0,\"y\":0,\"z\":0,\"sizeX\":1,\"sizeY\":1,\"sizeZ\":1}]";
        string defaultParent = null;

        var parsed = ParseItems(items);
        if (parsed == null || parsed.Count == 0)
        { result.SetResult(new { error = "items must be a non-empty JSON array" }); return; }

        var results = new List<object>();
        int successCount = 0;
        int failCount = 0;

        foreach (var item in parsed)
        {
            if (!ShapeTypeMap.TryGetValue(item.shape ?? "Cube", out var shapeType))
            {
                results.Add(new { success = false, name = item.name, error = "Unknown shape: " + item.shape });
                failCount++;
                continue;
            }

            var pos = new Vector3(item.x, item.y, item.z);
            var size = new Vector3(item.sizeX, item.sizeY, item.sizeZ);
            var rot = new Vector3(item.rotX, item.rotY, item.rotZ);
            var parent = item.parent ?? defaultParent;

            var pbMesh = CreatePBShape(shapeType, item.name, pos, size, rot, parent);
            if (pbMesh == null)
            {
                results.Add(new { success = false, name = item.name, error = "Failed to create shape: " + item.shape });
                failCount++;
                continue;
            }

            var go = pbMesh.gameObject;

            if (!string.IsNullOrEmpty(item.materialPath))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(item.materialPath);
                if (mat != null)
                    pbMesh.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            Undo.RegisterCreatedObjectUndo(go, "Create PB Shape");
            WorkflowManager.SnapshotObject(go, SnapshotType.Created);

            results.Add(new { success = true, name = go.name, instanceId = go.GetInstanceID(), shape = item.shape ?? "Cube" });
            successCount++;
        }

        result.SetResult(new
        {
            success = failCount == 0,
            totalItems = parsed.Count,
            successCount,
            failCount,
            results
        });
    }

    private static readonly Dictionary<string, Type> ShapeTypeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
    {
        { "Cube", typeof(Cube) }, { "Sphere", typeof(Sphere) }, { "Cylinder", typeof(Cylinder) },
        { "Cone", typeof(Cone) }, { "Torus", typeof(Torus) }, { "Prism", typeof(Prism) },
        { "Arch", typeof(Arch) }, { "Pipe", typeof(Pipe) }, { "Stairs", typeof(Stairs) },
        { "Door", typeof(Door) }, { "Plane", typeof(UnityEngine.ProBuilder.Shapes.Plane) },
    };

    private static ProBuilderMesh CreatePBShape(Type shapeType, string objName, Vector3 pos, Vector3 size, Vector3 rot, string parentName)
    {
        var pbMesh = ShapeFactory.Instantiate(shapeType);
        if (pbMesh == null) return null;

        var go = pbMesh.gameObject;
        if (!string.IsNullOrEmpty(objName)) go.name = objName;

        go.transform.localScale = size;
        pbMesh.FreezeScaleTransform();
        pbMesh.ToMesh();
        pbMesh.Refresh();

        go.transform.position = pos;
        go.transform.eulerAngles = rot;

        if (!string.IsNullOrEmpty(parentName))
        {
            var parent = GameObjectFinder.Find(name: parentName);
            if (parent != null) go.transform.SetParent(parent.transform, true);
        }

        return pbMesh;
    }

    private static List<_PBBatchItem> ParseItems(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        var list = new List<_PBBatchItem>();
        int i = 0;
        while (i < json.Length && json[i] != '[') i++;
        if (i >= json.Length) return null;
        i++;
        while (i < json.Length)
        {
            while (i < json.Length && (json[i] == ' ' || json[i] == ',' || json[i] == '\n' || json[i] == '\r' || json[i] == '\t')) i++;
            if (i >= json.Length || json[i] == ']') break;
            if (json[i] != '{') { i++; continue; }
            int end = FindObjectEnd(json, i);
            if (end < 0) break;
            var item = ParseObject(json.Substring(i, end - i + 1));
            if (item != null) list.Add(item);
            i = end + 1;
        }
        return list;
    }

    private static int FindObjectEnd(string s, int start)
    {
        int depth = 0;
        bool inStr = false;
        for (int i = start; i < s.Length; i++)
        {
            char c = s[i];
            if (inStr)
            {
                if (c == '\\' && i + 1 < s.Length) { i++; continue; }
                if (c == '"') inStr = false;
                continue;
            }
            if (c == '"') { inStr = true; continue; }
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) return i; }
        }
        return -1;
    }

    private static _PBBatchItem ParseObject(string obj)
    {
        var item = new _PBBatchItem();
        int i = 0;
        while (i < obj.Length)
        {
            while (i < obj.Length && obj[i] != '"') i++;
            if (i >= obj.Length) break;
            int keyStart = i + 1;
            i = keyStart;
            while (i < obj.Length && obj[i] != '"') { if (obj[i] == '\\' && i + 1 < obj.Length) i++; i++; }
            if (i >= obj.Length) break;
            string key = obj.Substring(keyStart, i - keyStart);
            i++;
            while (i < obj.Length && obj[i] != ':') i++;
            if (i >= obj.Length) break;
            i++;
            while (i < obj.Length && (obj[i] == ' ' || obj[i] == '\t' || obj[i] == '\n' || obj[i] == '\r')) i++;
            if (i >= obj.Length) break;
            string val;
            if (obj[i] == '"')
            {
                int vs = i + 1;
                i = vs;
                while (i < obj.Length && obj[i] != '"') { if (obj[i] == '\\' && i + 1 < obj.Length) i++; i++; }
                val = obj.Substring(vs, i - vs);
                i++;
            }
            else
            {
                int vs = i;
                while (i < obj.Length && obj[i] != ',' && obj[i] != '}') i++;
                val = obj.Substring(vs, i - vs).Trim();
            }
            SetField(item, key, val);
        }
        return item;
    }

    private static void SetField(_PBBatchItem item, string key, string val)
    {
        switch (key)
        {
            case "shape": item.shape = val; break;
            case "name": item.name = val; break;
            case "x": float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out item.x); break;
            case "y": float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out item.y); break;
            case "z": float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out item.z); break;
            case "sizeX": float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out item.sizeX); break;
            case "sizeY": float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out item.sizeY); break;
            case "sizeZ": float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out item.sizeZ); break;
            case "rotX": float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out item.rotX); break;
            case "rotY": float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out item.rotY); break;
            case "rotZ": float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out item.rotZ); break;
            case "parent": item.parent = val; break;
            case "materialPath": item.materialPath = val; break;
        }
    }
}
```
