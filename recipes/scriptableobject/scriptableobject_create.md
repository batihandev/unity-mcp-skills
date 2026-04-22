# scriptableobject_create

Create a new ScriptableObject asset.

**Signature:** `ScriptableObjectCreate(string typeName, string savePath)`

**Returns:** `{ success, type, path }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

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

        System.Type type = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic) continue;
            System.Type[] asmTypes;
            try { asmTypes = asm.GetTypes(); } catch { continue; }
            foreach (var t in asmTypes)
                if (t.Name == typeName && t.IsSubclassOf(typeof(ScriptableObject)) && !t.IsAbstract) { type = t; break; }
            if (type != null) break;
        }
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
