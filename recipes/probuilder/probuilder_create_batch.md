# probuilder_create_batch

Create multiple ProBuilder shapes in one call. Preferred for scene blockout with 2+ shapes.

**Signature:** `ProBuilderCreateBatch(string items, string defaultParent = null)`

**Returns:** `{ success, results: [{ success, name, instanceId, shape }] }`

## Notes

- `items`: JSON array; each element accepts `shape`, `name`, `x`, `y`, `z`, `sizeX`, `sizeY`, `sizeZ`, `rotX`, `rotY`, `rotZ`, `parent`, `materialPath`.
- `defaultParent`: applied to items that omit their own `parent`.
- `y` is the center of each shape — set `y = -sizeY/2` to place the bottom at world origin.
- Requires `com.unity.probuilder` package.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string defaultParent = null;
        string items = @"[
            { ""shape"": ""Cube"", ""name"": ""Floor"", ""sizeX"": 10, ""sizeY"": 0.3, ""sizeZ"": 10, ""y"": -0.15 },
            { ""shape"": ""Cube"", ""name"": ""Wall_N"", ""sizeX"": 10, ""sizeY"": 3, ""sizeZ"": 0.3, ""z"": -5, ""y"": 1.5 }
        ]";

        var res = UnitySkillsBridge.Call("probuilder_create_batch", new { items, defaultParent });
        result.Log("Batch created: {0}", res);
    }
}
```
