# uitk_list_uss_variables

Inspect CSS custom property (design token) definitions and `var()` usages in a USS file.

**Signature:** `UitkListUssVariables(filePath string)`

**Returns:** `{ path, definedCount, variables[] { name, value }, referencedVariables[] }`

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filePath = "Assets/UI/MyStyle.uss";

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

        var defRegex = new Regex(
            @"(--[\w-]+)\s*:\s*([^;]+);",
            RegexOptions.None, System.TimeSpan.FromSeconds(1));

        var variables = new List<object>();
        var seen = new HashSet<string>();
        foreach (Match match in defRegex.Matches(content))
        {
            var varName  = match.Groups[1].Value.Trim();
            var varValue = match.Groups[2].Value.Trim();
            if (seen.Add(varName))
                variables.Add(new { name = varName, value = varValue });
        }

        var usageRegex = new Regex(
            @"var\((--[\w-]+)\)",
            RegexOptions.None, System.TimeSpan.FromSeconds(1));
        var usages = new HashSet<string>();
        foreach (Match match in usageRegex.Matches(content))
            usages.Add(match.Groups[1].Value.Trim());

        result.SetResult(new
        {
            path = filePath,
            definedCount = variables.Count,
            variables,
            referencedVariables = usages.OrderBy(v => v).ToArray()
        });
    }
}
```
