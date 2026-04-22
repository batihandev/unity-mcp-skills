# uitk_list_uss_variables

Inspect CSS custom property (design token) definitions and `var()` usages in a USS file.

**Signature:** `UitkListUssVariables(filePath string)`

**Returns:** `{ path, definedCount, variables[] { name, value }, referencedVariables[] }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filePath = "Assets/UI/MyStyle.uss";

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

        // Find CSS custom property definitions: --var-name: value;
        var variables = new List<object>();
        var seen = new List<string>();
        int p = 0;
        while (p < content.Length - 2)
        {
            int dp = content.IndexOf("--", p);
            if (dp < 0) break;
            int ne = dp + 2;
            while (ne < content.Length && (char.IsLetterOrDigit(content[ne]) || content[ne] == '-')) ne++;
            string vn = content.Substring(dp, ne - dp);
            int cp = ne;
            while (cp < content.Length && (content[cp] == ' ' || content[cp] == '\t')) cp++;
            if (cp < content.Length && content[cp] == ':')
            {
                int vs = cp + 1;
                while (vs < content.Length && (content[vs] == ' ' || content[vs] == '\t')) vs++;
                int se = content.IndexOf(';', vs);
                if (se >= 0)
                {
                    string vv = content.Substring(vs, se - vs).Trim();
                    if (!seen.Contains(vn)) { seen.Add(vn); variables.Add(new { name = vn, value = vv }); }
                }
            }
            p = ne;
        }

        // Find var(--name) usages
        var usages = new List<string>();
        p = 0;
        while (p < content.Length - 6)
        {
            int vp = content.IndexOf("var(--", p);
            if (vp < 0) break;
            int ns2 = vp + 4;
            int ne2 = ns2 + 2;
            while (ne2 < content.Length && (char.IsLetterOrDigit(content[ne2]) || content[ne2] == '-')) ne2++;
            string u = content.Substring(ns2, ne2 - ns2); if (!usages.Contains(u)) usages.Add(u);
            p = ne2;
        }
        usages.Sort();

        result.SetResult(new
        {
            path = filePath,
            definedCount = variables.Count,
            variables,
            referencedVariables = usages.ToArray()
        });
    }
}
```
