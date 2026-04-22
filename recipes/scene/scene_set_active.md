# scene_set_active

Set the active scene for multi-scene editing.

**Signature:** `SceneSetActive(string sceneName)`

**Returns:** `{ success, activeScene }` or `{ success: false, error }`

`sceneName` can be the scene name or a path suffix. The scene must already be loaded. New GameObjects created at runtime are placed into the active scene.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string sceneName = "Level1"; // Scene name or path suffix of an already-loaded scene

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName || scene.path.EndsWith(sceneName + ".unity"))
            {
                if (!scene.isLoaded)
                {
                    result.SetResult(new { success = false, error = $"Scene '{sceneName}' is not loaded" });
                    return;
                }

                SceneManager.SetActiveScene(scene);
                result.SetResult(new { success = true, activeScene = scene.name });
                return;
            }
        }

        result.SetResult(new { success = false, error = $"Scene '{sceneName}' not found in loaded scenes" });
    }
}
```
