# cinemachine_remove_extension

Remove a `CinemachineExtension` component from a VCam.

**Signature:** `CinemachineRemoveExtension(string vcamName = null, int instanceId = 0, string path = null, string extensionName = null)`

**Returns:** `{ success, message }` or `{ error }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string vcamName = "My VCam";
        int instanceId = 0;
        string path = null;
        string extensionName = "CinemachineImpulseListener";

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var normalized = extensionName;
        if (!string.IsNullOrEmpty(normalized) && !normalized.StartsWith("Cinemachine"))
            normalized = "Cinemachine" + normalized;
        var type = typeof(CinemachineCamera).Assembly.GetType("Unity.Cinemachine." + normalized, false, true);
        if (type == null) { result.SetResult(new { error = "Could not find Cinemachine extension type: " + extensionName }); return; }

        var ext = go.GetComponent(type);
        if (ext == null) { result.SetResult(new { error = "Extension " + type.Name + " not found on " + go.name }); return; }

        WorkflowManager.SnapshotObject(go);
        Undo.DestroyObjectImmediate(ext);

        result.SetResult(new { success = true, message = "Removed extension " + type.Name });
    }
}
```
