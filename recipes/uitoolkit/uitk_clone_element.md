# uitk_clone_element

Duplicate a named element (including its entire subtree) and insert the clone after the original.

**Signature:** `UitkCloneElement(filePath string, elementName string, newName string = null)`

**Returns:** `{ success, path, clonedFrom, newName }`

**Notes:**
- The clone is inserted immediately after the original element in the parent's child list.
- Supply `newName` to give the clone a distinct `name` attribute; otherwise both elements share the same name.

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
        string newName = "my-button-copy";

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (Validate.Required(elementName, "elementName") is object nameErr) { result.SetResult(nameErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
        if (existing != null) WorkflowManager.SnapshotObject(existing);

        var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

        var (s, e, t) = FindEl(content, elementName);
        if (s < 0) { result.SetResult(new { error = $"Element with name '{elementName}' not found" }); return; }

        var elText = content.Substring(s, e - s);
        var clone = !string.IsNullOrEmpty(newName)
            ? elText.Replace("name=\"" + elementName + "\"", "name=\"" + newName + "\"")
              .Replace("name='" + elementName + "'", "name='" + newName + "'")
            : elText;

        var newContent = content.Substring(0, e) + "\n" + clone + content.Substring(e);
        File.WriteAllText(filePath, newContent, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, clonedFrom = elementName, newName = newName ?? "(copy)" });
    }

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
