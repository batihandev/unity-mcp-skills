# gameobject_set_layer_batch

Set the layer for multiple GameObjects in one call.

**Signature:** `GameObjectSetLayerBatch(string items)`

`items`: JSON array of objects `{ name, instanceId, path, layer, recursive }`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target, layer }] }`

## Notes

- `layer` is a layer name string (e.g., `"UI"`, `"Default"`, `"Ignore Raycast"`). Must match a layer defined in the project.
- `recursive` (bool, default `false`): when `true`, the layer change is applied to the object and all of its children recursively.
- A missing object or invalid layer name causes that item to fail without stopping the rest.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string items = @"[
            { ""name"": ""Player"", ""layer"": ""Player"" },
            { ""name"": ""EnemyRoot"", ""layer"": ""Enemy"", ""recursive"": true },
            { ""instanceId"": 12345, ""layer"": ""Default"" }
        ]";

        /* Original Logic:

            return BatchExecutor.Execute<BatchSetLayerItem>(items, item =>
            {
                var (go, error) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
                if (error != null) throw new System.Exception("Object not found");

                int layerId = LayerMask.NameToLayer(item.layer);
                if (layerId == -1)
                    throw new System.Exception($"Layer not found: {item.layer}");

                WorkflowManager.SnapshotObject(go);
                Undo.RecordObject(go, "Batch Set Layer");
                go.layer = layerId;

                if (item.recursive)
                {
                    foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
                    {
                        Undo.RecordObject(child.gameObject, "Batch Set Layer Recursive");
                        child.gameObject.layer = layerId;
                    }
                }

                return new { target = go.name, success = true, layer = item.layer };
            }, item => item.name ?? item.path);
        */
    }
}
```
