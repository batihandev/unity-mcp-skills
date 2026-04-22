# probuilder_combine_meshes

Merge multiple ProBuilder meshes into one for optimization.

**Signature:** `ProBuilderCombineMeshes(string names = null)`

**Returns:** `{ success, name, instanceId, combinedCount, resultMeshCount, vertexCount, faceCount }`

## Notes

- `names`: comma-separated GameObject names, e.g. `"Wall_A,Wall_B"`. Use `"selected"` to combine the current editor selection.
- Requires at least 2 ProBuilder meshes.
- Source meshes (all except the first) are destroyed after combining. This operation is undoable.
- The combined result is placed on the first named object.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.probuilder` package.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string names = null;

        List<ProBuilderMesh> meshes;

        if (!string.IsNullOrEmpty(names) && !names.Equals("selected", StringComparison.OrdinalIgnoreCase))
        {
            meshes = new List<ProBuilderMesh>();
            foreach (var n in names.Split(','))
            {
                var go = GameObjectFinder.Find(name: n.Trim());
                if (go == null) { result.SetResult(new { error = "GameObject not found: " + n.Trim() }); return; }
                var pb = go.GetComponent<ProBuilderMesh>();
                if (pb == null) { result.SetResult(new { error = "'" + n.Trim() + "' has no ProBuilderMesh" }); return; }
                meshes.Add(pb);
            }
        }
        else
        {
            meshes = Selection.gameObjects
                .Select(g => g.GetComponent<ProBuilderMesh>())
                .Where(pb => pb != null)
                .ToList();
        }

        if (meshes.Count < 2)
        { result.SetResult(new { error = "At least 2 ProBuilder meshes are required to combine" }); return; }

        foreach (var m in meshes)
        {
            Undo.RecordObject(m.gameObject, "Combine Meshes");
            WorkflowManager.SnapshotObject(m.gameObject);
        }

        var target = meshes[0];
        var combined = CombineMeshes.Combine(meshes, target);

        for (int i = 1; i < meshes.Count; i++)
            Undo.DestroyObjectImmediate(meshes[i].gameObject);

        target.ToMesh();
        target.Refresh();

        result.SetResult(new
        {
            success = true,
            name = target.gameObject.name,
            instanceId = target.gameObject.GetInstanceID(),
            combinedCount = meshes.Count,
            resultMeshCount = combined?.Count ?? 1,
            vertexCount = target.vertexCount,
            faceCount = target.faceCount
        });
    }
}
```
