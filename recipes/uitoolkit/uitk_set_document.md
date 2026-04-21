# uitk_set_document

Change the UIDocument asset bindings on an existing scene GameObject.

**Signature:** `UitkSetDocument(name string = null, instanceId int = 0, path string = null, uxmlPath string = null, panelSettingsPath string = null, sortOrder int? = null)`

**Returns:** `{ success, name, instanceId, visualTreeAsset, panelSettings, sortingOrder }`

**Notes:**
- Identify the target GameObject with exactly one of `name`, `instanceId`, or `path`.
- If the GameObject has no UIDocument component one will be added.
- Only the fields you supply are changed; omit the rest to leave them as-is.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "UIDocument";
        int instanceId = 0;
        string path = null;
        string uxmlPath = "Assets/UI/MyLayout.uxml";
        string panelSettingsPath = null;
        int? sortOrder = null;

        var go = GameObjectFinder.Find(name, instanceId, path);
        if (go == null) { result.SetResult(new { error = $"GameObject not found: {name ?? path}" }); return; }

        var doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
        Undo.RecordObject(doc, "Set UIDocument");

        if (!string.IsNullOrEmpty(uxmlPath))
        {
            if (Validate.SafePath(uxmlPath, "uxmlPath") is object uxmlErr) { result.SetResult(uxmlErr); return; }
            var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (vta == null) { result.SetResult(new { error = $"VisualTreeAsset not found: {uxmlPath}" }); return; }
            doc.visualTreeAsset = vta;
        }

        if (!string.IsNullOrEmpty(panelSettingsPath))
        {
            if (Validate.SafePath(panelSettingsPath, "panelSettingsPath") is object psErr) { result.SetResult(psErr); return; }
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
            if (ps == null) { result.SetResult(new { error = $"PanelSettings not found: {panelSettingsPath}" }); return; }
            doc.panelSettings = ps;
        }

        if (sortOrder.HasValue)
            doc.sortingOrder = sortOrder.Value;

        WorkflowManager.SnapshotObject(go);

        result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            visualTreeAsset = doc.visualTreeAsset != null ? AssetDatabase.GetAssetPath(doc.visualTreeAsset) : null,
            panelSettings   = doc.panelSettings   != null ? AssetDatabase.GetAssetPath(doc.panelSettings)   : null,
            sortingOrder = doc.sortingOrder
        });
    }
}
```
