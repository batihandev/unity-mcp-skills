# probuilder_set_material

Assign a material to an entire ProBuilder mesh, or apply a quick prototype color.

**Signature:** `ProBuilderSetMaterial(string name = null, int instanceId = 0, string path = null, string materialPath = null, float? r = null, float? g = null, float? b = null, float? a = null)`

**Returns:** `{ success, name, instanceId, material }` or `{ success, name, instanceId, materialName, color, note }` for runtime color.

## Notes

- Provide `materialPath` for a persistent material asset (e.g. `"Assets/Materials/MyMat.mat"`).
- Provide `r/g/b` (and optionally `a`) for a quick runtime color. The color is NOT saved as an asset.
- You must provide one or the other — both empty is an error.
- For per-face material assignment use `probuilder_set_face_material`.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";

        // Option A: persistent material asset
        string materialPath = "Assets/Materials/MyMat.mat";
        var res = UnitySkillsBridge.Call("probuilder_set_material", new { name, materialPath });

        // Option B: quick prototype color (runtime only, not saved as asset)
        // var res = UnitySkillsBridge.Call("probuilder_set_material", new { name, r = 0.2f, g = 0.5f, b = 0.8f });

        result.Log("Set material: {0}", res);
    }
}
```
