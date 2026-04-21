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
                    if (faces.Count < 2)
                        { result.SetResult(new { error = "At least 2 faces are required to merge. Provide faceIndexes as comma-separated indices." }); return; }

                    Undo.RecordObject(pbMesh, "Merge Faces");
                    WorkflowManager.SnapshotObject(pbMesh);

                    var merged = MergeElements.Merge(pbMesh, faces);
                    if (merged == null)
                        { result.SetResult(new { error = "Failed to merge faces. Ensure the selected faces are valid." }); return; }

                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        mergedFromCount = faces.Count,
                        totalFaces = pbMesh.faceCount,
                        totalVertices = pbMesh.vertexCount
                    }); return; }
        #endif
    }
}
```
