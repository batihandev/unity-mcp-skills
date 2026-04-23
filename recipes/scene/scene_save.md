# scene_save

Save the active scene.

**Signature:** `SceneSave(string scenePath = null)`

**Returns:** `{ success, scenePath }`

Omit `scenePath` to save to the scene's existing path. Provide a path to save-as (e.g. when saving a new unsaved scene for the first time).

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string scenePath = null; // null = save to current path; provide a path to save-as

        if (!string.IsNullOrEmpty(scenePath) && Validate.SafePath(scenePath, "scenePath") is object pathErr)
        {
            result.SetResult(pathErr);
            return;
        }

        var scene = SceneManager.GetActiveScene();
        var path = scenePath ?? scene.path;

        if (string.IsNullOrEmpty(path))
        {
            result.SetResult(new { error = "Scene has no path. Provide scenePath parameter." });
            return;
        }

        EditorSceneManager.SaveScene(scene, path);
        result.SetResult(new { success = true, scenePath = path });
    }
}
```
