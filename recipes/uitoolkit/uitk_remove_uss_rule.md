# uitk_remove_uss_rule

Remove a USS selector rule from a stylesheet.

**Signature:** `UitkRemoveUssRule(filePath string, selector string)`

**Returns:** `{ success, path, removedSelector }`

**Notes:**
- Returns an error if the selector is not found in the file.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

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

        // Find selector followed by optional whitespace then {
        int ruleStart = -1, ruleEnd = -1;
        int searchFrom = 0;
        while (searchFrom < content.Length)
        {
            int found = content.IndexOf(normalizedSelector, searchFrom, System.StringComparison.Ordinal);
            if (found < 0) break;
            int check = found + normalizedSelector.Length;
            while (check < content.Length && (content[check] == ' ' || content[check] == '\t' || content[check] == '\n' || content[check] == '\r')) check++;
            if (check < content.Length && content[check] == '{')
            {
                ruleStart = found;
                int depth = 0, p = check;
                while (p < content.Length) {
                    if (content[p] == '{') depth++;
                    else if (content[p] == '}') { depth--; if (depth == 0) { ruleEnd = p + 1; break; } }
                    p++;
                }
                break;
            }
            searchFrom = found + 1;
        }

        if (ruleStart < 0 || ruleEnd < 0)
        {
            result.SetResult(new { error = $"Selector '{normalizedSelector}' not found in {filePath}" });
            return;
        }

        // Also strip surrounding blank lines
        int removeFrom = ruleStart;
        while (removeFrom > 0 && (content[removeFrom - 1] == '\n' || content[removeFrom - 1] == '\r')) removeFrom--;
        var newContent = content.Substring(0, removeFrom) + "\n" + content.Substring(ruleEnd).TrimStart('\n', '\r');

        File.WriteAllText(filePath, newContent, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, removedSelector = normalizedSelector });
    }
}
```
