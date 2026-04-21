# gameobject_set_parent_batch

Set parent for multiple GameObjects in one call.

**Signature:** `GameObjectSetParentBatch(string items)`

`items`: JSON array of objects `{ childName, childInstanceId, childPath, parentName, parentInstanceId, parentPath }`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target, parent }] }`

## Notes

- At least one child identifier per item is required.
- Omit all parent fields to unparent to scene root.
- A missing child or parent causes that item to fail without stopping the rest.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string items = @"[
            { ""childName"": ""Wheel_FL"", ""parentName"": ""Car"" },
            { ""childName"": ""Wheel_FR"", ""parentName"": ""Car"" },
            { ""childInstanceId"": 12345, ""parentPath"": ""Level/Root"" }
        ]";

        /* Original Logic:

            return BatchExecutor.Execute<BatchSetParentItem>(items, item =>
            {
                var (child, childError) = GameObjectFinder.FindOrError(item.childName, item.childInstanceId, item.childPath);
                if (childError != null) throw new System.Exception("Child object not found");

                Transform parent = null;
                if (!string.IsNullOrEmpty(item.parentName) || item.parentInstanceId != 0 || !string.IsNullOrEmpty(item.parentPath))
                {
                    var (parentGo, parentError) = GameObjectFinder.FindOrError(item.parentName, item.parentInstanceId, item.parentPath);
                    if (parentError != null)
                        throw new System.Exception($"Parent not found: {item.parentName ?? item.parentPath}");
                    parent = parentGo.transform;
                }

                WorkflowManager.SnapshotObject(child.transform);
                Undo.SetTransformParent(child.transform, parent, "Batch Set Parent");
                return new
                {
                    target = child.name,
                    success = true,
                    parent = parent?.name ?? "(root)"
                };
            }, item => item.childName ?? item.childPath);
        */
    }
}
```
