# uitk_remove_uss_rule

Remove a USS selector rule from a stylesheet.

**Signature:** `UitkRemoveUssRule(filePath string, selector string)`

**Returns:** `{ success, path, removedSelector }`

**Notes:**
- Returns an error if the selector is not found in the file.

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filePath = "Assets/UI/MyStyle.uss";
        string selector = ".my-button";

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (Validate.Required(selector, "selector") is object selErr) { result.SetResult(selErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
        if (existing != null) WorkflowManager.SnapshotObject(existing);

        var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        var normalizedSelector = selector.Trim();

        var pattern = @"\n?" + Regex.Escape(normalizedSelector) + @"\s*\{[^}]*\}\n?";
        var regex = new Regex(pattern, RegexOptions.None, System.TimeSpan.FromSeconds(1));

        if (!regex.IsMatch(content))
        {
            result.SetResult(new { error = $"Selector '{normalizedSelector}' not found in {filePath}" });
            return;
        }

        var newContent = regex.Replace(content, "\n", 1);
        File.WriteAllText(filePath, newContent, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, removedSelector = normalizedSelector });
    }
}
```
