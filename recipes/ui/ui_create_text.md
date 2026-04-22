# ui_create_text

Create a Text label UI element (auto-detects TextMeshPro).

**Signature:** `UICreateText(name string = "Text", parent string = null, text string = "New Text", fontSize int = 14, r float = 0, g float = 0, b float = 0)`

**Returns:** `{ success, name, instanceId, parent, usingTMP }`

**Notes:**
- Automatically uses `TextMeshProUGUI` when TMP is in the project; falls back to legacy `Text`.
- Check `usingTMP` in the response before applying TMP-specific component edits.
- Default size is 200 x 50.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Text";
        string parent = null;
        string text = "New Text";
        int fontSize = 14;
        float r = 0f, g = 0f, b = 0f;

        var parentGo = FindOrCreateCanvas(parent);
        if (parentGo == null)
        {
            result.SetResult(new { error = "Parent not found and could not create Canvas" });
            return;
        }

        var go = new GameObject(name);
        go.transform.SetParent(parentGo.transform, false);

        var rectTransform = go.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50);

        AddTextComponent(go, text, fontSize, new Color(r, g, b));

        Undo.RegisterCreatedObjectUndo(go, "Create Text");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, name = go.name, instanceId = go.GetInstanceID(), parent = parentGo.name, usingTMP = IsTMPAvailable() });
    }
}
```
