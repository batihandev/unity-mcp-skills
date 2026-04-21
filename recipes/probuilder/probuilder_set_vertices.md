# probuilder_set_vertices

Set absolute positions for specific vertices on a ProBuilder mesh.

**Signature:** `ProBuilderSetVertices(string name = null, int instanceId = 0, string path = null, string vertices = null)`

**Returns:** `{ success, name, instanceId, setVertexCount, totalVertices }`

## Notes

- `vertices`: JSON array of `{ index, x, y, z }` objects (required).
- Out-of-range indices are skipped silently.
- Positions are in local object space.
- Use `probuilder_get_vertices` first to read current positions.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
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

                    if (Validate.RequiredJsonArray(vertices, "vertices") is object jsonErr) { result.SetResult(jsonErr); return; }

                    Undo.RecordObject(pbMesh, "Set Vertices");
                    WorkflowManager.SnapshotObject(pbMesh);

                    var positions = pbMesh.positions.ToArray();
                    var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<VertexPosItem>>(vertices);
                    int setCount = 0;

                    foreach (var item in items)
                    {
                        if (item.index >= 0 && item.index < positions.Length)
                        {
                            positions[item.index] = new Vector3(item.x, item.y, item.z);
                            setCount++;
                        }
                    }

                    pbMesh.positions = positions;
                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        setVertexCount = setCount,
                        totalVertices = pbMesh.vertexCount
                    }); return; }
        #endif
    }
}
```
