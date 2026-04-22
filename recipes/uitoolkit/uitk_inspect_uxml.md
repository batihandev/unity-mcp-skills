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
using System.Xml.Linq;
using System.Linq;

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
            var xdoc = XDocument.Parse(content);
            var hierarchy = ParseXmlNode(xdoc.Root, 0, depth);
            result.SetResult(new { path = filePath, hierarchy });
        }
        catch (System.Exception ex)
        {
            result.SetResult(new { error = $"Failed to parse UXML: {ex.Message}" });
        }
    }

    private object ParseXmlNode(XElement element, int currentDepth, int maxDepth)
    {
        var tag = element.Name.LocalName;
        var attrs = element.Attributes()
            .Where(a => !a.IsNamespaceDeclaration)
            .ToDictionary(a => a.Name.LocalName, a => a.Value);

        var childElements = element.Elements().ToArray();
        if (currentDepth >= maxDepth && childElements.Length > 0)
            return new { tag, attributes = attrs, children = new[] { new { note = $"[{childElements.Length} children; truncated at depth {maxDepth}]" } } };

        var children = childElements
            .Select(c => ParseXmlNode(c, currentDepth + 1, maxDepth))
            .ToArray();

        return new { tag, attributes = attrs, children };
    }
}
```
