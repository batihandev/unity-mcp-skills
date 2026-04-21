# probuilder_subdivide

Subdivide faces by connecting edge midpoints on a ProBuilder mesh.

**Signature:** `ProBuilderSubdivide(string name = null, int instanceId = 0, string path = null, string faceIndexes = null)`

**Returns:** `{ success, name, instanceId, totalFaces, totalVertices }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to subdivide all faces.
- Each subdivision pass multiplies face count roughly by 4 — use sparingly on high-poly meshes.
- Use `probuilder_get_info` to check face count before subdividing.

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

                    if (!string.IsNullOrEmpty(faceIndexes))
                    {
                        var faces = SelectFaces(pbMesh, faceIndexes);
                        if (faces.Count == 0)
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

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        totalFaces = pbMesh.faceCount,
                        totalVertices = pbMesh.vertexCount
                    }); return; }
        #endif
    }
}
```
