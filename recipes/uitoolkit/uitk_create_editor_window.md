# uitk_create_editor_window

Generate a Unity EditorWindow C# script that uses UI Toolkit.

**Signature:** `UitkCreateEditorWindow(savePath string, className string, windowTitle string = null, uxmlPath string = null, ussPath string = null, menuPath string = null)`

**Returns:** `{ success, path, className, windowTitle, menuPath }`

**Notes:**
- Fails if the file already exists.
- When `uxmlPath` is provided the window loads the UXML via `CloneTree`; otherwise it builds UI in code.
- When `ussPath` is provided the stylesheet is loaded and added to the root.
- `menuPath` defaults to `"Window/<className>"`.

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string savePath = "Assets/Editor/MyEditorWindow.cs";
        string className = "MyEditorWindow";
        string windowTitle = null;
        string uxmlPath = null;
        string ussPath = null;
        string menuPath = null;

        if (Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (Validate.Required(className, "className") is object classErr) { result.SetResult(classErr); return; }
        if (File.Exists(savePath)) { result.SetResult(new { error = $"File already exists: {savePath}" }); return; }

        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var title = windowTitle ?? className;
        var menu  = menuPath   ?? $"Window/{className}";

        var ussBlock  = string.IsNullOrEmpty(ussPath)  ? "" :
            $"\n        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(\"{ussPath}\");\n        if (styleSheet != null) root.styleSheets.Add(styleSheet);\n";
        var uxmlBlock = string.IsNullOrEmpty(uxmlPath) ?
            $"\n        // Build UI in code\n        root.Add(new Label(\"{title}\"));\n" :
            $"\n        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(\"{uxmlPath}\");\n        if (visualTree != null) visualTree.CloneTree(root);\n";

        var code = $@"using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class {className} : EditorWindow
{{
    [MenuItem(""{menu}"")]
    public static void ShowWindow()
    {{
        var wnd = GetWindow<{className}>();
        wnd.titleContent = new GUIContent(""{title}"");
        wnd.minSize = new Vector2(400, 300);
    }}

    public void CreateGUI()
    {{
        var root = rootVisualElement;
{ussBlock}{uxmlBlock}
        // Query elements and register callbacks
        // var button = root.Q<Button>(""my-button"");
        // button?.RegisterCallback<ClickEvent>(OnButtonClicked);
    }}
}}
";
        File.WriteAllText(savePath, code, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(savePath);

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(savePath);
        if (asset != null) WorkflowManager.SnapshotObject(asset, SnapshotType.Created);

        result.SetResult(new { success = true, path = savePath, className, windowTitle = title, menuPath = menu });
    }
}
```
