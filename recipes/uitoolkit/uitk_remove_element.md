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
using System.Xml.Linq;

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
        var xdoc = XDocument.Parse(content);

        var target = FindXmlElementByName(xdoc.Root, elementName);
        if (target == null) { result.SetResult(new { error = $"Element with name '{elementName}' not found" }); return; }

        target.Remove();
        File.WriteAllText(filePath, xdoc.ToString(), System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, removedElement = elementName });
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
