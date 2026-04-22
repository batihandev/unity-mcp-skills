# xr_setup_teleportation

Adds TeleportationProvider to the XR Origin. Auto-finds XR Origin if no target is specified.

**Signature:** `XRSetupTeleportation(name string = null, instanceId int = 0, path string = null)`

**Returns:** `{ success, name, instanceId, providerType, note }`

**Notes:**
- Omit all selector parameters to auto-target the XR Origin in the scene.
- After this call, create teleport destinations with `xr_add_teleport_area` or `xr_add_teleport_anchor`.
- Requires an XR Origin to already exist; create one with `xr_setup_rig` first.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;

        GameObject go;
        if (string.IsNullOrEmpty(name) && instanceId == 0 && string.IsNullOrEmpty(path))
        {
            var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (origin == null)
                { result.SetResult(new { error = "No XR Origin found in scene. Create one via xr_setup_rig, or specify the target object." }); return; }
            go = origin.gameObject;
        }
        else
        {
            var (found, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (findErr != null) { result.SetResult(findErr); return; }
            go = found;
        }

        Undo.RecordObject(go, "Setup Teleportation");

        var existing = go.GetComponent<TeleportationProvider>();
        var comp = existing != null ? existing : go.AddComponent<TeleportationProvider>();
        if (existing == null)
            Undo.RegisterCreatedObjectUndo(comp, "Add TeleportationProvider");

        WorkflowManager.SnapshotObject(go);

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            providerType = comp.GetType().Name,
            note = "Now create teleport targets via xr_add_teleport_area or xr_add_teleport_anchor."
        }); return; }
    }
}
```
