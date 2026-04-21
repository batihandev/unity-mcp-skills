# scene_load

Load an existing scene from disk.

**Signature:** `SceneLoad(string scenePath, bool additive = false)`

**Returns:** `{ success, sceneName, scenePath }`

Set `additive = true` to load the scene alongside the current one (multi-scene editing). The default `false` replaces the current scene. Save unsaved work before loading in single mode.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string scenePath = "Assets/Scenes/Level1.unity"; // Project-relative path to the scene asset
        bool additive = false;                            // true = keep current scene; false = replace it

        if (!File.Exists(scenePath))
        {
            result.SetResult(new { error = $"Scene not found: {scenePath}" });
            return;
        }

        var mode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
        var scene = EditorSceneManager.OpenScene(scenePath, mode);

        result.SetResult(new { success = true, sceneName = scene.name, scenePath = scene.path });
    }
}
```
