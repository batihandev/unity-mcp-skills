# validate_shader_errors

Find shader assets in the project that have compilation errors.

**Signature:** `ValidateShaderErrors(limit int = 50)`

**Returns:** `{ success, count, shaders: [{ name, path, errorCount }] }`

**Notes:**
- Uses `UnityEditor.ShaderUtil.GetShaderMessageCount` to detect compilation errors
- Scans all `t:Shader` assets in the project, not just those used in the active scene

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int limit = 50;

        var guids = AssetDatabase.FindAssets("t:Shader");
        var errors = new List<object>();
        foreach (var guid in guids)
        {
            if (errors.Count >= limit) break;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null) continue;
            int msgCount = UnityEditor.ShaderUtil.GetShaderMessageCount(shader);
            if (msgCount > 0)
                errors.Add(new { name = shader.name, path, errorCount = msgCount });
        }

        result.SetResult(new { success = true, count = errors.Count, shaders = errors });
    }
}
```
