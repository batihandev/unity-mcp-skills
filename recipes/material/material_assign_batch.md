# material_assign_batch

Assign materials to multiple GameObjects in a single call (efficient batch operation).

**Signature:** `MaterialAssignBatch(string items)`

**`items`:** JSON array of `{ name?, instanceId?, path?, materialPath }` objects.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, name, materialPath }] }`

## Notes

- Each item resolves its target GameObject via `name`, `instanceId`, or `path` — at least one must be provided.
- `materialPath` is required on every item.
- Prefer this over calling `material_assign` repeatedly when assigning to 2+ objects.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string items = @"[
            { ""name"": ""Cube"",   ""materialPath"": ""Assets/Materials/Red.mat"" },
            { ""name"": ""Sphere"", ""materialPath"": ""Assets/Materials/Blue.mat"" }
        ]";

        /* Original Logic:

            return BatchExecutor.Execute<BatchMaterialAssignItem>(items, item =>
            {
                var result = MaterialAssign(name: item.name, instanceId: item.instanceId, path: item.path, materialPath: item.materialPath);
                if (SkillResultHelper.TryGetError(result, out string errorText))
                    throw new System.Exception(errorText);
                return result;
            }, item => item.name ?? item.path);
        */
    }
}
```
