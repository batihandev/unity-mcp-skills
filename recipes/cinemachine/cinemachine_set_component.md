# cinemachine_set_component

Switch the pipeline component for a given stage (Body, Aim, or Noise) on a `CinemachineCamera`. Removes the existing component at that stage and adds the new one.

**Signature:** `CinemachineSetComponent(string vcamName = null, int instanceId = 0, string path = null, string stage = null, string componentType = null)`

**Returns:** `{ success, message }` or `{ error }`

**stage values:** `"Body"`, `"Aim"`, `"Noise"`

**Common componentType values:**
- Body: `"CinemachineFollow"`, `"CinemachineOrbitalFollow"`, `"CinemachineThirdPersonFollow"`, `"CinemachinePositionComposer"`, `"CinemachineSplineDolly"`
- Aim: `"CinemachineRotationComposer"`, `"CinemachinePanTilt"`, `"CinemachineHardLookAt"`
- Noise: `"CinemachineBasicMultiChannelPerlin"`
- Pass `"None"` to remove without adding a replacement.

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
        string stage = "Body";
        string componentType = "CinemachineFollow";  // or "None" to remove

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var vcam = go.GetComponent<CinemachineCamera>();
        if (vcam == null) { result.SetResult(new { error = "Not a CinemachineCamera" }); return; }

        if (!System.Enum.TryParse<CinemachineCore.Stage>(stage, true, out var stageEnum))
        {
            result.SetResult(new { error = "Invalid stage. Use Body, Aim, or Noise." });
            return;
        }

        WorkflowManager.SnapshotObject(go);

        var existing = vcam.GetCinemachineComponent(stageEnum);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        if (!string.IsNullOrEmpty(componentType) && !componentType.Equals("None", System.StringComparison.OrdinalIgnoreCase))
        {
            var normalized = componentType.StartsWith("Cinemachine") ? componentType : "Cinemachine" + componentType;
            var type = typeof(CinemachineCamera).Assembly.GetType("Unity.Cinemachine." + normalized, false, true);
            if (type == null) { result.SetResult(new { error = "Could not find type: " + componentType }); return; }
            var comp = Undo.AddComponent(go, type);
            if (comp == null) { result.SetResult(new { error = "Failed to add component " + type.Name }); return; }
            WorkflowManager.SnapshotCreatedComponent(comp);
        }

        EditorUtility.SetDirty(go);
        result.SetResult(new { success = true, message = "Set " + stage + " to " + (componentType ?? "None") });
    }
}
```
