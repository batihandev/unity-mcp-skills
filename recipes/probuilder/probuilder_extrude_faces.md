# probuilder_extrude_faces

Extrude selected faces along their normals.

**Signature:** `ProBuilderExtrudeFaces(string name = null, int instanceId = 0, string path = null, string faceIndexes = null, float distance = 0.5f, string method = "FaceNormal")`

**Returns:** `{ success, name, instanceId, extrudedFaceCount, method, distance, totalFaces, totalVertices }`

## Notes

- `faceIndexes`: comma-separated face indices, e.g. `"0,1,2"`. Omit to extrude all faces.
- `distance`: extrude amount in meters (default `0.5`).
- `method`: `FaceNormal` (default), `IndividualFaces`, `VertexNormal`.
- Call `probuilder_get_info` first to confirm face count before selecting indexes.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

**Requires:** `com.unity.probuilder` package.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string faceIndexes = null;
        float distance = 0.5f;
        string method = "FaceNormal";

        var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        if (!Enum.TryParse<ExtrudeMethod>(method, true, out var extrudeMethod))
        { result.SetResult(new { error = "Unknown extrude method: " + method + ". Available: IndividualFaces, FaceNormal, VertexNormal" }); return; }

        var faces = SelectFaces(pbMesh, faceIndexes);
        if (faces.Count == 0)
        { result.SetResult(new { error = "No faces selected. Provide faceIndexes as comma-separated indices (e.g. \"0,1,2\"), or omit to extrude all faces." }); return; }

        Undo.RecordObject(pbMesh, "Extrude Faces");
        WorkflowManager.SnapshotObject(pbMesh);

        var newFaces = pbMesh.Extrude(faces, extrudeMethod, distance);

        pbMesh.ToMesh();
        pbMesh.Refresh();

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            instanceId = pbMesh.gameObject.GetInstanceID(),
            extrudedFaceCount = newFaces?.Length ?? 0,
            method,
            distance,
            totalFaces = pbMesh.faceCount,
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

    private static List<Face> SelectFaces(ProBuilderMesh mesh, string faceIndexes)
    {
        var allFaces = mesh.faces;
        if (string.IsNullOrEmpty(faceIndexes))
            return allFaces.ToList();

        var indices = ParseIntList(faceIndexes);
        if (indices == null) return new List<Face>();

        return indices
            .Where(i => i >= 0 && i < allFaces.Count)
            .Select(i => allFaces[i])
            .ToList();
    }

    private static List<int> ParseIntList(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return null;
        var list = new List<int>();
        foreach (var part in csv.Split(','))
        {
            if (int.TryParse(part.Trim(), out var val))
                list.Add(val);
        }
        return list.Count > 0 ? list : null;
    }
}
```
