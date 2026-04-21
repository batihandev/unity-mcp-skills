# scene_screenshot

Capture a screenshot of the Game View.

**Signature:** `SceneScreenshot(string filename = "screenshot.png", int width = 1920, int height = 1080)`

**Returns:** `{ success, path, width, height }`

`filename` is a bare filename only — no path separators. The file is always saved under `Assets/Screenshots/`. The directory is created if it does not exist. If `filename` has no extension, `.png` is appended automatically.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filename = "screenshot.png"; // Bare filename only (no path separators); saved under Assets/Screenshots/
        int width = 1920;                   // Desired image width in pixels
        int height = 1080;                  // Desired image height in pixels

        // Strip any path components to prevent writing outside Screenshots/
        filename = Path.GetFileName(filename);
        if (string.IsNullOrEmpty(filename)) filename = "screenshot";
        if (!Path.HasExtension(filename)) filename += ".png";

        var path = Path.Combine(Application.dataPath, "Screenshots", filename);
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        int superSize = Mathf.Max(1, width / Screen.width);
        ScreenCapture.CaptureScreenshot(path, superSize);
        AssetDatabase.Refresh();

        result.SetResult(new { success = true, path, width, height });
    }
}
```
