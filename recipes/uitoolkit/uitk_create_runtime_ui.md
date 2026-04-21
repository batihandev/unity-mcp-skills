# uitk_create_runtime_ui

Generate a runtime MonoBehaviour C# script that queries UI Toolkit elements from a UIDocument.

**Signature:** `UitkCreateRuntimeUi(savePath string, className string, elementQueries string = null)`

**Returns:** `{ success, path, className }`

**Notes:**
- Fails if the file already exists.
- `elementQueries` is a comma-separated list of `Type:name` pairs, e.g. `"Button:play-btn,Label:score-label"`.
- The generated class is decorated with `[RequireComponent(typeof(UIDocument))]`.
- Callback registration stubs are included but commented out as reminders.

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string savePath = "Assets/Scripts/UI/MyRuntimeUI.cs";
        string className = "MyRuntimeUI";
        string elementQueries = "Button:play-btn,Label:score-label";

        if (Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (Validate.Required(className, "className") is object classErr) { result.SetResult(classErr); return; }
        if (File.Exists(savePath)) { result.SetResult(new { error = $"File already exists: {savePath}" }); return; }

        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var queryLines = new StringBuilder();
        var fields = new StringBuilder();
        if (!string.IsNullOrEmpty(elementQueries))
        {
            foreach (var q in elementQueries.Split(','))
            {
                var parts = q.Trim().Split(':');
                if (parts.Length != 2) continue;
                var elType  = parts[0].Trim();
                var elName  = parts[1].Trim();
                var fieldName = "m_" + elName.Replace("-", "").Replace("_", "");
                fields.AppendLine($"    private {elType} {fieldName};");
                queryLines.AppendLine($"        {fieldName} = root.Q<{elType}>(\"{elName}\");");
            }
        }

        var code = $@"using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class {className} : MonoBehaviour
{{
{fields}
    private void OnEnable()
    {{
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

{queryLines}
        // Register callbacks
        // m_playBtn?.RegisterCallback<ClickEvent>(OnPlayClicked);
    }}

    private void OnDisable()
    {{
        // Unregister callbacks to prevent memory leaks
        // m_playBtn?.UnregisterCallback<ClickEvent>(OnPlayClicked);
    }}
}}
";
        File.WriteAllText(savePath, code, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(savePath);

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(savePath);
        if (asset != null) WorkflowManager.SnapshotObject(asset, SnapshotType.Created);

        result.SetResult(new { success = true, path = savePath, className });
    }
}
```
