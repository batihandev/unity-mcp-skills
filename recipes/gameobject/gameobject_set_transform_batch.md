# gameobject_set_transform_batch

Set transform properties for multiple objects in one call via a typed item array.

**Signature:** `GameObjectSetTransformBatch(BatchTransformItem[] items)`

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, name, pos: { x, y, z } }] }`

## Notes

- Define one `BatchTransformItem` per object to modify. Every transform field is nullable — unset fields leave the current value untouched.
- RectTransform fields (`anchoredPosX/Y`, `anchorMinX/Y`, `anchorMaxX/Y`, `pivotX/Y`, `sizeDeltaX/Y`, `width`, `height`) only apply when the target has a `RectTransform`.
- A missing object causes that item to fail without stopping the rest.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchTransformItem
{
    public string name;
    public int instanceId;
    public string path;
    public float? posX, posY, posZ;
    public float? localPosX, localPosY, localPosZ;
    public float? rotX, rotY, rotZ;
    public float? scaleX, scaleY, scaleZ;
    public float? anchoredPosX, anchoredPosY;
    public float? anchorMinX, anchorMinY;
    public float? anchorMaxX, anchorMaxY;
    public float? pivotX, pivotY;
    public float? sizeDeltaX, sizeDeltaY;
    public float? width, height;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchTransformItem { name = "Cube1", posX = 0, posY = 1, posZ = 0 },
            new _BatchTransformItem { name = "Cube2", posX = 3, rotY = 45, scaleX = 2, scaleY = 2, scaleZ = 2 },
            new _BatchTransformItem { instanceId = 12345, localPosX = 1, localPosY = 0, localPosZ = 0 },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var (go, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (err != null) { results.Add(new { success = false, name = item.name ?? item.path, error = err }); failCount++; continue; }

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

            results.Add(new
            {
                success = true,
                name = go.name,
                pos = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z }
            });
            successCount++;
        }

        result.SetResult(new { success = true, totalItems = items.Length, successCount, failCount, results });
    }

    private static bool TryMergeVector3(float? x, float? y, float? z, Vector3 current, out Vector3 result)
    {
        if (!x.HasValue && !y.HasValue && !z.HasValue) { result = current; return false; }
        result = new Vector3(x ?? current.x, y ?? current.y, z ?? current.z);
        return true;
    }

    private static bool TryMergeVector2(float? x, float? y, Vector2 current, out Vector2 result)
    {
        if (!x.HasValue && !y.HasValue) { result = current; return false; }
        result = new Vector2(x ?? current.x, y ?? current.y);
        return true;
    }
}
```
