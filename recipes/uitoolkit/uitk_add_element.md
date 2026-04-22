# uitk_add_element

Add a child element to a UXML file.

**Signature:** `UitkAddElement(filePath string, elementType string, parentName string = null, elementName string = null, text string = null, classes string = null, style string = null, bindingPath string = null)`

**Returns:** `{ success, path, elementType, elementName }`

**Notes:**
- `parentName` is the `name` attribute of the parent element; omit to append to the document root.
- `elementType` is the unqualified tag name, e.g. `Button`, `Label`, `VisualElement`.
- `classes` is a space-separated list of USS class names.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filePath = "Assets/UI/MyLayout.uxml";
        string elementType = "Button";
        string parentName = null;
        string elementName = "my-button";
        string text = "Click Me";
        string classes = null;
        string style = null;
        string bindingPath = null;

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (Validate.Required(elementType, "elementType") is object typeErr) { result.SetResult(typeErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
        if (existing != null) WorkflowManager.SnapshotObject(existing);

        var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

        // Extract namespace prefix used in root (e.g. "engine:" from xmlns:engine="...")
        string nsPrefix = "";
        int xmlnsPos = content.IndexOf("xmlns:");
        if (xmlnsPos >= 0)
        {
            int nsPrefixEnd = xmlnsPos + 6;
            while (nsPrefixEnd < content.Length && content[nsPrefixEnd] != '=') nsPrefixEnd++;
            nsPrefix = content.Substring(xmlnsPos + 6, nsPrefixEnd - xmlnsPos - 6) + ":";
        }

        // Build new element tag
        var sb = new StringBuilder();
        sb.Append($"<{nsPrefix}{elementType}");
        if (!string.IsNullOrEmpty(elementName))  sb.Append($" name=\"{elementName}\"");
        if (!string.IsNullOrEmpty(text))         sb.Append($" text=\"{text}\"");
        if (!string.IsNullOrEmpty(classes))      sb.Append($" class=\"{classes}\"");
        if (!string.IsNullOrEmpty(style))        sb.Append($" style=\"{style}\"");
        if (!string.IsNullOrEmpty(bindingPath))  sb.Append($" binding-path=\"{bindingPath}\"");
        sb.Append(" />");
        string newElText = sb.ToString();

        string newContent;
        if (string.IsNullOrEmpty(parentName))
        {
            // Insert before root's closing tag (last </ in file)
            int lastClose = content.TrimEnd().LastIndexOf("</");
            if (lastClose < 0) { result.SetResult(new { error = "Cannot find root closing tag in UXML" }); return; }
            newContent = content.Substring(0, lastClose) + "    " + newElText + "\n" + content.Substring(lastClose);
        }
        else
        {
            var (ps, pe, pt) = FindEl(content, parentName);
            if (ps < 0) { result.SetResult(new { error = $"Parent element '{parentName}' not found" }); return; }

            int poe = content.IndexOf('>', ps);
            bool psc = poe > 0 && content[poe - 1] == '/';

            if (psc)
            {
                // Self-closing parent: convert to open/close and insert child
                string openPart = content.Substring(ps, poe - 1 - ps); // without trailing /
                string fullTag = content.Substring(ps + 1, content.IndexOf(' ', ps + 1) - ps - 1);
                if (string.IsNullOrEmpty(fullTag) || fullTag.Contains(">")) fullTag = pt;
                newContent = content.Substring(0, ps) + openPart + ">\n    " + newElText + "\n</" + nsPrefix + pt + ">" + content.Substring(pe);
            }
            else
            {
                // Walk backward from pe to find start of closing tag
                int closeStart = pe - 1; // at >
                while (closeStart > ps && content[closeStart] != '<') closeStart--;
                newContent = content.Substring(0, closeStart) + "    " + newElText + "\n" + content.Substring(closeStart);
            }
        }

        File.WriteAllText(filePath, newContent, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, elementType, elementName = elementName ?? "(unnamed)" });
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
