# uitk_add_element

Add a child element to a UXML file.

**Signature:** `UitkAddElement(filePath string, elementType string, parentName string = null, elementName string = null, text string = null, classes string = null, style string = null, bindingPath string = null)`

**Returns:** `{ success, path, elementType, elementName }`

**Notes:**
- `parentName` is the `name` attribute of the parent element; omit to append to the document root.
- `elementType` is the unqualified tag name, e.g. `Button`, `Label`, `VisualElement`.
- `classes` is a space-separated list of USS class names.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Linq;

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
        var xdoc = XDocument.Parse(content);

        var parent = string.IsNullOrEmpty(parentName)
            ? xdoc.Root
            : FindXmlElementByName(xdoc.Root, parentName);
        if (parent == null) { result.SetResult(new { error = $"Parent element '{parentName}' not found" }); return; }

        var ns = xdoc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var newEl = new XElement(ns + elementType);
        if (!string.IsNullOrEmpty(elementName))  newEl.SetAttributeValue("name", elementName);
        if (!string.IsNullOrEmpty(text))         newEl.SetAttributeValue("text", text);
        if (!string.IsNullOrEmpty(classes))      newEl.SetAttributeValue("class", classes);
        if (!string.IsNullOrEmpty(style))        newEl.SetAttributeValue("style", style);
        if (!string.IsNullOrEmpty(bindingPath))  newEl.SetAttributeValue("binding-path", bindingPath);
        parent.Add(newEl);

        File.WriteAllText(filePath, xdoc.ToString(), System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, elementType, elementName = elementName ?? "(unnamed)" });
    }

    private XElement FindXmlElementByName(XElement root, string name)
    {
        if (root == null) return null;
        if (root.Attribute("name")?.Value == name) return root;
        foreach (var child in root.Elements())
        {
            var found = FindXmlElementByName(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
```
