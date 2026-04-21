# gameobject_set_transform_batch

Set transform properties for multiple objects in one call.

**Signature:** `GameObjectSetTransformBatch(string items)`

`items`: JSON array of objects. Each item supports all of the same fields as `gameobject_set_transform`: `name`, `instanceId`, `path`, `posX/Y/Z`, `rotX/Y/Z`, `scaleX/Y/Z`, `localPosX/Y/Z`, and the full set of RectTransform fields (`anchoredPosX/Y`, `anchorMinX/Y`, `anchorMaxX/Y`, `pivotX/Y`, `sizeDeltaX/Y`, `width`, `height`).

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, name, pos: { x, y, z } }] }`

## Notes

- Per-item return key for position is `pos` (not `position`).
- UI (RectTransform) properties are supported per-item, same as the single variant.
- A missing object causes that item to fail without stopping the rest.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string items = @"[
            { ""name"": ""Cube1"", ""posX"": 0, ""posY"": 1, ""posZ"": 0 },
            { ""name"": ""Cube2"", ""posX"": 3, ""rotY"": 45, ""scaleX"": 2, ""scaleY"": 2, ""scaleZ"": 2 },
            { ""instanceId"": 12345, ""localPosX"": 1, ""localPosY"": 0, ""localPosZ"": 0 }
        ]";

        { result.SetResult(BatchExecutor.Execute<BatchTransformItem>(items, item =>
        {
            var (go, error) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (error != null) throw new System.Exception("Object not found");

            WorkflowManager.SnapshotObject(go.transform);
            Undo.RecordObject(go.transform, "Batch Set Transform");

            var rt = go.GetComponent<RectTransform>();
            bool isUI = rt != null;

            if (TryMergeVector3(item.posX, item.posY, item.posZ, go.transform.position, out var newPos))
                go.transform.position = newPos;
            if (TryMergeVector3(item.localPosX, item.localPosY, item.localPosZ, go.transform.localPosition, out var newLocalPos))
                go.transform.localPosition = newLocalPos;
            if (TryMergeVector3(item.rotX, item.rotY, item.rotZ, go.transform.eulerAngles, out var newRot))
                go.transform.eulerAngles = newRot;
            if (TryMergeVector3(item.scaleX, item.scaleY, item.scaleZ, go.transform.localScale, out var newScale))
                go.transform.localScale = newScale;

            if (isUI)
            {
                if (TryMergeVector2(item.anchoredPosX, item.anchoredPosY, rt.anchoredPosition, out var newAnchoredPos))
                    rt.anchoredPosition = newAnchoredPos;
                if (TryMergeVector2(item.anchorMinX, item.anchorMinY, rt.anchorMin, out var newAnchorMin))
                    rt.anchorMin = newAnchorMin;
                if (TryMergeVector2(item.anchorMaxX, item.anchorMaxY, rt.anchorMax, out var newAnchorMax))
                    rt.anchorMax = newAnchorMax;
                if (TryMergeVector2(item.pivotX, item.pivotY, rt.pivot, out var newPivot))
                    rt.pivot = newPivot;
                if (TryMergeVector2(item.sizeDeltaX, item.sizeDeltaY, rt.sizeDelta, out var newSizeDelta))
                    rt.sizeDelta = newSizeDelta;
                if (item.width.HasValue)
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, item.width.Value);
                if (item.height.HasValue)
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, item.height.Value);
            }

            return new
            {
                success = true,
                name = go.name,
                pos = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z }
            };
        }, item => item.name ?? item.path)); return; }
    }
}
```
