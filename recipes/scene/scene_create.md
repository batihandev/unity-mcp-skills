# scene_create

Create a new empty scene asset on disk.

**Signature:** `SceneCreate(string scenePath)`

**Returns:** `{ success, scenePath, sceneName }`

`scenePath` must be a project-relative path ending in `.unity` (e.g. `"Assets/Scenes/Level1.unity"`). The directory is created automatically if it does not exist.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string scenePath = "Assets/Scenes/NewScene.unity"; // Project-relative path for the new scene

        if (Validate.Required(scenePath, "scenePath") is object err) { result.SetResult(err); return; }
        if (Validate.SafePath(scenePath, "scenePath") is object pathErr) { result.SetResult(pathErr); return; }

        var dir = Path.GetDirectoryName(scenePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();

        result.SetResult(new { success = true, scenePath, sceneName = scene.name });
    }
}
```
