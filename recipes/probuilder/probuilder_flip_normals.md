# probuilder_flip_normals

Reverse face winding (flip normals) on a ProBuilder mesh.

**Signature:** `ProBuilderFlipNormals(string name = null, int instanceId = 0, string path = null, string faceIndexes = null)`

**Returns:** `{ success, name, instanceId, flippedCount }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to flip all faces.
- Use when a face appears invisible (back-face culling) and you want to make it face the camera.
- For consistent outward normals across the whole mesh prefer `probuilder_conform_normals`.

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
        if (faces.Count == 0)
        { result.SetResult(new { error = "No faces selected. Provide faceIndexes or omit to flip all." }); return; }

        Undo.RecordObject(pbMesh, "Flip Normals");
        WorkflowManager.SnapshotObject(pbMesh);

        foreach (var face in faces)
            face.Reverse();

        pbMesh.ToMesh();
        pbMesh.Refresh();

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            instanceId = pbMesh.gameObject.GetInstanceID(),
            flippedCount = faces.Count
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
