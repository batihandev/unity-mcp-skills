# debug_force_recompile

Force Unity to refresh the asset database and request immediate script recompilation.

**Signature:** `DebugForceRecompile()` — no parameters.

**Returns:** `{ success, message }`

## Notes

- Calls `AssetDatabase.Refresh()` then `CompilationPipeline.RequestScriptCompilation()`.
- Unity will reload assemblies after compilation — subsequent `Unity_RunCommand` calls queue during that window.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        AssetDatabase.Refresh();
        // Fully-qualified: `CompilationPipeline` short-name collides with
        // Unity.CompilationPipeline (the Unity_RunCommand compile namespace is
        // Unity.AI.Assistant.Agent.Dynamic.Extension.Editor — CS0234 on short form).
        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();

        result.SetResult(new { success = true, message = "Compilation requested" });
    }
}
```
