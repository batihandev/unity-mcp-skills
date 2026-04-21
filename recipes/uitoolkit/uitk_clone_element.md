# uitk_clone_element

Duplicate a named element (including its entire subtree) and insert the clone after the original.

**Signature:** `UitkCloneElement(filePath string, elementName string, newName string = null)`

**Returns:** `{ success, path, clonedFrom, newName }`

**Notes:**
- The clone is inserted immediately after the original element in the parent's child list.
- Supply `newName` to give the clone a distinct `name` attribute; otherwise both elements share the same name.

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
        string elementName = "my-button";
        string newName = "my-button-copy";

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (Validate.Required(elementName, "elementName") is object nameErr) { result.SetResult(nameErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
        if (existing != null) WorkflowManager.SnapshotObject(existing);

        var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        var xdoc = XDocument.Parse(content);

        var target = FindXmlElementByName(xdoc.Root, elementName);
        if (target == null) { result.SetResult(new { error = $"Element with name '{elementName}' not found" }); return; }

        var clone = new XElement(target);
        if (!string.IsNullOrEmpty(newName))
            clone.SetAttributeValue("name", newName);

        target.AddAfterSelf(clone);
        File.WriteAllText(filePath, xdoc.ToString(), System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, clonedFrom = elementName, newName = newName ?? "(copy)" });
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
