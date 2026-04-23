# shader_set_global_keyword

Enable or disable a global shader keyword.

**Signature:** `ShaderSetGlobalKeyword(string keyword, bool enabled)`

**Returns:** `{ success, keyword, enabled }`

**Notes:**
- Calls `Shader.EnableKeyword` or `Shader.DisableKeyword` globally
- Affects all shaders that reference the keyword

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string keyword = "EXAMPLE_FEATURE";
        bool enabled = true;

        if (enabled)
            Shader.EnableKeyword(keyword);
        else
            Shader.DisableKeyword(keyword);

        result.SetResult(new { success = true, keyword, enabled });
    }
}
```
