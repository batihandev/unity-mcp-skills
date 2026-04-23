# ui_set_text

Update the text content on a Text UI element (supports TMP and legacy Text).

**Signature:** `UISetText(name string = null, instanceId int = 0, path string = null, text string = null)`

**Returns:** `{ success, name, text, usingTMP }`

**Notes:**
- Provide at least one of `name`, `instanceId`, or `path` to identify the target.
- TMP component is checked first; falls back to legacy `Text` if TMP is unavailable or not present.
- Returns an error if neither component type is found on the target.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string text = null;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        // Try TMP first if available
        if (IsTMPAvailable())
        {
            var tmpComp = go.GetComponent(_tmpTextType);
            if (tmpComp != null)
            {
                WorkflowManager.SnapshotObject(tmpComp);
                Undo.RecordObject(tmpComp, "Set Text");
                SetTextOnComponent(tmpComp, text);
                result.SetResult(new { success = true, name = go.name, text, usingTMP = true });
                return;
            }
        }

        // Fallback to Legacy Text
        var textComp = go.GetComponent<Text>();
        if (textComp != null)
        {
            WorkflowManager.SnapshotObject(textComp);
            Undo.RecordObject(textComp, "Set Text");
            textComp.text = text;
            result.SetResult(new { success = true, name = go.name, text, usingTMP = false });
            return;
        }

        result.SetResult(new { error = "No Text component found (checked both TMP and Legacy UI)" });
    }

    private static bool _tmpChecked;
    private static bool _tmpAvailable;
    private static System.Type _tmpTextType;

    private static bool IsTMPAvailable()
    {
        if (!_tmpChecked)
        {
            _tmpChecked = true;
            _tmpTextType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            _tmpAvailable = _tmpTextType != null;
        }
        return _tmpAvailable;
    }

    private static bool SetTextOnComponent(Component comp, string text)
    {
        if (comp == null) return false;
        var textProp = comp.GetType().GetProperty("text");
        if (textProp != null) { textProp.SetValue(comp, text); return true; }
        return false;
    }
}
```
