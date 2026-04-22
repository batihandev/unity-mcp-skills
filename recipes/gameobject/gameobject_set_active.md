# gameobject_set_active

Enable or disable a GameObject.

**Signature:** `GameObjectSetActive(string name = null, int instanceId = 0, string path = null, bool active = true)`

**Returns:** `{ success, name, active }`

## Notes

- At least one identifier (`name`, `instanceId`, or `path`) is required.
- `active` defaults to `true` (enable). Pass `false` to disable.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;       // provide at least one identifier
        int instanceId = 0;
        string path = null;
        bool active = false;      // true = enable, false = disable

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        WorkflowManager.SnapshotObject(go);
        Undo.RecordObject(go, "Set Active");
        go.SetActive(active);

        { result.SetResult(new { success = true, name = go.name, active }); return; }
    }
}
```
