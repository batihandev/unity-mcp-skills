# shader_create_urp

Create a URP shader from template (Unlit or Lit).

**Signature:** `ShaderCreateUrp(string shaderName, string savePath, string type = "Unlit")`

**Returns:** `{ success, shaderName, savePath, type }`

**Notes:**
- `type`: `"Unlit"` or `"Lit"` — Lit includes lighting and shadow support
- `savePath` must end in `.shader` and the parent directory must exist (or will be created)
- Fails if the file already exists

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shaderName = "MyShader";
        string savePath = "Assets/Shaders/MyShader.shader";
        string type = "Unlit"; // "Unlit" or "Lit"

        result.SetResult(SmartSkillRunner.Run("shader_create_urp", new {
            shaderName,
            savePath,
            type
        }));
    }
}
```
