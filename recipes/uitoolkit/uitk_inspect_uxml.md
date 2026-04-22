# uitk_inspect_uxml

Parse a UXML file and return its element hierarchy.

**Signature:** `UitkInspectUxml(filePath string, depth int = 5)`

**Returns:** `{ path, hierarchy }`

**Notes:**
- `depth` controls how many levels of the XML tree are traversed before truncating.
- Returns an error if the file does not exist or cannot be parsed as valid XML.

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
        string filePath = "Assets/UI/MyLayout.uxml";
        int depth = 5;

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        try
        {
            var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            // Skip XML declaration / processing instructions to find root element
            int rootPos = 0;
            while (rootPos < content.Length)
            {
                while (rootPos < content.Length && char.IsWhiteSpace(content[rootPos])) rootPos++;
                if (rootPos >= content.Length || content[rootPos] != '<') break;
                if (rootPos + 1 < content.Length && (content[rootPos + 1] == '?' || content[rootPos + 1] == '!'))
                { int ce = content.IndexOf('>', rootPos); rootPos = ce < 0 ? content.Length : ce + 1; }
                else break;
            }
            if (rootPos >= content.Length) { result.SetResult(new { error = "No root element found" }); return; }
            var hierarchy = ParseNode(content, ref rootPos, 0, depth);
            result.SetResult(new { path = filePath, hierarchy });
        }
        catch (System.Exception ex)
        {
            result.SetResult(new { error = $"Failed to parse UXML: {ex.Message}" });
        }
    }

    private static object ParseNode(string x, ref int pos, int cur, int max)
    {
        if (pos >= x.Length || x[pos] != '<') { pos++; return null; }
        int p = pos + 1;
        while (p < x.Length && x[p] != ' ' && x[p] != '\t' && x[p] != '\n' && x[p] != '\r' && x[p] != '>' && x[p] != '/') p++;
        string ft = x.Substring(pos + 1, p - pos - 1);
        string tag = ft.Contains(":") ? ft.Substring(ft.LastIndexOf(':') + 1) : ft;
        int oe = x.IndexOf('>', pos); if (oe < 0) { pos = x.Length; return null; }
        bool sc = oe > 0 && x[oe - 1] == '/';
        var attrs = ReadAttrs(x, p, sc ? oe - 1 : oe);
        if (sc) { pos = oe + 1; return new { tag, attributes = attrs, children = new object[0] }; }
        pos = oe + 1;

        if (cur >= max)
        {
            // Count direct children and skip
            int cnt = 0, d = 1;
            while (pos < x.Length && d > 0) {
                int n = x.IndexOf('<', pos); if (n < 0) break;
                if (x[n + 1] == '/') { int ce = x.IndexOf('>', n); d--; pos = ce < 0 ? x.Length : ce + 1; }
                else if (x[n + 1] == '!' || x[n + 1] == '?') { int ce = x.IndexOf('>', n); pos = ce < 0 ? x.Length : ce + 1; }
                else { int ce = x.IndexOf('>', n); if (ce > 0 && x[ce - 1] != '/') { cnt++; d++; } pos = ce < 0 ? x.Length : ce + 1; }
            }
            return new { tag, attributes = attrs, children = cnt > 0 ? new object[] { (object)new { note = $"[{cnt} children; truncated at depth {max}]" } } : new object[0] };
        }

        var children = new List<object>();
        while (pos < x.Length) {
            while (pos < x.Length && char.IsWhiteSpace(x[pos])) pos++;
            if (pos >= x.Length) break;
            if (x[pos] != '<') { pos++; continue; }
            if (pos + 1 < x.Length && x[pos + 1] == '/') { int ce = x.IndexOf('>', pos); pos = ce < 0 ? x.Length : ce + 1; break; }
            if (pos + 1 < x.Length && (x[pos + 1] == '!' || x[pos + 1] == '?')) { int ce = x.IndexOf('>', pos); pos = ce < 0 ? x.Length : ce + 1; continue; }
            var child = ParseNode(x, ref pos, cur + 1, max);
            if (child != null) children.Add(child);
        }
        return new { tag, attributes = attrs, children = children.ToArray() };
    }

    private static Dictionary<string, string> ReadAttrs(string x, int start, int end)
    {
        var d = new Dictionary<string, string>();
        int p = start;
        while (p < end) {
            while (p < end && char.IsWhiteSpace(x[p])) p++;
            if (p >= end) break;
            int ke = p; while (ke < end && x[ke] != '=' && !char.IsWhiteSpace(x[ke])) ke++;
            string k = x.Substring(p, ke - p);
            if (ke >= end || x[ke] != '=') { p = ke + 1; continue; }
            p = ke + 1; if (p >= end) break;
            char q = x[p]; p++;
            if (q != '"' && q != '\'') continue;
            int ve = x.IndexOf(q, p); if (ve < 0 || ve > end) break;
            if (!k.StartsWith("xmlns")) d[k] = x.Substring(p, ve - p);
            p = ve + 1;
        }
        return d;
    }
}
```
