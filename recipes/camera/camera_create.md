# camera_create

Create a new Game Camera GameObject in the scene. Optionally adds an AudioListener component.

**Signature:** `CameraCreate(string name = "New Camera", float x = 0, float y = 1, float z = -10, bool addAudioListener = false)`

**Returns:** `{ success, name, instanceId }`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Main Camera";
        float x = 0f;
        float y = 1f;
        float z = -10f;
        bool addAudioListener = false;

        var go = new GameObject(name);
        go.AddComponent<Camera>();
        if (addAudioListener) go.AddComponent<AudioListener>();
        go.transform.position = new Vector3(x, y, z);
        Undo.RegisterCreatedObjectUndo(go, "Create Camera");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);
        result.SetResult(new { success = true, name = go.name, instanceId = go.GetInstanceID() });
    }
}
```
