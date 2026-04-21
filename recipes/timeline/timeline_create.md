# timeline_create

Create a new Timeline asset and Director instance.

**Signature:** `TimelineCreate(name string, folder string = "Assets/Timelines")`

**Returns:** `{ success, assetPath, gameObjectName, directorInstanceId }`

**Notes:**
- `name` must not contain path separators (`/`, `\`, `..`)
- The folder is created if it does not exist
- The asset path is made unique via `AssetDatabase.GenerateUniqueAssetPath`

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyTimeline";
        string folder = "Assets/Timelines";

        if (Validate.Required(name, "name") is object nameErr) { result.SetResult(nameErr); return; }
        if (name.Contains("/") || name.Contains("\\") || name.Contains(".."))
        {
            result.SetResult(new { error = "name must not contain path separators" });
            return;
        }
        if (Validate.SafePath(folder, "folder") is object folderErr) { result.SetResult(folderErr); return; }

        if (!System.IO.Directory.Exists(folder))
            System.IO.Directory.CreateDirectory(folder);

        string assetPath = System.IO.Path.Combine(folder, name + ".playable");
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        var timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timelineAsset, assetPath);

        var go = new GameObject(name);
        var director = go.AddComponent<PlayableDirector>();
        director.playableAsset = timelineAsset;

        AssetDatabase.SaveAssets();

        result.SetResult(new
        {
            success = true,
            assetPath,
            gameObjectName = go.name,
            directorInstanceId = director.GetInstanceID()
        });
    }
}
```
