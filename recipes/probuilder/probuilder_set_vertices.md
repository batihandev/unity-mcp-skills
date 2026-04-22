# probuilder_set_vertices

Set absolute positions for specific vertices on a ProBuilder mesh.

**Signature:** `ProBuilderSetVertices(string name = null, int instanceId = 0, string path = null, string vertices = null)`

**Returns:** `{ success, name, instanceId, setVertexCount, totalVertices }`

## Notes

- `vertices`: JSON array of `{ index, x, y, z }` objects (required).
- Out-of-range indices are skipped silently.
- Positions are in local object space.
- Use `probuilder_get_vertices` first to read current positions.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

**Requires:** `com.unity.probuilder` package.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using System.Linq;

internal sealed class _VertexPos
{
    public int index;
    public float x, y, z;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string vertices = "[]";

        var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        if (Validate.RequiredJsonArray(vertices, "vertices") is object jsonErr) { result.SetResult(jsonErr); return; }

        Undo.RecordObject(pbMesh, "Set Vertices");
        WorkflowManager.SnapshotObject(pbMesh);

        var positions = pbMesh.positions.ToArray();
        var items = ParseVertices(vertices);
        int setCount = 0;

        foreach (var item in items)
        {
            if (item.index >= 0 && item.index < positions.Length)
            {
                positions[item.index] = new Vector3(item.x, item.y, item.z);
                setCount++;
            }
        }

        pbMesh.positions = positions;
        pbMesh.ToMesh();
        pbMesh.Refresh();

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            instanceId = pbMesh.gameObject.GetInstanceID(),
            setVertexCount = setCount,
            totalVertices = pbMesh.vertexCount
        });
    }

    private static (ProBuilderMesh mesh, object error) FindProBuilderMesh(string fname, int fid, string fpath)
    {
        var (go, findErr) = GameObjectFinder.FindOrError(fname, fid, fpath);
        if (findErr != null) return (null, findErr);

        var pbMesh = go.GetComponent<ProBuilderMesh>();
        if (pbMesh == null)
            return (null, new { error = "GameObject '" + go.name + "' does not have a ProBuilderMesh component" });

        return (pbMesh, null);
    }

    private static List<_VertexPos> ParseVertices(string json)
    {
        var list = new List<_VertexPos>();
        if (string.IsNullOrEmpty(json)) return list;
        int i = 0;
        while (i < json.Length && json[i] != '[') i++;
        if (i >= json.Length) return list;
        i++;
        while (i < json.Length)
        {
            while (i < json.Length && (json[i] == ' ' || json[i] == ',' || json[i] == '\n' || json[i] == '\r' || json[i] == '\t')) i++;
            if (i >= json.Length || json[i] == ']') break;
            if (json[i] != '{') { i++; continue; }
            int end = FindObjectEnd(json, i);
            if (end < 0) break;
            var item = ParseOne(json.Substring(i, end - i + 1));
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

    private static _VertexPos ParseOne(string obj)
    {
        var item = new _VertexPos();
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
            int vs = i;
            while (i < obj.Length && obj[i] != ',' && obj[i] != '}') i++;
            string val = obj.Substring(vs, i - vs).Trim();
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            switch (key)
            {
                case "index": int.TryParse(val, System.Globalization.NumberStyles.Integer, ci, out item.index); break;
                case "x": float.TryParse(val, System.Globalization.NumberStyles.Float, ci, out item.x); break;
                case "y": float.TryParse(val, System.Globalization.NumberStyles.Float, ci, out item.y); break;
                case "z": float.TryParse(val, System.Globalization.NumberStyles.Float, ci, out item.z); break;
            }
        }
        return item;
    }
}
```
