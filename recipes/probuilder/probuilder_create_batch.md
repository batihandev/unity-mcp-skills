# probuilder_create_batch

Create multiple ProBuilder shapes in one call. Preferred for scene blockout with 2+ shapes.

**Signature:** `ProBuilderCreateBatch(string items, string defaultParent = null)`

**Returns:** `{ success, results: [{ success, name, instanceId, shape }] }`

## Notes

- `items`: JSON array; each element accepts `shape`, `name`, `x`, `y`, `z`, `sizeX`, `sizeY`, `sizeZ`, `rotX`, `rotY`, `rotZ`, `parent`, `materialPath`.
- `defaultParent`: applied to items that omit their own `parent`.
- `y` is the center of each shape — set `y = -sizeY/2` to place the bottom at world origin.
- Requires `com.unity.probuilder` package.

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
                    { result.SetResult(BatchExecutor.Execute<PBBatchItem>(items, item =>
                    {
                        if (!ShapeTypeMap.TryGetValue(item.shape ?? "Cube", out var shapeType))
                            return new { error = $"Unknown shape: {item.shape}" };

                        var pos = new Vector3(item.x, item.y, item.z);
                        var size = new Vector3(item.sizeX, item.sizeY, item.sizeZ);
                        var rot = new Vector3(item.rotX, item.rotY, item.rotZ);
                        var parent = item.parent ?? defaultParent;

                        var pbMesh = CreatePBShape(shapeType, item.name, pos, size, rot, parent);
                        if (pbMesh == null)
                            return new { error = $"Failed to create shape: {item.shape}" };

                        var go = pbMesh.gameObject;

                        // Apply material if specified
                        if (!string.IsNullOrEmpty(item.materialPath))
                        {
                            var mat = AssetDatabase.LoadAssetAtPath<Material>(item.materialPath);
                            if (mat != null)
                                pbMesh.GetComponent<MeshRenderer>().sharedMaterial = mat;
                        }

                        Undo.RegisterCreatedObjectUndo(go, "Create PB Shape");
                        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

                        return new { success = true, name = go.name, instanceId = go.GetInstanceID(), shape = item.shape ?? "Cube" };
                    }, item => item.name ?? item.shape)); return; }
        #endif
    }
}
```
