# uitk_add_uss_rule

Add a new USS selector rule to a stylesheet, or replace the rule if the selector already exists.

**Signature:** `UitkAddUssRule(filePath string, selector string, properties string)`

**Returns:** `{ success, path, selector, action }`

**Notes:**
- `action` is `"added"` for new selectors or `"updated"` for existing ones.
- `properties` is the raw CSS property block content, e.g. `"color: white; font-size: 14px;"`.
- USS does not support `display:grid`, `box-shadow`, `calc()`, `@media`, `::before`/`::after`, or gradients.

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
        string properties = "background-color: #4A90D9;\ncolor: white;\nborder-radius: 4px;";

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (Validate.Required(selector, "selector") is object selErr) { result.SetResult(selErr); return; }
        if (Validate.Required(properties, "properties") is object propErr) { result.SetResult(propErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
        if (existing != null) WorkflowManager.SnapshotObject(existing);

        var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        var normalizedSelector = selector.Trim();

        var pattern = Regex.Escape(normalizedSelector) + @"\s*\{[^}]*\}";
        var regex = new Regex(pattern, RegexOptions.None, System.TimeSpan.FromSeconds(1));

        // Format properties with indentation
        var formattedProps = string.Join("\n", System.Array.ConvertAll(
            properties.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries),
            p => { var t = p.Trim(); return string.IsNullOrEmpty(t) ? "" : $"    {t};"; }
        )).Trim('\n');

        var newRule = $"{normalizedSelector} {{\n{formattedProps}\n}}";

        string newContent;
        bool existed = regex.IsMatch(content);
        if (existed)
            newContent = regex.Replace(content, newRule, 1);
        else
            newContent = content.TrimEnd() + "\n\n" + newRule + "\n";

        File.WriteAllText(filePath, newContent, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, selector = normalizedSelector, action = existed ? "updated" : "added" });
    }
}
```
