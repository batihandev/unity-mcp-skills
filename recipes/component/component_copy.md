# component_copy

Copy a component from one GameObject to another using Unity's built-in ComponentUtility (equivalent to right-click "Copy Component" / "Paste Component as New" in the editor).

**Signature:** `ComponentCopy(string sourceName = null, int sourceInstanceId = 0, string sourcePath = null, string targetName = null, int targetInstanceId = 0, string targetPath = null, string componentType = null)`

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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`component_type_finder`](../_shared/component_type_finder.md), [`skills_common`](../_shared/skills_common.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string componentType = "AudioSource";
        string sourceName = null; int sourceInstanceId = 0; string sourcePath = null;
        string targetName = null; int targetInstanceId = 0; string targetPath = null;

        if (Validate.Required(componentType, "componentType") is object err) { result.SetResult(err); return; }
        var (srcGo, srcErr) = GameObjectFinder.FindOrError(name: sourceName, instanceId: sourceInstanceId, path: sourcePath);
        if (srcErr != null) { result.SetResult(srcErr); return; }
        var (dstGo, dstErr) = GameObjectFinder.FindOrError(name: targetName, instanceId: targetInstanceId, path: targetPath);
        if (dstErr != null) { result.SetResult(dstErr); return; }

        var type = ComponentSkills.FindComponentType(componentType);
        if (type == null) { result.SetResult(new { error = $"Component type not found: {componentType}" }); return; }

        var srcComp = srcGo.GetComponent(type);
        if (srcComp == null) { result.SetResult(new { error = $"No {componentType} on {srcGo.name}" }); return; }

        UnityEditorInternal.ComponentUtility.CopyComponent(srcComp);
        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(dstGo);
        result.SetResult(new { success = true, source = srcGo.name, target = dstGo.name, componentType });
    }
}
```
