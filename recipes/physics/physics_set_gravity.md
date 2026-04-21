# physics_set_gravity

Set global gravity setting.

**Signature:** `PhysicsSetGravity(float x, float y, float z)`

**Returns:** `{ success, gravity: { x, y, z } }`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float x = 0f;
        float y = -9.81f;
        float z = 0f;

        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset");
        if (assets != null && assets.Length > 0)
        {
            Undo.RecordObject(assets[0], "Set Gravity");
        }

        Physics.gravity = new Vector3(x, y, z);

        if (assets != null && assets.Length > 0)
        {
            EditorUtility.SetDirty(assets[0]);
        }

        result.SetResult(new { success = true, gravity = new { x, y, z } });
    }
}
```
