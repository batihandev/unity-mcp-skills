# probuilder_delete_faces

Delete faces by index from a ProBuilder mesh.

**Signature:** `ProBuilderDeleteFaces(string name = null, int instanceId = 0, string path = null, string faceIndexes = null)`

**Returns:** `{ success, name, instanceId, deletedCount, remainingFaces, remainingVertices }`

## Notes

- `faceIndexes` is required (comma-separated integers, e.g. `"0,1"`).
- Out-of-range indices are silently skipped; at least one valid index is required.
- Call `probuilder_get_info` first to verify face count.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        #if !PROBUILDER
                    { result.SetResult(NoProBuilder()); return; }
        #else
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
                        { result.SetResult(new { error = $"No valid face indices. Mesh has {allFaces.Count} faces (0-{allFaces.Count - 1})." }); return; }

                    Undo.RecordObject(pbMesh, "Delete Faces");
                    WorkflowManager.SnapshotObject(pbMesh);

                    var facesToDelete = validIndices.Select(i => allFaces[i]).ToArray();
                    pbMesh.DeleteFaces(facesToDelete);

                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        deletedCount = validIndices.Count,
                        remainingFaces = pbMesh.faceCount,
                        remainingVertices = pbMesh.vertexCount
                    }); return; }
        #endif
    }
}
```
