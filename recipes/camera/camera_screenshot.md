# camera_screenshot

Capture a PNG screenshot from a Game Camera to a file. For quick Scene View captures, prefer the native `Unity_Camera_Capture` tool. Use this command when you need a specific Game Camera, custom resolution, or a path under `Assets/`.

**Signature:** `CameraScreenshot(string savePath = "Assets/screenshot.png", int width = 1920, int height = 1080, string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, path, width, height }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string savePath = "Assets/Screenshots/capture.png";
        int width = 1920;
        int height = 1080;

        // Provide at least one of: name, instanceId, or path
        string name = "Main Camera";
        int instanceId = 0;
        string path = null;

        var (cam, err) = GameObjectFinder.FindComponentOrError<Camera>(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        if (Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (!savePath.EndsWith(".png")) savePath += ".png";

        var dir = System.IO.Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        var rt = new RenderTexture(width, height, 24);
        Texture2D tex = null;
        RenderTexture oldTarget = cam.targetTexture;
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            System.IO.File.WriteAllBytes(savePath, tex.EncodeToPNG());
        }
        finally
        {
            cam.targetTexture = oldTarget;
            RenderTexture.active = null;
            if (rt != null) Object.DestroyImmediate(rt);
            if (tex != null) Object.DestroyImmediate(tex);
        }

        AssetDatabase.ImportAsset(savePath);
        result.SetResult(new { success = true, path = savePath, width, height });
    }
}
```
