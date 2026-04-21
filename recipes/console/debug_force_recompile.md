# debug_force_recompile

Force Unity to refresh the asset database and request immediate script recompilation.

**Signature:** `DebugForceRecompile()` — no parameters.

**Returns:** `{ success, message, serverAvailability }`

## Notes

- Calls `AssetDatabase.Refresh()` then `CompilationPipeline.RequestScriptCompilation()`.
- The REST server will be transiently unavailable while Unity reloads assemblies after compilation.
- `serverAvailability` contains a structured notice; always surface it to the user.
- Requires `using UnityEditor.Compilation;`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        AssetDatabase.Refresh();
        CompilationPipeline.RequestScriptCompilation();

        result.SetResult(new
        {
            success = true,
            message = "Compilation requested",
            serverAvailability = ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                "Compilation was requested manually. The REST server may be briefly unavailable while Unity reloads assemblies.",
                alwaysInclude: true)
        });
    }
}
```
