# uitk_add_uss_rule

Add a new USS selector rule to a stylesheet, or replace the rule if the selector already exists.

**Signature:** `UitkAddUssRule(filePath string, selector string, properties string)`

**Returns:** `{ success, path, selector, action }`

**Notes:**
- `action` is `"added"` for new selectors or `"updated"` for existing ones.
- `properties` is the raw CSS property block content, e.g. `"color: white; font-size: 14px;"`.
- USS does not support `display:grid`, `box-shadow`, `calc()`, `@media`, `::before`/`::after`, or gradients.

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
        string properties = "background-color: #4A90D9;\ncolor: white;\nborder-radius: 4px;";

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (Validate.Required(selector, "selector") is object selErr) { result.SetResult(selErr); return; }
        if (Validate.Required(properties, "properties") is object propErr) { result.SetResult(propErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
        if (existing != null) WorkflowManager.SnapshotObject(existing);

        var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        var normalizedSelector = selector.Trim();

        // Format properties with indentation
        var formattedProps = string.Join("\n", System.Array.ConvertAll(
            properties.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries),
            p => { var t = p.Trim(); return string.IsNullOrEmpty(t) ? "" : $"    {t};"; }
        )).Trim('\n');

        var newRule = $"{normalizedSelector} {{\n{formattedProps}\n}}";

        // Find selector followed by optional whitespace then {
        bool existed = false;
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
                existed = ruleEnd > ruleStart;
                break;
            }
            searchFrom = found + 1;
        }

        string newContent;
        if (existed)
            newContent = content.Substring(0, ruleStart) + newRule + content.Substring(ruleEnd);
        else
            newContent = content.TrimEnd() + "\n\n" + newRule + "\n";

        File.WriteAllText(filePath, newContent, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, selector = normalizedSelector, action = existed ? "updated" : "added" });
    }
}
```
