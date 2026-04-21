# cinemachine_set_component

Switch the pipeline component for a given stage (Body, Aim, or Noise) on a VCam. Removes the existing component and adds the new one. **CM3 only** — for CM2 use `cinemachine_add_component`.

**Signature:** `CinemachineSetComponent(string vcamName = null, int instanceId = 0, string path = null, string stage = null, string componentType = null)`

**Returns:** `{ success, message }` or `{ error }`

**stage values:** `"Body"`, `"Aim"`, `"Noise"`

**Common componentType values:**
- Body: `"CinemachineFollow"`, `"CinemachineOrbitalFollow"`, `"CinemachineThirdPersonFollow"`, `"CinemachinePositionComposer"`, `"CinemachineSplineDolly"`
- Aim: `"CinemachineRotationComposer"`, `"CinemachinePanTilt"`, `"CinemachineHardLookAt"`
- Noise: `"CinemachineBasicMultiChannelPerlin"`
- Pass `"None"` to remove without adding a replacement.

```csharp
using UnityEngine;
using UnityEditor;

#if CINEMACHINE_3
using Unity.Cinemachine;
#endif

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string vcamName = "My VCam";
        int instanceId = 0;
        string path = null;
        string stage = "Body";
        string componentType = "CinemachineFollow";  // or "None" to remove

#if CINEMACHINE_3
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
            var type = CinemachineAdapter.FindCinemachineType(componentType);
            if (type == null) { result.SetResult(new { error = "Could not find type: " + componentType }); return; }
            var comp = Undo.AddComponent(go, type);
            if (comp == null) { result.SetResult(new { error = "Failed to add component " + type.Name }); return; }
            WorkflowManager.SnapshotCreatedComponent(comp);
        }

        EditorUtility.SetDirty(go);
        result.SetResult(new { success = true, message = "Set " + stage + " to " + (componentType ?? "None") });
#else
        result.SetResult(new { error = "cinemachine_set_component requires Cinemachine 3.x. For CM2, use cinemachine_add_component." });
#endif
    }
}
```
