# gameobject_set_transform

Set position, rotation, scale, and/or RectTransform properties. All transform parameters are optional — only the fields you provide are applied.

**Signature:**
```
GameObjectSetTransform(
    string name = null, int instanceId = 0, string path = null,
    float? posX = null, float? posY = null, float? posZ = null,
    float? rotX = null, float? rotY = null, float? rotZ = null,
    float? scaleX = null, float? scaleY = null, float? scaleZ = null,
    float? localPosX = null, float? localPosY = null, float? localPosZ = null,
    float? anchoredPosX = null, float? anchoredPosY = null,
    float? anchorMinX = null, float? anchorMinY = null,
    float? anchorMaxX = null, float? anchorMaxY = null,
    float? pivotX = null, float? pivotY = null,
    float? sizeDeltaX = null, float? sizeDeltaY = null,
    float? width = null, float? height = null
)
```

**Returns (3D):**
```
{ success, name, instanceId, isUI: false, position: {x,y,z}, localPosition: {x,y,z}, rotation: {x,y,z}, scale: {x,y,z} }
```

**Returns (UI / RectTransform):**
```
{ success, name, instanceId, isUI: true, anchoredPosition: {x,y}, anchorMin: {x,y}, anchorMax: {x,y}, pivot: {x,y}, sizeDelta: {x,y}, rect: {width,height}, localPosition: {x,y,z} }
```

## Parameter Groups

| Group | Params | Scope |
|-------|--------|-------|
| World position | `posX`, `posY`, `posZ` | 3D objects |
| Local position | `localPosX`, `localPosY`, `localPosZ` | 3D and UI |
| Rotation (euler) | `rotX`, `rotY`, `rotZ` | 3D objects |
| Scale | `scaleX`, `scaleY`, `scaleZ` | 3D objects |
| Anchored position | `anchoredPosX`, `anchoredPosY` | UI (RectTransform) |
| Anchor min | `anchorMinX`, `anchorMinY` | UI (RectTransform) |
| Anchor max | `anchorMaxX`, `anchorMaxY` | UI (RectTransform) |
| Pivot | `pivotX`, `pivotY` | UI (RectTransform) |
| Size delta | `sizeDeltaX`, `sizeDeltaY` | UI (RectTransform) |
| Width / Height | `width`, `height` | UI shortcut via `SetSizeWithCurrentAnchors` |

## Notes

- Only supplied fields are written; unset fields keep their current values.
- `posX/Y/Z` sets world-space position; `localPosX/Y/Z` sets local-space position.
- The skill auto-detects whether the object has a `RectTransform` and applies UI params accordingly.
- UI params (`anchoredPos*`, `anchor*`, `pivot*`, `sizeDelta*`, `width`, `height`) are silently ignored for non-UI objects.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyObject";
        int instanceId = 0;
        string path = null;
        float? posX = 5f, posY = 0f, posZ = 3f;
        float? localPosX = null, localPosY = null, localPosZ = null;
        float? rotX = null, rotY = 45f, rotZ = null;
        float? scaleX = null, scaleY = null, scaleZ = null;
        float? anchoredPosX = null, anchoredPosY = null;
        float? anchorMinX = null, anchorMinY = null;
        float? anchorMaxX = null, anchorMaxY = null;
        float? pivotX = null, pivotY = null;
        float? sizeDeltaX = null, sizeDeltaY = null;
        float? width = null, height = null;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        WorkflowManager.SnapshotObject(go.transform);
        Undo.RecordObject(go.transform, "Set Transform");

        var rt = go.GetComponent<RectTransform>();
        bool isUI = rt != null;

        if (TryMergeVector3(posX, posY, posZ, go.transform.position, out var newPos))
            go.transform.position = newPos;
        if (TryMergeVector3(localPosX, localPosY, localPosZ, go.transform.localPosition, out var newLocalPos))
            go.transform.localPosition = newLocalPos;
        if (TryMergeVector3(rotX, rotY, rotZ, go.transform.eulerAngles, out var newRot))
            go.transform.eulerAngles = newRot;
        if (TryMergeVector3(scaleX, scaleY, scaleZ, go.transform.localScale, out var newScale))
            go.transform.localScale = newScale;

        // RectTransform specific properties
        if (isUI)
        {
            if (TryMergeVector2(anchoredPosX, anchoredPosY, rt.anchoredPosition, out var newAnchoredPos))
                rt.anchoredPosition = newAnchoredPos;
            if (TryMergeVector2(anchorMinX, anchorMinY, rt.anchorMin, out var newAnchorMin))
                rt.anchorMin = newAnchorMin;
            if (TryMergeVector2(anchorMaxX, anchorMaxY, rt.anchorMax, out var newAnchorMax))
                rt.anchorMax = newAnchorMax;
            if (TryMergeVector2(pivotX, pivotY, rt.pivot, out var newPivot))
                rt.pivot = newPivot;
            if (TryMergeVector2(sizeDeltaX, sizeDeltaY, rt.sizeDelta, out var newSizeDelta))
                rt.sizeDelta = newSizeDelta;

            // Width/Height shortcuts
            if (width.HasValue || height.HasValue)
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width ?? rt.rect.width);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height ?? rt.rect.height);
            }

            EditorUtility.SetDirty(rt);

            { result.SetResult(new
            {
                success = true,
                name = go.name,
                instanceId = go.GetInstanceID(),
                isUI = true,
                anchoredPosition = new { x = rt.anchoredPosition.x, y = rt.anchoredPosition.y },
                anchorMin = new { x = rt.anchorMin.x, y = rt.anchorMin.y },
                anchorMax = new { x = rt.anchorMax.x, y = rt.anchorMax.y },
                pivot = new { x = rt.pivot.x, y = rt.pivot.y },
                sizeDelta = new { x = rt.sizeDelta.x, y = rt.sizeDelta.y },
                rect = new { width = rt.rect.width, height = rt.rect.height },
                localPosition = new { x = go.transform.localPosition.x, y = go.transform.localPosition.y, z = go.transform.localPosition.z }
            }); return; }
        }

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            isUI = false,
            position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z },
            localPosition = new { x = go.transform.localPosition.x, y = go.transform.localPosition.y, z = go.transform.localPosition.z },
            rotation = new { x = go.transform.eulerAngles.x, y = go.transform.eulerAngles.y, z = go.transform.eulerAngles.z },
            scale = new { x = go.transform.localScale.x, y = go.transform.localScale.y, z = go.transform.localScale.z }
        }); return; }
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
