# ui_screenshot_tool

Captures individual UGUI panels in Edit Mode by temporarily switching the Canvas to Screen Space - Camera, rendering the target panel in isolation, and saving a PNG. No Play Mode required.

## Install

**Path:** `Assets/<YourProject>/Scripts/Editor/UIScreenshotTools.cs`

Install via `Unity_CreateScript` with the C# content below.

## Post-install

Menus available after compile:
- `Tools/UI/Screenshot/<ScreenName>` — capture a single named panel
- `Tools/UI/Screenshot/All Screens` — capture all panels in sequence

Wait for Unity to recompile before calling via `ExecuteMenuItem`. Poll `EditorApplication.isCompiling` first (see UI Authoring Loop Phase 1 in `skills/ui/SKILL.md`).

## Adapt

| What to change | Where |
|----------------|-------|
| Panel GameObject names | First arg of each `CaptureScreen("YourPanelName", ...)` call |
| Output file names | Second arg of each `CaptureScreen(...)` call |
| Canvas reference resolution | `CaptureW` / `CaptureH` constants |
| Canvas root node name | `canvas.transform.Find("UIRoot")` |
| Background colour | `cam.backgroundColor = Color.black` |
| Output directory | `OutDir` constant |
| Namespace | `namespace YourProject.Editor` at top of file |

```csharp
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace YourProject.Editor
{
    public static class UIScreenshotTools
    {
        private const string OutDir = "Assets/Screenshots/UI";
        private const int CaptureW = 390; // match your Canvas reference resolution width
        private const int CaptureH = 844; // match your Canvas reference resolution height

        // Add one [MenuItem] per screen panel. Adjust names to match your project's panel GameObjects.
        [MenuItem("Tools/UI/Screenshot/MainMenu")]
        public static void ScreenshotMainMenu() => CaptureScreen("YourPanelName", "ui_main_menu");

        [MenuItem("Tools/UI/Screenshot/All Screens")]
        public static void ScreenshotAll()
        {
            CaptureScreen("PanelA", "ui_panel_a");
            CaptureScreen("PanelB", "ui_panel_b");
            // add one line per panel
        }

        private static void CaptureScreen(string panelName, string fileName)
        {
            if (!Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[UIScreenshot] Canvas not found"); return; }

            var canvasComp = canvas.GetComponent<Canvas>();
            var scaler     = canvas.GetComponent<CanvasScaler>();
            var uiRoot     = canvas.transform.Find("UIRoot"); // change if your panel root has a different name
            if (uiRoot == null) { Debug.LogError("[UIScreenshot] UIRoot not found"); return; }

            var target = uiRoot.Find(panelName);
            if (target == null) { Debug.LogError($"[UIScreenshot] '{panelName}' not found"); return; }

            var origMode          = canvasComp.renderMode;
            var origWorldCam      = canvasComp.worldCamera;
            var origPlane         = canvasComp.planeDistance;
            var origScaleMode     = scaler != null ? scaler.uiScaleMode        : default;
            var origRefResolution = scaler != null ? scaler.referenceResolution : default;
            bool[] origActive = new bool[uiRoot.childCount];
            for (int i = 0; i < uiRoot.childCount; i++)
                origActive[i] = uiRoot.GetChild(i).gameObject.activeSelf;

            for (int i = 0; i < uiRoot.childCount; i++)
                uiRoot.GetChild(i).gameObject.SetActive(uiRoot.GetChild(i) == target);

            var camGo = new GameObject("__UICaptureCam");
            var cam   = camGo.AddComponent<Camera>();
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = Color.black; // set to your project's background colour
            cam.cullingMask      = -1;
            cam.orthographic     = true;
            cam.orthographicSize = CaptureH * 0.5f;
            cam.nearClipPlane    = 0.1f;
            cam.farClipPlane     = 1000f;
            cam.transform.position = new Vector3(0, 0, -500f);

            canvasComp.renderMode    = RenderMode.ScreenSpaceCamera;
            canvasComp.worldCamera   = cam;
            canvasComp.planeDistance = 100f;
            if (scaler != null)
            {
                scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(CaptureW, CaptureH);
            }

            try { Canvas.ForceUpdateCanvases(); } catch (System.Exception) { }

            var rt = new RenderTexture(CaptureW, CaptureH, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;

            RenderTexture.active = rt;
            var tex = new Texture2D(CaptureW, CaptureH, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, CaptureW, CaptureH), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo);

            canvasComp.renderMode    = origMode;
            canvasComp.worldCamera   = origWorldCam;
            canvasComp.planeDistance = origPlane;
            if (scaler != null)
            {
                scaler.uiScaleMode         = origScaleMode;
                scaler.referenceResolution = origRefResolution;
            }
            for (int i = 0; i < uiRoot.childCount; i++)
                uiRoot.GetChild(i).gameObject.SetActive(origActive[i]);

            var outPath = $"{OutDir}/{fileName}.png";
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[UIScreenshot] Saved → {outPath}");
        }
    }
}
```

## Pitfalls

- `GameObject.Find("Canvas")` matches by exact name — update if your canvas is named differently.
- `Find("UIRoot")` is the intermediate node between Canvas and your screen panels — rename to match your hierarchy.
- The script temporarily modifies canvas mode and panel visibility and fully restores both. If Unity crashes mid-capture, changes are not registered with Undo — reload the scene from disk to recover.
- `Canvas.ForceUpdateCanvases()` may throw TMP glyph atlas exceptions. The `try/catch` is intentional — these do not affect output.
- `AssetDatabase.ImportAsset` may trigger a Sprite Editor warning ("rect lies outside of texture") if the project's default texture type is Sprite. This is cosmetic — the PNG is saved correctly. Ignore the warning.
- After install, wait for Unity to compile the new script before calling `ExecuteMenuItem`. Use `EditorApplication.isCompiling` to check.
