# asset_move

Move or rename an asset within the project.

**Signature:** `AssetMove(string sourcePath, string destinationPath)`

**Returns:** `{ success, from, to }` — plus a `serverAvailability` notice if either path touches the script domain.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string sourcePath = "Assets/Scripts/OldName.cs";      // Current project-relative path
        string destinationPath = "Assets/Scripts/NewName.cs"; // New project-relative path (also use for rename)

        if (Validate.SafePath(sourcePath, "sourcePath") is object err1)
        {
            result.SetResult(err1);
            return;
        }
        if (Validate.SafePath(destinationPath, "destinationPath") is object err2)
        {
            result.SetResult(err2);
            return;
        }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(sourcePath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        var error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
        if (!string.IsNullOrEmpty(error))
        {
            result.SetResult(new { error });
            return;
        }

        var res = new Dictionary<string, object>
        {
            ["success"] = true,
            ["from"] = sourcePath,
            ["to"] = destinationPath
        };

        if (ServerAvailabilityHelper.AffectsScriptDomain(sourcePath) || ServerAvailabilityHelper.AffectsScriptDomain(destinationPath))
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                res,
                $"Moved script-domain asset: {sourcePath} -> {destinationPath}. Unity may briefly reload the script domain.",
                alwaysInclude: true);
        }
        else
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                res,
                $"Asset move completed: {destinationPath}. Unity may still be refreshing assets.",
                alwaysInclude: false);
        }

        result.SetResult(res);
    }
}
```
