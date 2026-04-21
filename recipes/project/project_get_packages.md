# project_get_packages

Read the project's UPM `Packages/manifest.json` and return its full contents. Read-only; no parameters required.

**Signature:** `ProjectGetPackages()`

**Returns:** `{ success, manifest }` where `manifest` is the parsed JSON object from `manifest.json`.

## Notes

- Returns `{ error: "manifest.json not found" }` when `Packages/manifest.json` does not exist.
- `manifest` contains `dependencies` and optionally `scopedRegistries`, `lock`, etc.
- This is the equivalent of reading the raw manifest; no package resolution is performed.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

## C# Template

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var manifestPath = "Packages/manifest.json";
        if (!File.Exists(manifestPath))
        {
            result.SetResult(new { error = "manifest.json not found" });
            return;
        }

        var json = File.ReadAllText(manifestPath, System.Text.Encoding.UTF8);
        var manifest = Newtonsoft.Json.Linq.JObject.Parse(json);

        result.SetResult(new { success = true, manifest });
    }
}
```
