# cinemachine_add_component

Add a Cinemachine component to a VCam by type name. Prefer `cinemachine_set_component` when you want to replace the existing Body/Aim/Noise pipeline slot; this recipe just adds a component without removing conflicts at the same stage.

**Signature:** `CinemachineAddComponent(string vcamName = null, int instanceId = 0, string path = null, string componentType = null)`

**Returns:** `{ success, message }` or `{ error }`

**Notes:**
- The `"Cinemachine"` prefix is added automatically if the passed `componentType` omits it.
- This command does not remove existing components at the same pipeline stage. Use `cinemachine_set_component` for proper stage management.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

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
        string componentType = "CinemachineBasicMultiChannelPerlin";

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var normalized = componentType;
        if (!string.IsNullOrEmpty(normalized) && !normalized.StartsWith("Cinemachine"))
            normalized = "Cinemachine" + normalized;

        var type = typeof(CinemachineCamera).Assembly.GetType("Unity.Cinemachine." + normalized, false, true);
        if (type == null) { result.SetResult(new { error = "Could not find Cinemachine component type: " + componentType }); return; }

        WorkflowManager.SnapshotObject(go);

        var comp = Undo.AddComponent(go, type);
        if (comp != null)
        {
            WorkflowManager.SnapshotCreatedComponent(comp);
            result.SetResult(new { success = true, message = "Added " + type.Name + " to " + go.name });
        }
        else
        {
            result.SetResult(new { error = "Failed to add component." });
        }
    }
}
```
