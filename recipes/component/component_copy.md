# component_copy

Copy a component from one GameObject to another using Unity's built-in ComponentUtility (equivalent to right-click "Copy Component" / "Paste Component as New" in the editor).

**Signature:** `ComponentCopy(string sourceName = null, int sourceInstanceId = 0, string sourcePath = null, string targetName = null, int targetInstanceId = 0, string targetPath = null, string componentType = null)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `sourceName` | string | No* | null | Source GameObject name |
| `sourceInstanceId` | int | No* | 0 | Source Instance ID |
| `sourcePath` | string | No* | null | Source hierarchy path |
| `targetName` | string | No* | null | Target GameObject name |
| `targetInstanceId` | int | No* | 0 | Target Instance ID |
| `targetPath` | string | No* | null | Target hierarchy path |
| `componentType` | string | Yes | - | Component type to copy |

*At least one source identifier and one target identifier required.

## Returns

```json
{
  "success": true,
  "source": "Player",
  "target": "Enemy",
  "componentType": "AudioSource"
}
```

## Notes

- Uses `UnityEditorInternal.ComponentUtility.CopyComponent` and `PasteComponentAsNew` — this is the exact same path as the editor menu action.
- Copies all serialized property values from the source component to a new component instance on the target.
- If the source object does not have the specified component, the call returns an error.
- Workflow tracking is enabled; the pasted component is recorded.

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        /* Original Logic:

            if (Validate.Required(componentType, "componentType") is object err) return err;
            var (srcGo, srcErr) = GameObjectFinder.FindOrError(name: sourceName, instanceId: sourceInstanceId, path: sourcePath);
            if (srcErr != null) return srcErr;
            var (dstGo, dstErr) = GameObjectFinder.FindOrError(name: targetName, instanceId: targetInstanceId, path: targetPath);
            if (dstErr != null) return dstErr;

            var type = FindComponentType(componentType);
            if (type == null) return new { error = $"Component type not found: {componentType}" };

            var srcComp = srcGo.GetComponent(type);
            if (srcComp == null) return new { error = $"No {componentType} on {sourceName}" };

            UnityEditorInternal.ComponentUtility.CopyComponent(srcComp);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(dstGo);
            return new { success = true, source = sourceName, target = targetName, componentType };
        */
    }
}
```
