# cinemachine_create_freelook

Create a FreeLook camera. CM2 creates `CinemachineFreeLook`; CM3 creates `CinemachineCamera` + `OrbitalFollow(ThreeRing)` + `RotationComposer`.

**Signature:** `CinemachineCreateFreeLook(string name, string followName = null, string lookAtName = null)`

**Returns:** `{ success, gameObjectName, instanceId }` or `{ error }`

**Notes:**
- Auto-adds `CinemachineBrain` to Main Camera if missing.
- `followName` and `lookAtName` can be set later with `cinemachine_set_targets`.
- In CM3, tune the three-ring orbit with `cinemachine_configure_body` (`orbitStyle`, `topHeight`/`topRadius`, `midHeight`/`midRadius`, `bottomHeight`/`bottomRadius`).

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "My FreeLook";
        string followName = "Player";  // set to null to skip
        string lookAtName = "Player";  // set to null to skip

        var go = CinemachineAdapter.CreateFreeLook(name);

        var mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<CinemachineBrain>() == null)
            Undo.AddComponent<CinemachineBrain>(mainCamera.gameObject);

        var vcam = CinemachineAdapter.GetVCam(go);
        if (vcam != null)
        {
            if (!string.IsNullOrEmpty(followName))
            {
                var followGo = GameObjectFinder.Find(followName);
                if (followGo != null) CinemachineAdapter.SetFollow(vcam, followGo.transform);
            }
            if (!string.IsNullOrEmpty(lookAtName))
            {
                var lookAtGo = GameObjectFinder.Find(lookAtName);
                if (lookAtGo != null) CinemachineAdapter.SetLookAt(vcam, lookAtGo.transform);
            }
        }

#if CINEMACHINE_2
        // CM2 FreeLook has its own Follow/LookAt fields
        var freeLook = go.GetComponent<CinemachineFreeLook>();
        if (freeLook != null)
        {
            if (!string.IsNullOrEmpty(followName))
            {
                var followGo = GameObjectFinder.Find(followName);
                if (followGo != null) freeLook.m_Follow = followGo.transform;
            }
            if (!string.IsNullOrEmpty(lookAtName))
            {
                var lookAtGo = GameObjectFinder.Find(lookAtName);
                if (lookAtGo != null) freeLook.m_LookAt = lookAtGo.transform;
            }
        }
#endif

        Undo.RegisterCreatedObjectUndo(go, "Create FreeLook Camera");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, gameObjectName = go.name, instanceId = go.GetInstanceID() });
    }
}
```
