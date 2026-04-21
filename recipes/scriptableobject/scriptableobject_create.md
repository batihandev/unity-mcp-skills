# scriptableobject_create

Create a new ScriptableObject asset.

**Signature:** `ScriptableObjectCreate(string typeName, string savePath)`

**Returns:** `{ success, type, path }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string typeName = "MyScriptableObject"; // ScriptableObject type name
        string savePath = "Assets/Data/MyAsset.asset"; // Asset save path (must start with Assets/)

        if (Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }

        var type = FindScriptableObjectType(typeName);
        if (type == null)
        {
            result.SetResult(new { error = $"ScriptableObject type not found: {typeName}" });
            return;
        }

        var instance = ScriptableObject.CreateInstance(type);
        if (instance == null)
        {
            result.SetResult(new { error = $"Failed to create instance of: {typeName}" });
            return;
        }

        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        AssetDatabase.CreateAsset(instance, savePath);
        WorkflowManager.SnapshotObject(instance, SnapshotType.Created);
        AssetDatabase.SaveAssets();

        result.SetResult(new { success = true, type = typeName, path = savePath });
    }
}
```
