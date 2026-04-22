# gameobject_create_batch

Create multiple GameObjects in one call.

**Signature:** `GameObjectCreateBatch(string items)`

`items`: JSON array of objects with fields: `name`, `primitiveType`, `x`, `y`, `z`, `rotX`, `rotY`, `rotZ`, `scaleX` (default 1), `scaleY` (default 1), `scaleZ` (default 1), `parentName`, `parentInstanceId`, `parentPath`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, name, instanceId, path, position: { x, y, z } }] }`

## Notes

- `primitiveType` accepts `Cube`, `Sphere`, `Capsule`, `Cylinder`, `Plane`, `Quad`, `Empty`, or `null`.
- Scale defaults to 1 for each axis — only applied when any scale value differs from 1.
- Rotation defaults to 0 — only applied when any rot value is non-zero.
- Each item's parent is resolved independently; a missing parent causes that item to fail without stopping the rest.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchCreateItem
{
    public string name;
    public string primitiveType;
    public float x, y, z;
    public float rotX, rotY, rotZ;
    public float scaleX = 1, scaleY = 1, scaleZ = 1;
    public string parentName;
    public int parentInstanceId;
    public string parentPath;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchCreateItem { name = "Cube1", primitiveType = "Cube" },
            new _BatchCreateItem { name = "Sphere1", primitiveType = "Sphere", x = 2 },
            new _BatchCreateItem { name = "Empty1", x = 4 },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            GameObject go;
            string primitiveType = item.primitiveType;
            if (string.IsNullOrEmpty(primitiveType) ||
                primitiveType.Equals("Empty", System.StringComparison.OrdinalIgnoreCase) ||
                primitiveType.Equals("None", System.StringComparison.OrdinalIgnoreCase))
            {
                go = new GameObject(item.name);
                primitiveType = null;
            }
            else if (System.Enum.TryParse<PrimitiveType>(primitiveType, true, out var pt))
            {
                go = GameObject.CreatePrimitive(pt);
                go.name = item.name;
            }
            else
            {
                results.Add(new { success = false, name = item.name, error = "Unknown primitive type: " + primitiveType });
                failCount++;
                continue;
            }

            if (!string.IsNullOrEmpty(item.parentName) || item.parentInstanceId != 0 || !string.IsNullOrEmpty(item.parentPath))
            {
                var (parentGo, parentErr) = GameObjectFinder.FindOrError(item.parentName, item.parentInstanceId, item.parentPath);
                if (parentErr != null)
                {
                    Object.DestroyImmediate(go);
                    results.Add(new { success = false, name = item.name, error = "Parent not found" });
                    failCount++;
                    continue;
                }
                go.transform.SetParent(parentGo.transform, false);
            }

            go.transform.localPosition = new Vector3(item.x, item.y, item.z);
            if (item.rotX != 0 || item.rotY != 0 || item.rotZ != 0)
                go.transform.eulerAngles = new Vector3(item.rotX, item.rotY, item.rotZ);
            if (item.scaleX != 1 || item.scaleY != 1 || item.scaleZ != 1)
                go.transform.localScale = new Vector3(item.scaleX, item.scaleY, item.scaleZ);

            Undo.RegisterCreatedObjectUndo(go, "Batch Create " + item.name);
            WorkflowManager.SnapshotCreatedGameObject(go, primitiveType);

            results.Add(new
            {
                success = true,
                name = go.name,
                instanceId = go.GetInstanceID(),
                path = GameObjectFinder.GetPath(go),
                position = new { x = item.x, y = item.y, z = item.z }
            });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
