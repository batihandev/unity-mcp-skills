# probuilder_project_uv

Box-project UVs onto faces of a ProBuilder mesh.

**Signature:** `ProBuilderProjectUV(string name = null, int instanceId = 0, string path = null, string faceIndexes = null, int channel = 0)`

**Returns:** `{ success, name, instanceId, projectedFaceCount, channel, method }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to project all faces.
- `channel`: UV channel `0`–`3` (default `0`). Channel `1` is the lightmap UV.
- Only box projection is supported; other UV projection modes are not available.
- Uses reflection to access `UVEditing.ProjectFacesBox` (internal in ProBuilder 5.x).

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
                        { result.SetResult(new { error = "No faces selected. Provide faceIndexes or omit to project all." }); return; }

                    if (channel < 0 || channel > 3)
                        { result.SetResult(new { error = "UV channel must be 0-3 (0=primary, 1=lightmap)" }); return; }

                    Undo.RecordObject(pbMesh, "Project UV");
                    WorkflowManager.SnapshotObject(pbMesh);

                    // UVEditing is internal — use reflection
                    if (!InvokeProjectFacesBox(pbMesh, faces.ToArray(), channel))
                        { result.SetResult(new { error = "Failed to project UVs. UVEditing.ProjectFacesBox is not accessible in this ProBuilder version." }); return; }

                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        projectedFaceCount = faces.Count,
                        channel,
                        method = "Box"
                    }); return; }
        #endif
    }
}
```
