# prefab_create_variant

Create a prefab variant from an existing prefab. The variant inherits the source prefab and can override individual properties.

**Signature:** `PrefabCreateVariant(string sourcePrefabPath, string variantPath)`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `sourcePrefabPath` | string | Yes | Path to the source prefab asset |
| `variantPath` | string | Yes | Save path for the new variant (must end in `.prefab`) |

## Returns

```json
{
  "success": true,
  "sourcePath": "Assets/Prefabs/Enemy.prefab",
  "variantPath": "Assets/Prefabs/EnemyElite.prefab",
  "name": "EnemyElite"
}
```

## Notes

- Internally instantiates the source prefab temporarily, saves it as a connected variant, then destroys the temp instance.
- Intermediate directories are created automatically.
- The variant maintains a parent-child relationship with the source — changes to the source propagate unless overridden.
- Workflow snapshot records the created variant asset.

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        /* Original Logic:

            if (Validate.Required(sourcePrefabPath, "sourcePrefabPath") is object err) return err;
            if (Validate.SafePath(variantPath, "variantPath") is object pathErr) return pathErr;

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
            if (source == null) return new { error = $"Prefab not found: {sourcePrefabPath}" };

            var dir = Path.GetDirectoryName(variantPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            var variant = PrefabUtility.SaveAsPrefabAssetAndConnect(
                instance, variantPath, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(instance);

            return new { success = true, sourcePath = sourcePrefabPath, variantPath, name = variant.name };
        */
    }
}
```
