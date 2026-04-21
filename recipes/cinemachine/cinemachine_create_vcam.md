# cinemachine_create_vcam

Create a new Virtual Camera (CM2: `CinemachineVirtualCamera`, CM3: `CinemachineCamera`). Auto-adds `CinemachineBrain` to Main Camera if missing.

**Signature:** `CinemachineCreateVCam(string name, string folder = "Assets/Settings")`

**Returns:** `{ success, gameObjectName, instanceId }`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "My VCam";
        // folder param is accepted but not used for scene placement

        var go = new GameObject(name);
        var vcam = go.AddComponent(CinemachineAdapter.FindCinemachineType(CinemachineAdapter.VCamTypeName)) as MonoBehaviour;
        CinemachineAdapter.SetPriority(vcam, 10);

        Undo.RegisterCreatedObjectUndo(go, "Create Virtual Camera");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            var brain = mainCamera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                var brainComp = Undo.AddComponent<CinemachineBrain>(mainCamera.gameObject);
                WorkflowManager.SnapshotCreatedComponent(brainComp);
            }
        }

        result.SetResult(new { success = true, gameObjectName = go.name, instanceId = go.GetInstanceID() });
    }
}
```
