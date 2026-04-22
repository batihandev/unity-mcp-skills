# cinemachine_create_freelook

Create a CM3 FreeLook rig: `CinemachineCamera` + `CinemachineOrbitalFollow(ThreeRing)` + `CinemachineRotationComposer`.

**Signature:** `CinemachineCreateFreeLook(string name, string followName = null, string lookAtName = null)`

**Returns:** `{ success, gameObjectName, instanceId }` or `{ error }`

**Notes:**
- Auto-adds `CinemachineBrain` to Main Camera if missing.
- `followName` and `lookAtName` can be set later with `cinemachine_set_targets`.
- Tune the three-ring orbit with `cinemachine_configure_body` (`orbitStyle`, `topHeight`/`topRadius`, `midHeight`/`midRadius`, `bottomHeight`/`bottomRadius`).

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
        string name = "My FreeLook";
        string followName = "Player";  // set to null to skip
        string lookAtName = "Player";  // set to null to skip

        var go = new GameObject(name);
        var vcam = go.AddComponent<CinemachineCamera>();
        vcam.Priority.Value = 10;
        var orbital = go.AddComponent<CinemachineOrbitalFollow>();
        orbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing;
        go.AddComponent<CinemachineRotationComposer>();

        var mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<CinemachineBrain>() == null)
            Undo.AddComponent<CinemachineBrain>(mainCamera.gameObject);

        if (!string.IsNullOrEmpty(followName))
        {
            var followGo = GameObjectFinder.Find(followName);
            if (followGo != null) vcam.Follow = followGo.transform;
        }
        if (!string.IsNullOrEmpty(lookAtName))
        {
            var lookAtGo = GameObjectFinder.Find(lookAtName);
            if (lookAtGo != null) vcam.LookAt = lookAtGo.transform;
        }

        Undo.RegisterCreatedObjectUndo(go, "Create FreeLook Camera");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, gameObjectName = go.name, instanceId = go.GetInstanceID() });
    }
}
```
