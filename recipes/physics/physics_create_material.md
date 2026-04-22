# physics_create_material

Create a PhysicMaterial asset.

**Signature:** `PhysicsCreateMaterial(string name = "New PhysicMaterial", string savePath = "Assets", float dynamicFriction = 0.6f, float staticFriction = 0.6f, float bounciness = 0f)`

**Returns:** `{ success, path }`

> Unity 6+: uses `PhysicsMaterial`; older versions: `PhysicMaterial`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "New PhysicMaterial";
        string savePath = "Assets";
        float dynamicFriction = 0.6f;
        float staticFriction = 0.6f;
        float bounciness = 0f;

        if (string.IsNullOrEmpty(name))
        {
            result.SetResult(new { error = "name is required" });
            return;
        }
        if (name.Contains("/") || name.Contains("\\") || name.Contains(".."))
        {
            result.SetResult(new { error = "name must not contain path separators" });
            return;
        }

        var mat = new PhysicsMaterial(name)
        {
            dynamicFriction = dynamicFriction,
            staticFriction = staticFriction,
            bounciness = bounciness
        };

        var path = System.IO.Path.Combine(savePath, name + ".physicMaterial");
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        var dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        result.SetResult(new { success = true, path });
    }
}
```
