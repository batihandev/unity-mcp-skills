# uitk_create_document

Create a new GameObject with a UIDocument component in the active scene.

**Signature:** `UitkCreateDocument(name string = "UIDocument", uxmlPath string = null, panelSettingsPath string = null, sortOrder int = 0, parentName string = null, parentInstanceId int = 0, parentPath string = null)`

**Returns:** `{ success, name, instanceId, hasUxml, hasPanelSettings, sortOrder }`

**Notes:**
- `uxmlPath` and `panelSettingsPath` are optional; the document is created without them if omitted.
- Provide at most one of `parentName`, `parentInstanceId`, or `parentPath` to reparent the new object.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "UIDocument";
        string uxmlPath = null;
        string panelSettingsPath = null;
        int sortOrder = 0;
        string parentName = null;
        int parentInstanceId = 0;
        string parentPath = null;

        var go = new GameObject(name);

        if (!string.IsNullOrEmpty(parentName) || parentInstanceId != 0 || !string.IsNullOrEmpty(parentPath))
        {
            var parent = GameObjectFinder.Find(parentName, parentInstanceId, parentPath);
            if (parent == null)
            {
                UnityEngine.Object.DestroyImmediate(go);
                result.SetResult(new { error = $"Parent not found: {parentName ?? parentPath}" });
                return;
            }
            go.transform.SetParent(parent.transform, false);
        }

        var doc = go.AddComponent<UIDocument>();

        if (!string.IsNullOrEmpty(uxmlPath))
        {
            if (Validate.SafePath(uxmlPath, "uxmlPath") is object uxmlErr)
            {
                UnityEngine.Object.DestroyImmediate(go);
                result.SetResult(uxmlErr);
                return;
            }
            var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (vta == null)
            {
                UnityEngine.Object.DestroyImmediate(go);
                result.SetResult(new { error = $"VisualTreeAsset not found: {uxmlPath}" });
                return;
            }
            doc.visualTreeAsset = vta;
        }

        if (!string.IsNullOrEmpty(panelSettingsPath))
        {
            if (Validate.SafePath(panelSettingsPath, "panelSettingsPath") is object psErr)
            {
                UnityEngine.Object.DestroyImmediate(go);
                result.SetResult(psErr);
                return;
            }
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
            if (ps == null)
            {
                UnityEngine.Object.DestroyImmediate(go);
                result.SetResult(new { error = $"PanelSettings not found: {panelSettingsPath}" });
                return;
            }
            doc.panelSettings = ps;
        }

        doc.sortingOrder = sortOrder;
        Undo.RegisterCreatedObjectUndo(go, "Create UIDocument");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            hasUxml = doc.visualTreeAsset != null,
            hasPanelSettings = doc.panelSettings != null,
            sortOrder
        });
    }
}
```
