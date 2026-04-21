# uitk_modify_element

Change attributes, classes, text, or inline style on a named UXML element.

**Signature:** `UitkModifyElement(filePath string, elementName string, text string = null, classes string = null, style string = null, newName string = null, bindingPath string = null, setAttribute string = null, setAttributeValue string = null)`

**Returns:** `{ success, path, element }`

**Notes:**
- Only provided (non-null) parameters are changed; all others are left as-is.
- Use `setAttribute`/`setAttributeValue` to set arbitrary XML attributes not covered by the named parameters.

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
        var xdoc = XDocument.Parse(content);

        var target = FindXmlElementByName(xdoc.Root, elementName);
        if (target == null) { result.SetResult(new { error = $"Element with name '{elementName}' not found" }); return; }

        if (text        != null) target.SetAttributeValue("text",         text);
        if (classes     != null) target.SetAttributeValue("class",        classes);
        if (style       != null) target.SetAttributeValue("style",        style);
        if (newName     != null) target.SetAttributeValue("name",         newName);
        if (bindingPath != null) target.SetAttributeValue("binding-path", bindingPath);
        if (!string.IsNullOrEmpty(setAttribute))
            target.SetAttributeValue(setAttribute, setAttributeValue ?? "");

        File.WriteAllText(filePath, xdoc.ToString(), System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, element = newName ?? elementName });
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
