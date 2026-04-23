# validate_fix_missing_scripts

Remove missing script components from all GameObjects in the active scene, with optional dry-run preview.

**Signature:** `ValidateFixMissingScripts(dryRun bool = true)`

**Returns:** `{ success, dryRun, fixedCount, message, objects: [{ gameObject, path, missingCount }] }`

**Notes:**
- Always run with `dryRun = true` first to preview what will be removed
- Registers an Undo operation and snapshots each object before modification
- Only searches scene objects; does not process prefab assets on disk

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool dryRun = true;

        var fixedObjects = new List<object>();
        var sceneObjects = FindHelper.FindAll<GameObject>();

        foreach (var go in sceneObjects)
        {
            var missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (missingCount > 0)
            {
                fixedObjects.Add(new
                {
                    gameObject = go.name,
                    path = GameObjectFinder.GetPath(go),
                    missingCount
                });

                if (!dryRun)
                {
                    WorkflowManager.SnapshotObject(go);
                    Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                }
            }
        }

        result.SetResult(new
        {
            success = true,
            dryRun,
            fixedCount = fixedObjects.Count,
            message = dryRun ? "Dry run - no scripts removed" : $"Removed missing scripts from {fixedObjects.Count} objects",
            objects = fixedObjects
        });
    }
}
```
