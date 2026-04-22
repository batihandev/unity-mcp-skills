# scene_get_info

Get metadata about the currently active scene.

**Signature:** `SceneGetInfo()`

**Returns:** `{ sceneName, scenePath, isDirty, rootObjectCount, rootObjects: [{ name, instanceId, childCount }] }`

No parameters. Use this to verify which scene is loaded and whether it has unsaved changes before performing destructive operations.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();

        result.SetResult(new
        {
            sceneName = scene.name,
            scenePath = scene.path,
            isDirty = scene.isDirty,
            rootObjectCount = roots.Length,
            rootObjects = roots.Select(go => new
            {
                name = go.name,
                instanceId = go.GetInstanceID(),
                childCount = go.transform.childCount
            }).ToArray()
        });
    }
}
```
