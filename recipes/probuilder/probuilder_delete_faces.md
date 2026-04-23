# probuilder_delete_faces

Delete faces by index from a ProBuilder mesh.

**Signature:** `ProBuilderDeleteFaces(string name = null, int instanceId = 0, string path = null, string faceIndexes = null)`

**Returns:** `{ success, name, instanceId, deletedCount, remainingFaces, remainingVertices }`

## Notes
- Out-of-range indices are silently skipped; at least one valid index is required.
- Call `probuilder_get_info` first to verify face count.

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

        if (string.IsNullOrEmpty(faceIndexes))
        { result.SetResult(new { error = "faceIndexes is required (comma-separated, e.g. \"0,1,2\")" }); return; }

        var indices = ParseIntList(faceIndexes);
        if (indices == null || indices.Count == 0)
        { result.SetResult(new { error = "Invalid faceIndexes format. Use comma-separated integers." }); return; }

        var allFaces = pbMesh.faces;
        var validIndices = indices.Where(i => i >= 0 && i < allFaces.Count).ToList();
        if (validIndices.Count == 0)
        { result.SetResult(new { error = "No valid face indices. Mesh has " + allFaces.Count + " faces (0-" + (allFaces.Count - 1) + ")." }); return; }

        Undo.RecordObject(pbMesh, "Delete Faces");
        WorkflowManager.SnapshotObject(pbMesh);

        var facesToDelete = validIndices.Select(i => allFaces[i]).ToArray();
        pbMesh.DeleteFaces(facesToDelete);

        pbMesh.ToMesh();
        pbMesh.Refresh();

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            instanceId = pbMesh.gameObject.GetInstanceID(),
            deletedCount = validIndices.Count,
            remainingFaces = pbMesh.faceCount,
            remainingVertices = pbMesh.vertexCount
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
