# material_create_batch

Create multiple materials in a single call (efficient batch operation).

**Signature:** `MaterialCreateBatch(string items)`

**`items`:** JSON array of `{ name, shaderName?, savePath? }` objects.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, name, path }] }`

## Notes

- Each item delegates to `material_create` internally; pipeline auto-detection applies per item.
- Prefer this over calling `material_create` repeatedly when creating 2+ materials.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string items = @"[
            { ""name"": ""RedMat"",   ""savePath"": ""Assets/Materials"" },
            { ""name"": ""BlueMat"",  ""shaderName"": ""Universal Render Pipeline/Lit"", ""savePath"": ""Assets/Materials"" },
            { ""name"": ""GreenMat"", ""savePath"": ""Assets/Materials"" }
        ]";

        /* Original Logic:

            return BatchExecutor.Execute<BatchMaterialCreateItem>(items, item =>
            {
                var result = MaterialCreate(item.name, item.shaderName, item.savePath);
                if (SkillResultHelper.TryGetError(result, out string errorText))
                    throw new System.Exception(errorText);
                return result;
            }, item => item.name);
        */
    }
}
```
