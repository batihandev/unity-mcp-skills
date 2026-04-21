# probuilder_detach_faces

Detach faces from shared vertices, creating independent geometry on a ProBuilder mesh.

**Signature:** `ProBuilderDetachFaces(string name = null, int instanceId = 0, string path = null, string faceIndexes = null, bool deleteSourceFaces = false)`

**Returns:** `{ success, name, instanceId, detachedFaceCount, deleteSourceFaces, totalFaces, totalVertices }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to detach all faces.
- `deleteSourceFaces`: if `true`, removes the original faces after detach (default `false`).
- Detached faces share no vertices with adjacent faces, breaking smooth shading across the boundary.

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

                    var faces = SelectFaces(pbMesh, faceIndexes);
                    if (faces.Count == 0)
                        { result.SetResult(new { error = "No faces selected. Provide faceIndexes or omit to detach all." }); return; }

                    Undo.RecordObject(pbMesh, "Detach Faces");
                    WorkflowManager.SnapshotObject(pbMesh);

                    var newFaces = pbMesh.DetachFaces(faces, deleteSourceFaces);

                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        detachedFaceCount = newFaces?.Count ?? 0,
                        deleteSourceFaces,
                        totalFaces = pbMesh.faceCount,
                        totalVertices = pbMesh.vertexCount
                    }); return; }
        #endif
    }
}
```
