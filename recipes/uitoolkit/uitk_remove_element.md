# uitk_remove_element

Remove a named element (and its subtree) from a UXML file.

**Signature:** `UitkRemoveElement(filePath string, elementName string)`

**Returns:** `{ success, path, removedElement }`

**Notes:**
- The element is identified by its `name` attribute.
- The entire subtree rooted at the matched element is removed.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filePath = "Assets/UI/MyLayout.uxml";
        string elementName = "my-button";

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (Validate.Required(elementName, "elementName") is object nameErr) { result.SetResult(nameErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
        if (existing != null) WorkflowManager.SnapshotObject(existing);

        var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

        var (s, e, t) = FindEl(content, elementName);
        if (s < 0) { result.SetResult(new { error = $"Element with name '{elementName}' not found" }); return; }

        var newContent = content.Substring(0, s) + content.Substring(e);
        File.WriteAllText(filePath, newContent, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, removedElement = elementName });
    }

    // Find element where name="v". Returns (tagStart, exclusiveEnd, localTagName).
    private static (int s, int e, string tag) FindEl(string x, string v)
    {
        int ap = x.IndexOf("name=\"" + v + "\""); if (ap < 0) ap = x.IndexOf("name='" + v + "'");
        if (ap < 0) return (-1, -1, null);
        int ts = ap; while (ts > 0 && x[ts] != '<') ts--;
        int ne = ts + 1; while (ne < x.Length && x[ne] != ' ' && x[ne] != '\t' && x[ne] != '\n' && x[ne] != '>' && x[ne] != '/') ne++;
        string ft = x.Substring(ts + 1, ne - ts - 1), tl = ft.Contains(":") ? ft.Substring(ft.LastIndexOf(':') + 1) : ft;
        int oe = x.IndexOf('>', ts); if (oe < 0) return (-1, -1, null);
        if (x[oe - 1] == '/') return (ts, oe + 1, tl);
        int d = 1, p = oe + 1;
        while (p < x.Length && d > 0) {
            int n = x.IndexOf('<', p); if (n < 0) break;
            if (x[n + 1] == '/') { int ce = x.IndexOf('>', n); if (ce < 0) break; string ct = x.Substring(n + 2, ce - n - 2).Trim(), cl = ct.Contains(":") ? ct.Substring(ct.LastIndexOf(':') + 1) : ct; if (cl == tl) d--; p = ce + 1; }
            else if (x[n + 1] == '!' || x[n + 1] == '?') { int ce = x.IndexOf('>', n); p = ce < 0 ? x.Length : ce + 1; }
            else { int ce = x.IndexOf('>', n); if (ce < 0) break; if (x[ce - 1] != '/') d++; p = ce + 1; }
        }
        return (ts, p, tl);
    }
}
```
