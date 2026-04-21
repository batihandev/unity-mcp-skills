# material_set_emission_batch

Set emission color on multiple objects in a single call (efficient batch operation).

**Signature:** `MaterialSetEmissionBatch(string items)`

**`items`:** JSON array of `{ name?, instanceId?, path?, r?, g?, b?, intensity?, enableEmission? }` objects.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target, emissionEnabled }] }`

## Notes

- Each item delegates to `material_set_emission` internally; all the same rules apply (HDR, keyword enable/disable).
- `intensity` defaults to `1.0` for items that omit it (if `<= 0`, falls back to `1.0`).
- `enableEmission` defaults to `true`.
- Prefer this over calling `material_set_emission` repeatedly when operating on 2+ objects.

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
        string items = @"[
            { ""name"": ""Lantern1"", ""r"": 1.0, ""g"": 0.8, ""b"": 0.2, ""intensity"": 3.0 },
            { ""name"": ""Lantern2"", ""r"": 1.0, ""g"": 0.8, ""b"": 0.2, ""intensity"": 3.0 },
            { ""name"": ""Screen"",   ""r"": 0.2, ""g"": 0.8, ""b"": 1.0, ""intensity"": 2.0 }
        ]";

        { result.SetResult(BatchExecutor.Execute<BatchEmissionItem>(items, item =>
        {
            var result = MaterialSetEmission(name: item.name, instanceId: item.instanceId, path: item.path,
                r: item.r, g: item.g, b: item.b, intensity: item.intensity > 0 ? item.intensity : 1f, enableEmission: item.enableEmission);
            if (SkillResultHelper.TryGetError(result, out string errorText))
                throw new System.Exception(errorText);
            return result;
        }, item => item.name ?? item.path)); return; }
    }
}
```
