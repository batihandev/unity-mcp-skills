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
                        { result.SetResult(new { error = "No faces selected. Provide faceIndexes or omit to flip all." }); return; }

                    Undo.RecordObject(pbMesh, "Flip Normals");
                    WorkflowManager.SnapshotObject(pbMesh);

                    foreach (var face in faces)
                        face.Reverse();

                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        flippedCount = faces.Count
                    }); return; }
        #endif
    }
}
```
