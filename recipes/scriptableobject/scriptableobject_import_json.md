# scriptableobject_import_json

Import JSON data into an existing ScriptableObject asset.

**Signature:** `ScriptableObjectImportJson(string assetPath, string json = null, string jsonFilePath = null)`

**Returns:** `{ success, assetPath }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

## Notes

- Provide either `json` or `jsonFilePath`; `json` takes precedence.
- Uses `EditorJsonUtility.FromJsonOverwrite` — only fields present in the JSON are updated.
- Change is recorded via `Undo` and auto-saved.

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Data/MyAsset.asset"; // Target ScriptableObject asset path
        string json = null; // Inline JSON string (takes precedence over jsonFilePath)
        string jsonFilePath = null; // Path to a JSON file (e.g. "Assets/Export/MyAsset.json")

        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (asset == null) { result.SetResult(new { error = $"ScriptableObject not found: {assetPath}" }); return; }

        var data = json;
        if (string.IsNullOrEmpty(data) && !string.IsNullOrEmpty(jsonFilePath))
        {
            if (Validate.SafePath(jsonFilePath, "jsonFilePath") is object pathErr) { result.SetResult(pathErr); return; }
            data = File.ReadAllText(jsonFilePath, System.Text.Encoding.UTF8);
        }

        if (string.IsNullOrEmpty(data)) { result.SetResult(new { error = "No JSON data provided" }); return; }

        WorkflowManager.SnapshotObject(asset);
        Undo.RecordObject(asset, "Import JSON to SO");
        EditorJsonUtility.FromJsonOverwrite(data, asset);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        result.SetResult(new { success = true, assetPath });
    }
}
```
