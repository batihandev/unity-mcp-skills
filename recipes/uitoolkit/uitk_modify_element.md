# uitk_modify_element

Change attributes, classes, text, or inline style on a named UXML element.

**Signature:** `UitkModifyElement(filePath string, elementName string, text string = null, classes string = null, style string = null, newName string = null, bindingPath string = null, setAttribute string = null, setAttributeValue string = null)`

**Returns:** `{ success, path, element }`

**Notes:**
- Only provided (non-null) parameters are changed; all others are left as-is.
- Use `setAttribute`/`setAttributeValue` to set arbitrary XML attributes not covered by the named parameters.

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
        string text = "New Label";
        string classes = null;
        string style = null;
        string newName = null;
        string bindingPath = null;
        string setAttribute = null;
        string setAttributeValue = null;

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (Validate.Required(elementName, "elementName") is object nameErr) { result.SetResult(nameErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
        if (existing != null) WorkflowManager.SnapshotObject(existing);

        var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

        var (s, e, t) = FindEl(content, elementName);
        if (s < 0) { result.SetResult(new { error = $"Element with name '{elementName}' not found" }); return; }

        // Modify only the opening tag (content[s..oe+1]); keep children and closing tag intact
        int oe = content.IndexOf('>', s);
        bool sc = oe > 0 && content[oe - 1] == '/';
        string openTag = content.Substring(s, oe + 1 - s);

        if (text        != null) openTag = SetAttr(openTag, "text",         text,         sc);
        if (classes     != null) openTag = SetAttr(openTag, "class",        classes,      sc);
        if (style       != null) openTag = SetAttr(openTag, "style",        style,        sc);
        if (newName     != null) openTag = SetAttr(openTag, "name",         newName,      sc);
        if (bindingPath != null) openTag = SetAttr(openTag, "binding-path", bindingPath,  sc);
        if (!string.IsNullOrEmpty(setAttribute))
            openTag = SetAttr(openTag, setAttribute, setAttributeValue ?? "", sc);

        var newContent = content.Substring(0, s) + openTag + content.Substring(oe + 1);
        File.WriteAllText(filePath, newContent, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, element = newName ?? elementName });
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

    // Set or insert an attribute in an opening XML tag string
    private static string SetAttr(string tag, string attr, string value, bool selfClose)
    {
        string q1 = attr + "=\"", q2 = attr + "='";
        int p1 = tag.IndexOf(q1);
        if (p1 >= 0) { int vs = p1 + q1.Length, ve = tag.IndexOf('"', vs); return tag.Substring(0, vs) + value + tag.Substring(ve); }
        int p2 = tag.IndexOf(q2);
        if (p2 >= 0) { int vs = p2 + q2.Length, ve = tag.IndexOf('\'', vs); return tag.Substring(0, vs) + value + tag.Substring(ve); }
        string ins = $" {attr}=\"{value}\"";
        return selfClose
            ? tag.Substring(0, tag.Length - 2) + ins + " />"
            : tag.Substring(0, tag.Length - 1) + ins + ">";
    }
}
```
