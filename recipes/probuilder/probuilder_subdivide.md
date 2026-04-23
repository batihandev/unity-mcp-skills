# probuilder_subdivide

Subdivide faces by connecting edge midpoints on a ProBuilder mesh.

**Signature:** `ProBuilderSubdivide(string name = null, int instanceId = 0, string path = null, string faceIndexes = null)`

**Returns:** `{ success, name, instanceId, totalFaces, totalVertices }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to subdivide all faces.
- Each subdivision pass multiplies face count roughly by 4 — use sparingly on high-poly meshes.
- Use `probuilder_get_info` to check face count before subdividing.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.probuilder` package.

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

        if (!string.IsNullOrEmpty(faceIndexes))
        {
            var preview = SelectFaces(pbMesh, faceIndexes);
            if (preview.Count == 0)
            { result.SetResult(new { error = "No valid face indices provided." }); return; }
        }

        Undo.RecordObject(pbMesh, "Subdivide");
        WorkflowManager.SnapshotObject(pbMesh);

        if (string.IsNullOrEmpty(faceIndexes))
        {
            var allFaces = pbMesh.faces.ToArray();
            ConnectElements.Connect(pbMesh, allFaces);
        }
        else
        {
            ConnectElements.Connect(pbMesh, SelectFaces(pbMesh, faceIndexes));
        }

        pbMesh.ToMesh();
        pbMesh.Refresh();

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            instanceId = pbMesh.gameObject.GetInstanceID(),
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
