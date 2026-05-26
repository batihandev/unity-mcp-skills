# Tooling Folder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce `tooling/` as a first-class folder in the skill pack for install-once Editor script templates, separate from `recipes/` (Unity_RunCommand one-shot templates).

**Architecture:** Create `tooling/<domain>/` with a formal file contract (Install → Post-install → Adapt → C# block → Pitfalls). Move `ui_screenshot_tool` from `recipes/ui/` to `tooling/ui/`. Update all cross-references. No validation gate applies to `tooling/`.

**Tech Stack:** Markdown only. No code changes. No commits — user controls commit timing.

**Tracker note:** `tools/tracker_next.py` reads from its own markdown data file, not the filesystem. `tooling/` is automatically excluded from tracker scope. No changes to tracker tools needed.

---

### Task 1: Create tooling/README.md

**Files:**
- Create: `tooling/README.md`

- [ ] **Step 1: Create the file**

```markdown
# Tooling

Install-once Editor script templates for Unity projects.

## How this differs from `recipes/`

| | `recipes/` | `tooling/` |
|--|-----------|------------|
| Execution | `Unity_RunCommand` (one-shot `IRunCommand`) | `Unity_CreateScript` (install into project) |
| Persistence | None — runs and returns | Permanent file in project's Editor folder |
| Validation gate | compile + run required | None |

## Usage

1. Find the tool in the domain subfolder below.
2. Create the C# file at the specified install path via `Unity_CreateScript`.
3. Wait for Unity to recompile.
4. Call the resulting menu items via `EditorApplication.ExecuteMenuItem`.

## Domain Map

| Domain | Folder | Contents |
|--------|--------|----------|
| ui | [`tooling/ui/`](ui/README.md) | UGUI Editor tools |
```

- [ ] **Step 2: Verify**

Confirm `tooling/README.md` exists and the domain map table renders correctly.

---

### Task 2: Create tooling/ui/README.md

**Files:**
- Create: `tooling/ui/README.md`

- [ ] **Step 1: Create the file**

```markdown
# tooling/ui

Install-once Editor scripts for UGUI workflows.

Tool path rule: `../../tooling/ui/<tool>.md`

| Tool | File | Description |
|------|------|-------------|
| `ui_screenshot_tool` | [ui_screenshot_tool.md](ui_screenshot_tool.md) | Capture individual UGUI panels to PNG in Edit Mode |
```

- [ ] **Step 2: Verify**

Confirm `tooling/ui/README.md` exists and the link to `ui_screenshot_tool.md` is correct.

---

### Task 3: Create tooling/ui/ui_screenshot_tool.md

**Files:**
- Create: `tooling/ui/ui_screenshot_tool.md`

- [ ] **Step 1: Create the file with Approach A contract**

````markdown
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

```csharp
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Install at: Assets/<YourProject>/Scripts/Editor/UIScreenshotTools.cs
// After install, invoke via: EditorApplication.ExecuteMenuItem("Tools/UI/Screenshot/<ScreenName>")
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

        // Save canvas state
        var origMode          = canvasComp.renderMode;
        var origWorldCam      = canvasComp.worldCamera;
        var origPlane         = canvasComp.planeDistance;
        var origScaleMode     = scaler != null ? scaler.uiScaleMode        : default;
        var origRefResolution = scaler != null ? scaler.referenceResolution : default;
        bool[] origActive = new bool[uiRoot.childCount];
        for (int i = 0; i < uiRoot.childCount; i++)
            origActive[i] = uiRoot.GetChild(i).gameObject.activeSelf;

        // Isolate target panel
        for (int i = 0; i < uiRoot.childCount; i++)
            uiRoot.GetChild(i).gameObject.SetActive(uiRoot.GetChild(i) == target);

        // Dedicated capture camera
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

        // Switch canvas to Screen Space - Camera
        canvasComp.renderMode    = RenderMode.ScreenSpaceCamera;
        canvasComp.worldCamera   = cam;
        canvasComp.planeDistance = 100f;
        if (scaler != null)
        {
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CaptureW, CaptureH);
        }

        // TMP glyph exceptions here are benign — canvas layout still updates correctly
        try { Canvas.ForceUpdateCanvases(); } catch (System.Exception) { }

        // Render to texture
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

        // Restore canvas state
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

        // Save PNG
        var outPath = $"{OutDir}/{fileName}.png";
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"[UIScreenshot] Saved → {outPath}");
    }
}
```

## Pitfalls

- `GameObject.Find("Canvas")` matches by exact name — update if your canvas is named differently.
- `Find("UIRoot")` is the intermediate node between Canvas and your screen panels — rename to match your hierarchy.
- The script temporarily modifies canvas mode and panel visibility and fully restores both. If Unity crashes mid-capture, undo recovers the scene.
- `Canvas.ForceUpdateCanvases()` may throw TMP glyph atlas exceptions. The `try/catch` is intentional — these do not affect output.
- After install, wait for Unity to compile the new script before calling `ExecuteMenuItem`. Use `EditorApplication.isCompiling` to check.
````

- [ ] **Step 2: Verify**

Confirm `tooling/ui/ui_screenshot_tool.md` exists. Check the five required sections are present in order: Install → Post-install → Adapt → C# block → Pitfalls.

---

### Task 4: Remove ui_screenshot_tool from recipes/

**Files:**
- Delete: `recipes/ui/ui_screenshot_tool.md`
- Modify: `recipes/ui/README.md`

- [ ] **Step 1: Delete `recipes/ui/ui_screenshot_tool.md`**

Remove the file entirely. No compat shim, no redirect comment. Content now lives in `tooling/ui/ui_screenshot_tool.md`.

- [ ] **Step 2: Update `recipes/ui/README.md`**

Remove this row from the table:
```
| `ui_screenshot_tool` | [ui_screenshot_tool.md](ui_screenshot_tool.md) | Editor script: capture UGUI panels to PNG in Edit Mode (UI Authoring Loop Phase 3 prerequisite) |
```

- [ ] **Step 3: Verify**

Confirm `recipes/ui/ui_screenshot_tool.md` no longer exists. Confirm the `recipes/ui/README.md` table no longer contains `ui_screenshot_tool`.

---

### Task 5: Update recipes/README.md

**Files:**
- Modify: `recipes/README.md`

- [ ] **Step 1: Add pointer to tooling/**

Append to the end of `recipes/README.md`:

```markdown
## Editor Script Templates

For install-once Editor scripts (persistent project tooling that creates menu items, custom windows, or other Editor-side tools), see [`tooling/`](../tooling/README.md). These are not `Unity_RunCommand` templates and are not subject to compile/run validation gates.
```

- [ ] **Step 2: Verify**

Confirm the section appears at the end of `recipes/README.md` and the relative link `../tooling/README.md` resolves correctly from `recipes/`.

---

### Task 6: Update skills/ui/SKILL.md Phase 3 path

**Files:**
- Modify: `skills/ui/SKILL.md`

- [ ] **Step 1: Update the prerequisite link**

In `skills/ui/SKILL.md` line 185, replace:

```
see [`recipes/ui/ui_screenshot_tool.md`](../../recipes/ui/ui_screenshot_tool.md)
```

with:

```
see [`tooling/ui/ui_screenshot_tool.md`](../../tooling/ui/ui_screenshot_tool.md)
```

Full updated line:
```
> **Prerequisite:** `Tools/UI/Screenshot/` menu items must exist in the project. If they do not, install the screenshot Editor script first — see [`tooling/ui/ui_screenshot_tool.md`](../../tooling/ui/ui_screenshot_tool.md). Create the script via `Unity_CreateScript`, wait for Unity to recompile, then continue.
```

- [ ] **Step 2: Verify**

Confirm the Phase 3 prerequisite note in `skills/ui/SKILL.md` references `tooling/ui/` and not `recipes/ui/`.

---

### Task 7: Update root SKILL.md

**Files:**
- Modify: `SKILL.md`

- [ ] **Step 1: Add tooling/ to the Other root files section**

`tooling/` is not a domain skill — it belongs in the "Other root files" section (not the Domain Skill Map). Append to the `## Other root files` bullet list:

```
- `tooling/README.md` — install-once Editor script templates (not subject to RunCommand validation); domain subfolders mirror `recipes/`.
```

- [ ] **Step 2: Verify**

Confirm the bullet appears in the "Other root files" section of `SKILL.md` and not in the Domain Skill Map table.

---

## Verification Checklist

After all tasks complete, confirm:

- [ ] `tooling/README.md` exists with domain map
- [ ] `tooling/ui/README.md` exists with tool table
- [ ] `tooling/ui/ui_screenshot_tool.md` exists with all five sections (Install, Post-install, Adapt, C# block, Pitfalls)
- [ ] `recipes/ui/ui_screenshot_tool.md` does not exist
- [ ] `recipes/ui/README.md` has no `ui_screenshot_tool` entry
- [ ] `recipes/README.md` has pointer to `tooling/`
- [ ] `skills/ui/SKILL.md` Phase 3 references `tooling/ui/` not `recipes/ui/`
- [ ] `SKILL.md` "Other root files" lists `tooling/README.md`
- [ ] No broken cross-references (`grep -r "recipes/ui/ui_screenshot_tool" .` returns empty)
