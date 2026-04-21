# scene_unload

Unload an additively loaded scene.

**Signature:** `SceneUnload(string sceneName)`

**Returns:** `{ success, unloaded }` or `{ success: false, error }`

`sceneName` can be the scene name (without `.unity`) or a path suffix. The scene is auto-saved if it has unsaved changes before being unloaded. Cannot unload the only loaded scene.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string sceneName = "Level1"; // Scene name or path suffix (e.g. "Level1" matches "Assets/Scenes/Level1.unity")

        Scene sceneToUnload = default;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName || scene.path.EndsWith(sceneName + ".unity"))
            {
                sceneToUnload = scene;
                break;
            }
        }

        if (!sceneToUnload.IsValid())
        {
            result.SetResult(new { success = false, error = $"Scene '{sceneName}' not found in loaded scenes" });
            return;
        }

        if (SceneManager.sceneCount <= 1)
        {
            result.SetResult(new { success = false, error = "Cannot unload the only loaded scene" });
            return;
        }

        if (sceneToUnload.isDirty)
            EditorSceneManager.SaveScene(sceneToUnload);

        EditorSceneManager.CloseScene(sceneToUnload, true);
        result.SetResult(new { success = true, unloaded = sceneName });
    }
}
```
