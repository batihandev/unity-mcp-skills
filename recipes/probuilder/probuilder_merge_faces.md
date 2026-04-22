# probuilder_merge_faces

Merge 2 or more faces into a single face on a ProBuilder mesh.

**Signature:** `ProBuilderMergeFaces(string name = null, int instanceId = 0, string path = null, string faceIndexes = null)`

**Returns:** `{ success, name, instanceId, mergedFromCount, totalFaces, totalVertices }`

## Notes

- `faceIndexes`: comma-separated face indices (at least 2 required), e.g. `"2,3"`. Omit to merge all faces.
- Returns an error if fewer than 2 valid faces are selected.
- Useful to clean up over-subdivided areas or combine coplanar faces.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder.FindOrError(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

**Requires:** `com.unity.probuilder` package.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
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

        var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var faces = SelectFaces(pbMesh, faceIndexes);
        if (faces.Count < 2)
        { result.SetResult(new { error = "At least 2 faces are required to merge. Provide faceIndexes as comma-separated indices." }); return; }

        Undo.RecordObject(pbMesh, "Merge Faces");
        WorkflowManager.SnapshotObject(pbMesh);

        var merged = MergeElements.Merge(pbMesh, faces);
        if (merged == null)
        { result.SetResult(new { error = "Failed to merge faces. Ensure the selected faces are valid." }); return; }

        pbMesh.ToMesh();
        pbMesh.Refresh();

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            instanceId = pbMesh.gameObject.GetInstanceID(),
            mergedFromCount = faces.Count,
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
