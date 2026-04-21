# probuilder_conform_normals

Make face normals point consistently outward on a ProBuilder mesh.

**Signature:** `ProBuilderConformNormals(string name = null, int instanceId = 0, string path = null, string faceIndexes = null)`

**Returns:** `{ success, name, instanceId, status, notification, faceCount }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to conform all faces.
- Unlike `probuilder_flip_normals`, this operation detects the correct outward direction rather than simply reversing winding.
- `status` reflects the ProBuilder `ActionResult` status string.

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
                        { result.SetResult(new { error = "No faces selected. Provide faceIndexes or omit to conform all." }); return; }

                    Undo.RecordObject(pbMesh, "Conform Normals");
                    WorkflowManager.SnapshotObject(pbMesh);

                    var result = pbMesh.ConformNormals(faces);

                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        status = result.status.ToString(),
                        notification = result.notification ?? "",
                        faceCount = faces.Count
                    }); return; }
        #endif
    }
}
```
